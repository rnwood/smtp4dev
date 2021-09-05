using System.Text.RegularExpressions;

namespace Rnwood.Smtp4dev.RulesEngine
{
    public static class Helpers
    {
        public static bool RegexMatch(string check, string regexExpression)
        {
            var regex = new Regex(regexExpression);
            var match = regex.Match(check);
            return match.Success;
        }
    }
}