using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace XmlRpc.Client.DataTypes
{
    public static class DateTime8601
    {
        static readonly Regex DateTime8601Regex = new Regex(
            @"(((?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}))|((?<year>\d{4})(?<month>\d{2})(?<day>\d{2})))"
          + @"T"
          + @"(((?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2}))|((?<hour>\d{2})(?<minute>\d{2})(?<second>\d{2})))"
          + @"(?<tz>$|Z|([+-]\d{2}:?(\d{2})?))");

        static readonly string[] Formats = new[]
        {
            "yyyyMMdd'T'HHmmss",
            "yyyyMMdd'T'HHmmss'Z'",
            "yyyyMMdd'T'HHmmsszzz",
            "yyyyMMdd'T'HHmmsszz",
        };

        static public bool TryParseDateTime8601(string date, out DateTime result)
        {
            result = default;

            var m = DateTime8601Regex.Match(date);
            if (m == null)
                return false;

            var normalized = m.Groups["year"].Value + m.Groups["month"].Value + m.Groups["day"].Value
              + "T"
              + m.Groups["hour"].Value + m.Groups["minute"].Value + m.Groups["second"].Value
              + m.Groups["tz"].Value;

            try
            {
                result = DateTime.ParseExact(normalized, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}