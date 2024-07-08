using System;
using System.Runtime.CompilerServices;

namespace RavenBot.Core
{
    public static class GameVersion
    {
        public static Version Parse(string input)
        {
            TryParse(input, out var value);
            return value ?? new Version();
        }

        public static bool TryParse(string input, out Version version)
        {
            if (string.IsNullOrEmpty(input))
            {
                version = new Version();
                return false;
            }

            var versionString = input.ToLower().Replace("a-alpha", "").Replace("v", "").Replace("a", "").Replace("b", "");
            return System.Version.TryParse(versionString, out version);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLessThanOrEquals(string version, string comparison)
        {
            if (!TryParse(version, out var src))
                return false;
            if (!TryParse(comparison, out var dst))
                return false;
            return src <= dst;
        }
    }
}