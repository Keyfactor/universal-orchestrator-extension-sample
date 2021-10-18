using System;

namespace UniversalOrchestratorExtensionSample
{
    public static class Extensions
    {
        public static string EnsureEndsWith(this string s, string ending)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            if (!s.EndsWith(ending))
                return s + ending;

            return s;
        }
    }
}
