namespace SendAfrica.Utils
{
    /// <summary>Small, dependency-free validators shared across resources. Ported from the Python SDK's <c>utils/validators.py</c>.</summary>
    public static class Validators
    {
        /// <summary>GSM alphanumeric sender ID cap used by most aggregators.</summary>
        public const int MaxSenderIdLength = 11;

        /// <summary>Throws <see cref="ValidationException"/> if <paramref name="value"/> is null or blank.</summary>
        public static string Require(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException($"'{fieldName}' is required");
            }
            return value!;
        }

        /// <summary>Trims and validates an optional sender ID. Returns null for null/blank input.</summary>
        public static string? ValidateSenderId(string? sender)
        {
            if (sender is null) return null;

            sender = sender.Trim();
            if (sender.Length == 0) return null;

            if (sender.Length > MaxSenderIdLength)
            {
                throw new ValidationException($"sender id '{sender}' exceeds {MaxSenderIdLength} characters");
            }

            return sender;
        }

        /// <summary>Throws <see cref="ValidationException"/> if <paramref name="amount"/> is not positive.</summary>
        public static int ValidatePositiveAmount(int amount, string fieldName = "amount")
        {
            if (amount <= 0)
            {
                throw new ValidationException($"'{fieldName}' must be a positive number");
            }
            return amount;
        }
    }
}
