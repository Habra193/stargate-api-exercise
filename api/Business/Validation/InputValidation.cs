using System.Text.RegularExpressions;

namespace StargateAPI.Business.Validation
{
    public static class InputValidation
    {
        private static readonly Regex NamePattern = new(@"^[A-Za-z0-9 '-]+$", RegexOptions.Compiled);
        private static readonly Regex PlainTextPattern = new(@"^[A-Za-z0-9 ]+$", RegexOptions.Compiled);

        public static bool IsValidPersonName(string value)
        {
            return NamePattern.IsMatch(value);
        }

        public static bool IsValidRankOrDutyTitle(string value)
        {
            return PlainTextPattern.IsMatch(value);
        }
    }
}
