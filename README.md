# KrogerScrape

Fetch receipt data from Kroger.com.

This tool logs in to Kroger.com and saves all of your receipt information into a database stored on your file system.
Currently, the data is only saving in a SQLite database, so this tool is not very useful yet. Stay tuned for more
features.

As far as I know, this works on other [Kroger-owned grocery stores](https://en.wikipedia.org/wiki/Kroger#Chains) like
Fry's, QFC, Fred Meyer, Ralph's, etc.

## Planned Features

1. Export data to JSON
1. Export data to CSV
1. Generate reports

## Requirements

.NET Core 2.0 or newer.

## Example

```
> dotnet .\KrogerScrape.dll scrape --email plapin@dundermifflinpaper.com
Password: ***********
Only new receipts will be fetched.
Logging in with email address plapin@dundermifflinpaper.com.
Fetching the receipt summaries.
Found 3 receipts. Processing them from oldest to newest.

Fetching receipt 1 of 3.
  URL:  https://www.kroger.com/mypurchases/detail/670~00178~2017-12-18~0100~42
  Date: 2017-12-18
Done. The receipt had 17 items, totaling $42.52.

Fetching receipt 2 of 3.
  URL:  https://www.kroger.com/mypurchases/detail/670~00178~2018-09-20~0100~42
  Date: 2018-09-20
Done. The receipt had 4 items, totaling $7.89.

Fetching receipt 3 of 3.
  URL:  https://www.kroger.com/mypurchases/detail/670~00178~2018-10-23~0100~42
  Date: 2018-10-23
Done. The receipt had 7 items, totaling $53.01.
The scrape command has completed successfully.
```
