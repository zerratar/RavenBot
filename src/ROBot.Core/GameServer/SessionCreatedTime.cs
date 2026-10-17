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

        public static bool TryParse(string value, out DateTime created)
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

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out created))
            {
                return true;
            }

            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out created))
            {
                return true;
            }

            created = Unknown;
            return false;
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
