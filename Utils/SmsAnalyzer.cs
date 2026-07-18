using System.Collections.Generic;

namespace SendAfrica.Utils
{
    /// <summary>
    /// SMS text analysis: encoding detection, character count, part/credit
    /// estimation. Ported from the Python SDK's <c>utils/sms.py</c>. Mirrors
    /// real telco billing behavior:
    /// GSM-7 (basic Latin + limited symbols): 160 chars/segment, 153 when concatenated.
    /// UCS-2 (anything outside GSM-7, e.g. emoji, most Swahili diacritics are fine
    /// but emoji/accents force this): 70 chars/segment, 67 when concatenated.
    /// </summary>
    public static class SmsAnalyzer
    {
        // GSM 03.38 basic character set (simplified — covers the common case).
        private const string Gsm7Basic =
            "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ" +
            " !\"#¤%&'()*+,-./0123456789:;<=>?" +
            "¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§" +
            "¿abcdefghijklmnopqrstuvwxyzäöñüà";

        private static readonly HashSet<char> Gsm7Set = new(Gsm7Basic.ToCharArray());

        private const int Gsm7SingleLimit = 160;
        private const int Gsm7ConcatLimit = 153;
        private const int Ucs2SingleLimit = 70;
        private const int Ucs2ConcatLimit = 67;

        /// <summary>
        /// Analyzes an SMS body and estimates encoding, segment count, and credit
        /// cost. No network call — pure local computation. Credit cost is assumed
        /// to be 1 credit per segment/part, matching standard SMS aggregator
        /// billing; adjust if SendAfrica's actual pricing differs.
        /// </summary>
        public static SmsAnalysis Analyze(string text)
        {
            int length = text.Length;
            string encoding;
            int singleLimit, concatLimit;

            if (IsGsm7(text))
            {
                encoding = "GSM-7";
                singleLimit = Gsm7SingleLimit;
                concatLimit = Gsm7ConcatLimit;
            }
            else
            {
                encoding = "UCS-2";
                singleLimit = Ucs2SingleLimit;
                concatLimit = Ucs2ConcatLimit;
            }

            int parts;
            if (length == 0)
            {
                parts = 0;
            }
            else if (length <= singleLimit)
            {
                parts = 1;
            }
            else
            {
                parts = (length + concatLimit - 1) / concatLimit; // ceil division
            }

            int normalizedParts = length > 0 ? System.Math.Max(parts, 1) : 0;

            return new SmsAnalysis
            {
                Encoding = encoding,
                Characters = length,
                Parts = normalizedParts,
                Credits = normalizedParts,
            };
        }

        private static bool IsGsm7(string text)
        {
            foreach (char c in text)
            {
                if (!Gsm7Set.Contains(c)) return false;
            }
            return true;
        }
    }
}
