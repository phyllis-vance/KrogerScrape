using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using KrogerScrape.Support;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace KrogerScrape.Client
{
    public class KrogerClient : IDisposable
    {
        private const string ConnectionString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

        private static readonly IReadOnlyList<string> ExcludedExtensions = new List<string>
        {
            ".css",
            ".gif",
            ".jpg",
            ".png",
            ".svg",
            ".woff2",
        };

        private static readonly IReadOnlyList<string> ExcludedSubstrings = new List<string>
        {
            "ads.scorecardresearch.com",
            "assets.adobedtm.com",
            "cm.everesttech.net",
            "demdex.net",
            "doubleclick.net",
            "fonts.googleapis.com",
            "googleadservices.com",
            "googletagmanager.com",
            "omtrdc.net",
            "sstats.kroger.com",
            "www.kroger.com/asset/",
            "www.kroger.com/clickstream/",
            "www.kroger.com/product/images/",
        };

        private readonly ILogger<KrogerClient> _logger;
        private readonly AsyncBlockingQueue<Response> _queue;
        private readonly Task _dequeueTask;
        private readonly ConcurrentQueue<Func<Response, Task>> _listeners;
        private readonly Lazy<Task<Browser>> _lazyBrowser;
        private Browser _browserForDispose;

        public KrogerClient(ILogger<KrogerClient> logger)
        {
            _logger = logger;
            _queue = new AsyncBlockingQueue<Response>();
            _dequeueTask = DequeueAsync();
            _listeners = new ConcurrentQueue<Func<Response, Task>>();
            _lazyBrowser = new Lazy<Task<Browser>>(async () =>
            {
                var  revisionInfo = await EnsureDownloadsAsync();

                _logger.LogInformation($"Launching Chromium from:{Environment.NewLine}{{ExecutablePath}}", revisionInfo.ExecutablePath);
                var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    ExecutablePath = revisionInfo.ExecutablePath,
                    Headless = true,
                    DumpIO = false,
                });
                _browserForDispose = browser;
                return browser;
            });
        }

        private static string GetDownloadsDirectory()
        {
            var applicationDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var path = Path.Combine(applicationDirectory, ".local-chromium");
            return Path.GetFullPath(path);
        }

        private async Task<RevisionInfo> EnsureDownloadsAsync()
        {
            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions
            {
                Path = GetDownloadsDirectory(),
            });
            var desiredRevision = BrowserFetcher.DefaultRevision;
            var revisionInfo = browserFetcher.RevisionInfo(desiredRevision);

            if (revisionInfo.Downloaded && revisionInfo.Local)
            {
                _logger.LogDebug(
                    "Using Chromium revision {Revision} for {Platform}.",
                    revisionInfo.Revision,
                    revisionInfo.Platform);
            }
            else
            {
                _logger.LogInformation(
                    "Downloading Chromium revision {Revision} for {Platform}.",
                    revisionInfo.Revision,
                    revisionInfo.Platform);

                // Set up the progress report.
                long? contentLength;
                using (var httpClient = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Head, revisionInfo.Url))
                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                {
                    contentLength = response.Content.Headers.ContentLength;
                }

                long progressIncrement;
                if (contentLength.HasValue)
                {
                    _logger.LogDebug("The download will be {Bytes:N0} bytes.", contentLength);

                    // 5%
                    progressIncrement = 5;
                }
                else
                {
                    // 1 megabyte
                    progressIncrement = 1024 * 1024;
                }

                long nextProgress = 0;
                var progressLock = new object();
                browserFetcher.DownloadProgressChanged += (sender, e) =>
                {
                    lock (progressLock)
                    {
                        if (contentLength.HasValue)
                        {
                            var progressPercentage = (int)Math.Round((100.0 * e.BytesReceived) / contentLength.Value);
                            if (progressPercentage > nextProgress)
                            {
                                _logger.LogInformation("Download progress: {Percentage}%", progressPercentage);
                                nextProgress += progressIncrement;
                            }
                        }
                        else
                        {
                            if (e.BytesReceived > nextProgress)
                            {
                                _logger.LogInformation("Download progress: {BytesReceived:N0} bytes", e.BytesReceived);
                                nextProgress += progressIncrement;
                            }
                        }
                    }
                };

                // Start the download.
                revisionInfo = await browserFetcher.DownloadAsync(desiredRevision);

                _logger.LogInformation("Chromium is done downloading.");
            }

            return revisionInfo;
        }

        private async Task DequeueAsync()
        {
            await Task.Yield();

            bool hasItem;
            do
            {
                var result = await _queue.TryDequeueAsync();
                hasItem = result.HasItem;

                if (hasItem)
                {
                    foreach (var listener in _listeners)
                    {
                        await listener(result.Item);
                    }
                }
            }
            while (hasItem);
        }

        public void AddResponseRecordListener(Func<Response, Task> onResponseRecordAsync)
        {
            _listeners.Enqueue(onResponseRecordAsync);
        }

        public async Task CompleteAsync()
        {
            _queue.MarkAsComplete();
            await _dequeueTask;
        }

        private async Task<Page> GetPageAsync()
        {
            var browser = await _lazyBrowser.Value;
            var page = await browser.NewPageAsync();

            await page.SetUserAgentAsync(ConnectionString);

            await page.EvaluateOnNewDocumentAsync(@"
function () {
    Object.defineProperty(navigator, 'webdriver', {
        get: () => false,
    });
}");

            await page.SetRequestInterceptionAsync(true);

            page.Request += async (sender, requestEventArgs) =>
            {
                var url = requestEventArgs.Request.Url;

                foreach (var extension in ExcludedExtensions)
                {
                    if (url.EndsWith(extension))
                    {
                        await requestEventArgs.Request.AbortAsync();
                        return;
                    }
                }

                foreach (var substring in ExcludedSubstrings)
                {
                    if (url.Contains(substring))
                    {
                        await requestEventArgs.Request.AbortAsync();
                        return;
                    }
                }

                await requestEventArgs.Request.ContinueAsync();
            };

            return page;
        }

        public void KillOrphanBrowsers()
        {
            _logger.LogInformation("Searching for orphan Chromium processes.");
            var downloadsDirectory = GetDownloadsDirectory();
            var allProcesses = Process.GetProcesses();
            try
            {
                var orphanProcesses = Process
                    .GetProcesses()
                    .Where(x =>
                    {
                        try
                        {
                            return Path.GetFullPath(x.MainModule.FileName).StartsWith(downloadsDirectory);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .OrderBy(x => x.StartTime)
                    .ToList();

                var fileNameList = string.Join(
                    Environment.NewLine,
                    orphanProcesses
                        .Select(x => x.MainModule.FileName)
                        .GroupBy(x => x)
                        .OrderByDescending(x => x.Count())
                        .ThenBy(x => x.Key, StringComparer.Ordinal)
                        .Select(x => $"  - {x.Key} (count: {x.Count()})"));

                _logger.LogInformation(
                    $"Found {{Count}} processes with the following file names:{Environment.NewLine}{{FileNameList}}",
                    orphanProcesses.Count,
                    fileNameList);

                foreach (var process in orphanProcesses)
                {
                    _logger.LogInformation(
                        "Killing process {ProcessId}, which was started on {StartTime}.",
                        process.Id,
                        process.StartTime);
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to kill this process.");
                    }
                }
            }
            finally
            {
                foreach (var process in allProcesses)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch
                    {
                        // Ignore this failure.
                    }
                }
            }
        }

        public async Task InitializeAsync()
        {
            using (var page = await GetPageAsync())
            {
            }
        }

        public async Task<SignInResponse> SignInAsync(string email, string password)
        {
            using (var page = await GetPageAsync())
            using (var captureState = new CaptureState(
                OperationType.SignIn,
                null,
                _queue))
            {
                captureState.CaptureAuthenticationState(page);
                captureState.CaptureSignIn(page);

                await page.GoToAsync(
                    "https://www.kroger.com/signin?redirectUrl=/",
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                await page.TypeAsync("#SignIn-emailInput", email);
                await page.TypeAsync("#SignIn-passwordInput", password);
                await page.ClickAsync("#SignIn-submitButton");

                await page.WaitForNavigationAsync(
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                await captureState.WaitForCompletionAsync();

                return captureState
                    .GetValues<SignInResponse>()
                    .LastOrDefault();
            }
        }

        public async Task<List<Receipt>> GetReceiptSummariesAsync()
        {
            using (var page = await GetPageAsync())
            using (var captureState = new CaptureState(
                OperationType.GetReceiptSummaries,
                null,
                _queue))
            {
                captureState.CaptureAuthenticationState(page);
                captureState.CaptureReceiptSummaryByUserId(page);

                await page.GoToAsync(
                    "https://www.kroger.com/mypurchases",
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                await captureState.WaitForCompletionAsync();

                return captureState
                    .GetValues<List<Receipt>>()
                    .LastOrDefault();
            }
        }

        public string GetReceiptUrl(ReceiptId receiptId)
        {
            var pageUrl = "https://www.kroger.com/mypurchases/detail/" + string.Join("~", new[]
            {
                receiptId.DivisionNumber,
                receiptId.StoreNumber,
                receiptId.TransactionDate,
                receiptId.TerminalNumber,
                receiptId.TransactionId,
            });
            return pageUrl;
        }

        public async Task<Receipt> GetReceiptAsync(ReceiptId receiptId)
        {
            var pageUrl = GetReceiptUrl(receiptId);

            using (var page = await GetPageAsync())
            using (var captureState = new CaptureState(
                OperationType.GetReceipt,
                receiptId,
                _queue))
            {
                captureState.CaptureAuthenticationState(page);
                captureState.CaptureReceiptDetail(page);

                await page.GoToAsync(
                    pageUrl,
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                await captureState.WaitForCompletionAsync();

                return captureState
                    .GetValues<Receipt>()
                    .LastOrDefault();
            }
        }

        public void Dispose()
        {
            _browserForDispose?.Dispose();
        }
    }
}
