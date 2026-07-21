using System.Globalization;

namespace Api.PrinterProd.Configuration
{
    public static class ConfigurationExtensions
    {
        public static string GetRequiredString(this IConfiguration configuration, string key)
        {
            string? value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Configuration key '{key}' is missing or empty.");
            }

            return value;
        }

        public static int GetRequiredInt(this IConfiguration configuration, string key)
        {
            string value = configuration.GetRequiredString(key);
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                throw new InvalidOperationException($"Configuration key '{key}' must be a valid integer.");
            }

            return result;
        }
    }
}
