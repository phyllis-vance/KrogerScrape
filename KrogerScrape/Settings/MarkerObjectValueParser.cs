using System;
using System.Globalization;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace KrogerScrape.Settings
{
    public class MarkerObjectValueParser<T> : IValueParser<T>
    {
        public Type TargetType => typeof(T);
        public T Parse(string argName, string value, CultureInfo culture) => Activator.CreateInstance<T>();
        object IValueParser.Parse(string argName, string value, CultureInfo culture) => Activator.CreateInstance<T>();
    }
}
