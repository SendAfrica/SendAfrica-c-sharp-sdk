using System.Text.RegularExpressions;

namespace SendAfrica.Utils
{
    /// <summary>
    /// Phone number validation and normalization. Focused on Tanzanian mobile
    /// numbers (SendAfrica's primary market) but accepts any number that can be
    /// normalized to E.164. Ported from the Python SDK's <c>utils/phone.py</c>.
    /// </summary>
    public static class PhoneUtil
    {
        // Tanzanian mobile prefixes after the leading 0 is stripped (e.g. "712345678").
        // Covers Vodacom, Tigo/Mixx, Airtel, Halotel, TTCL ranges in common use.
        private static readonly Regex TzMobilePrefix = new(@"^(6|7)\d{8}$", RegexOptions.Compiled);
        private const string DefaultCountryCode = "255";
        private static readonly Regex NonDigitOrPlus = new(@"[^\d+]", RegexOptions.Compiled);

        /// <summary>
        /// Normalizes a phone number to E.164 (e.g. "+255712345678"). Accepts
        /// formats like "0712345678", "712345678", "255712345678", "+255712345678",
        /// "+255 712 345 678". Throws <see cref="InvalidPhoneException"/> if the
        /// number cannot be confidently normalized.
        /// </summary>
        public static string NormalizePhone(string? raw, string defaultCountryCode = DefaultCountryCode)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidPhoneException($"Phone number must be a non-empty string, got '{raw}'");
            }

            string digits = NonDigitOrPlus.Replace(raw!.Trim(), "");

            string candidate;
            if (digits.StartsWith("+"))
            {
                candidate = digits.Substring(1);
            }
            else if (digits.StartsWith("00"))
            {
                candidate = digits.Substring(2);
            }
            else if (digits.StartsWith("0"))
            {
                candidate = defaultCountryCode + digits.Substring(1);
            }
            else if (digits.StartsWith(defaultCountryCode))
            {
                candidate = digits;
            }
            else if (TzMobilePrefix.IsMatch(digits))
            {
                candidate = defaultCountryCode + digits;
            }
            else
            {
                candidate = digits;
            }

            if (candidate.Length == 0 || !IsAllDigits(candidate) || candidate.Length < 9 || candidate.Length > 15)
            {
                throw new InvalidPhoneException($"'{raw}' is not a valid phone number");
            }

            return "+" + candidate;
        }

        /// <summary>Returns true if <paramref name="raw"/> normalizes to a plausible Tanzanian mobile number.</summary>
        public static bool IsValidTzMobile(string? raw)
        {
            string normalized;
            try
            {
                normalized = NormalizePhone(raw);
            }
            catch (InvalidPhoneException)
            {
                return false;
            }

            string prefix = "+" + DefaultCountryCode;
            if (!normalized.StartsWith(prefix))
            {
                return false;
            }

            string localPart = normalized.Substring(prefix.Length);
            return TzMobilePrefix.IsMatch(localPart);
        }

        private static bool IsAllDigits(string value)
        {
            foreach (char c in value)
            {
                if (c < '0' || c > '9') return false;
            }
            return true;
        }
    }
}
