using System;
using System.Globalization;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace KrogerScrape
{
    public class DatabasePathValueParser : IValueParser<DatabasePath>
    {
        public Type TargetType => typeof(DatabasePath);
        public DatabasePath Parse(string argName, string value, CultureInfo culture) => new DatabasePath();
        object IValueParser.Parse(string argName, string value, CultureInfo culture) => new DatabasePath();
    }
}
