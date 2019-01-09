using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
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

        private readonly Deserializer _deserializer;
        private readonly IKrogerClientSettings _settings;
        private readonly ILogger<KrogerClient> _logger;
        private readonly ConcurrentQueue<Func<Response, Task>> _listeners;
        private readonly Lazy<Task<Browser>> _lazyBrowser;
        private Browser _browserForDispose;
        private bool _disposed;
        private bool _signedIn;

        public KrogerClient(
            Deserializer deserializer,
            IKrogerClientSettings settings,
            ILogger<KrogerClient> logger)
        {
            _deserializer = deserializer;
            _settings = settings;
            _logger = logger;
            _listeners = new ConcurrentQueue<Func<Response, Task>>();
            _lazyBrowser = new Lazy<Task<Browser>>(async () =>
            {
                var  revisionInfo = await EnsureDownloadsAsync();

                _logger.LogDebug($"Launching Chromium from:{Environment.NewLine}{{ExecutablePath}}", revisionInfo.ExecutablePath);
                var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    ExecutablePath = revisionInfo.ExecutablePath,
                    Headless = true,
                    DumpIO = _settings.Debug,
                });
                _browserForDispose = browser;
                return browser;
            });
        }

        private string GetDownloadsDirectory()
        {
            return Path.GetFullPath(Path.Combine(_settings.DownloadsPath, "Chromium"));
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
                    $"Downloading Chromium revision {{Revision}} for {{Platform}} to:{Environment.NewLine}{{FolderPath}}",
                    revisionInfo.Revision,
                    revisionInfo.Platform,
                    revisionInfo.FolderPath);

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

                if (contentLength.HasValue)
                {
                    _logger.LogInformation("Download progress: {Percentage}%", 100);
                }

                _logger.LogInformation("Chromium is done downloading.");
            }

            return revisionInfo;
        }

        public void AddResponseRecordListener(Func<Response, Task> onResponseRecordAsync)
        {
            _listeners.Enqueue(onResponseRecordAsync);
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

            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1024,
                Height = 768,
            });

            await page.SetRequestInterceptionAsync(true);

            page.Request += async (sender, requestEventArgs) =>
            {
                if (!_settings.Debug)
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
                }

                await requestEventArgs.Request.ContinueAsync();
            };

            return page;
        }

        public void KillOrphanBrowsers()
        {
            _logger.LogDebug("Searching for orphan Chromium processes.");
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


                if (!orphanProcesses.Any())
                {
                    _logger.LogDebug("None were found.");
                }
                else
                {
                    var fileNameList = string.Join(
                        Environment.NewLine,
                        orphanProcesses
                            .Select(x => x.MainModule.FileName)
                            .GroupBy(x => x)
                            .OrderByDescending(x => x.Count())
                            .ThenBy(x => x.Key, StringComparer.Ordinal)
                            .Select(x => $"{x.Key} (count: {x.Count()})"));

                    _logger.LogWarning(
                        $"Found {{Count}} orphan Chromium processes with the following file names:{Environment.NewLine}{{FileNameList}}",
                        orphanProcesses.Count,
                        fileNameList);

                    foreach (var process in orphanProcesses)
                    {
                        if (process.HasExited)
                        {
                            _logger.LogWarning("Process {ProcessId} has already exited.", process.Id);
                        }
                        else
                        {
                            _logger.LogWarning(
                               "Stopping process {ProcessId}, which was started on {StartTime}.",
                               process.Id,
                               process.StartTime);
                            try
                            {
                                process.Kill();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Failed to stop process {ProcessId}.", process.Id);
                            }
                        }
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
            ThrowIfDisposed();

            using (var page = await GetPageAsync())
            {
            }
        }

        public async Task<DeserializedResponse<SignInResponse>> SignInAsync(CancellationToken token)
        {
            ThrowIfDisposed();
            if (_signedIn)
            {
                throw new InvalidOperationException("The user is already signed in.");
            }

            using (var page = await GetPageAsync())
            using (var captureState = new CaptureState(
                _deserializer,
                OperationType.SignIn,
                null,
                _listeners.ToList(),
                _logger))
            {
                captureState.CaptureAuthenticationState(page);
                captureState.CaptureSignIn(page);

                await page.GoToAsync(
                    "https://www.kroger.com/signin?redirectUrl=/",
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                token.ThrowIfCancellationRequested();

                await CaptureScreenshotIfDebugAsync(page, $"{nameof(SignInAsync)}-Before");

                await page.TypeAsync("#SignIn-emailInput", _settings.Email);
                await page.TypeAsync("#SignIn-passwordInput", _settings.Password);
                await page.ClickAsync("#SignIn-submitButton");

                await page.WaitForNavigationAsync(
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                await CaptureScreenshotIfDebugAsync(page, $"{nameof(SignInAsync)}-After");

                token.ThrowIfCancellationRequested();

                await captureState.WaitForCompletionAsync();

                var response = captureState
                    .GetValues<SignInResponse>()
                    .LastOrDefault();

                _signedIn = response?.Response.AuthenticationState?.Authenticated == true;

                return response;
            }
        }

        private async Task CaptureScreenshotIfDebugAsync(Page page, string name)
        {
            if (!_settings.Debug)
            {
                return;
            }

            var fileName = $"{DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss-fffffff")}-{name}.png";
            var directory = Path.Combine(_settings.DownloadsPath, "Screenshots");
            var path = Path.Combine(directory, fileName);

            _logger.LogDebug(
                $"Capturing a screenshot of URL:{Environment.NewLine}" +
                $"{{PageUrl}}{Environment.NewLine}" +
                $"The screenshot will be written to:{Environment.NewLine}" +
                $"{{ScreenshotPath}}",
                page.Url,
                path);

            Directory.CreateDirectory(directory);

            try
            {
                await page.ScreenshotAsync(path, new ScreenshotOptions
                {
                    FullPage = true,
                    Type = ScreenshotType.Png,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The screenshot could not be captured.");
            }
        }

        public async Task<DeserializedResponse<List<Receipt>>> GetReceiptSummariesAsync(CancellationToken token)
        {
            ThrowIfDisposed();
            ThrowIfNotSignedIn();

            using (var page = await GetPageAsync())
            using (var captureState = new CaptureState(
                _deserializer,
                OperationType.GetReceiptSummaries,
                null,
                _listeners.ToList(),
                _logger))
            {
                captureState.CaptureAuthenticationState(page);
                captureState.CaptureReceiptSummaryByUserId(page);

                await page.GoToAsync(
                    "https://www.kroger.com/mypurchases",
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                await CaptureScreenshotIfDebugAsync(
                    page,
                    nameof(GetReceiptSummariesAsync));

                await captureState.WaitForCompletionAsync();

                return captureState
                    .GetValues<List<Receipt>>()
                    .LastOrDefault();
            }
        }

        public string GetReceiptUrl(ReceiptId receiptId)
        {
            var pageUrl = "https://www.kroger.com/mypurchases/detail/" + string.Join("~", GetReceiptPieces(receiptId));
            return pageUrl;
        }

        private static string[] GetReceiptPieces(ReceiptId receiptId)
        {
            return new[]
            {
                receiptId.DivisionNumber,
                receiptId.StoreNumber,
                receiptId.TransactionDate,
                receiptId.TerminalNumber,
                receiptId.TransactionId,
            };
        }

        public async Task<DeserializedResponse<Receipt>> GetReceiptAsync(ReceiptId receiptId, CancellationToken token)
        {
            ThrowIfDisposed();
            ThrowIfNotSignedIn();

            var pageUrl = GetReceiptUrl(receiptId);

            using (var page = await GetPageAsync())
            using (var captureState = new CaptureState(
                _deserializer,
                OperationType.GetReceipt,
                receiptId,
                _listeners.ToList(),
                _logger))
            {
                captureState.CaptureAuthenticationState(page);
                captureState.CaptureReceiptDetail(page);

                await page.GoToAsync(
                    pageUrl,
                    new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    });

                token.ThrowIfCancellationRequested();

                await CaptureScreenshotIfDebugAsync(
                    page,
                    $"{nameof(GetReceiptAsync)}-{string.Join("_", GetReceiptPieces(receiptId))}");

                await captureState.WaitForCompletionAsync();

                return captureState
                    .GetValues<Receipt>()
                    .LastOrDefault();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(KrogerClient));
            }
        }

        private void ThrowIfNotSignedIn()
        {
            if (!_signedIn)
            {
                throw new InvalidOperationException("The user is not signed in.");
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _browserForDispose?.Dispose();
        }
    }
}
