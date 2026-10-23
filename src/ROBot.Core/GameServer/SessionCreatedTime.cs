using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace ROBot.Core.GameServer
{
    /// <summary>
    /// Reads the session created time the game sends with its session info.
    /// Older games send created.ToString() in the game's own culture, newer ones send ISO 8601 ("o"),
    /// and Newtonsoft may already have turned an ISO string into a date. A value that cannot be read
    /// becomes DateTime.MinValue, which means unknown and must never make a connection lose.
    /// </summary>
    public static class SessionCreatedTime
    {
        public static DateTime Unknown => DateTime.MinValue;

        public static bool IsKnown(DateTime created) => created != Unknown;

        public static bool TryParse(object value, out DateTime created)
        {
            created = Unknown;
            if (value == null)
            {
                return false;
            }

            if (value is JValue jv)
            {
                value = jv.Value;
                if (value == null)
                {
                    return false;
                }
            }

            if (value is DateTime dt)
            {
                created = dt;
                return true;
            }

            if (value is DateTimeOffset dto)
            {
                created = dto.UtcDateTime;
                return true;
            }

            return TryParse(value.ToString(), out created);
        }

        // Date and time layouts older games send from created.ToString() in the streamer's own culture. Parsed
        // with the invariant culture, so they work even when the server has no culture data.
        private static readonly string[] CultureLayouts =
        {
            "M/d/yyyy h:mm:ss tt", "M/d/yyyy H:mm:ss",
            "d/M/yyyy H:mm:ss", "d/M/yyyy h:mm:ss tt",
            "d.M.yyyy H:mm:ss", "d.M.yyyy H.mm.ss",
            "d-M-yyyy H:mm:ss", "yyyy-M-d H:mm:ss", "yyyy/M/d H:mm:ss", "yyyy.M.d H:mm:ss",
            "yyyy-M-d tt h:mm:ss",
        };

        public static bool TryParse(string value, out DateTime created) => TryParse(value, DateTime.Now, out created);

        /// <summary>
        /// ISO 8601 is exact. Anything else may be day first or month first (23/10 is clear, 05/10 is not), so
        /// every layout that reads it is a candidate and the one closest to <paramref name="now"/> wins: a session
        /// was created recently, never more than a day ahead of the server's clock.
        /// </summary>
        public static bool TryParse(string value, DateTime now, out DateTime created)
        {
            created = Unknown;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim();

            if (DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out created))
            {
                return true;
            }

            var found = false;
            var best = Unknown;
            void Consider(DateTime candidate)
            {
                if (candidate > now.AddDays(1))
                {
                    return;
                }

                if (!found || Math.Abs((candidate - now).Ticks) < Math.Abs((best - now).Ticks))
                {
                    best = candidate;
                    found = true;
                }
            }

            foreach (var layout in CultureLayouts)
            {
                if (DateTime.TryParseExact(value, layout, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var candidate))
                {
                    Consider(candidate);
                }
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var invariant))
            {
                Consider(invariant);
            }

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out var current))
            {
                Consider(current);
            }

            created = found ? best : Unknown;
            return found;
        }

        /// <summary>
        /// True when a connection with the incoming created time should be refused because the
        /// existing session is known to be newer. Unknown times never refuse.
        /// </summary>
        public static bool IsOlder(DateTime incoming, DateTime existing)
        {
            if (!IsKnown(incoming) || !IsKnown(existing))
            {
                return false;
            }

            return existing > incoming;
        }
    }
}
