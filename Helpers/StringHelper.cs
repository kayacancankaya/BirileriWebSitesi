using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace BirileriWebSitesi.Helpers
{
    public class StringHelper
    {

       
        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static string GetFirstName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : string.Empty;
        }
        public static string GetLastName(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[^1] : string.Empty;
        }
        public static bool IsValidCardNumber(string number)
        {
            int sum = 0;
            bool shouldDouble = false;

            for (int i = number.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(number[i])) return false;
                int digit = number[i] - '0';
                if (shouldDouble)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                shouldDouble = !shouldDouble;
            }
            return sum % 10 == 0;
        }

        public static bool IsValidExpiry(string month, string year)
        {
            if (!int.TryParse(month, out int m) || !int.TryParse(year, out int y)) return false;
            if (m < 1 || m > 12) return false;

            y = (y < 100) ? 2000 + y : y; // support 2-digit years

            var now = DateTime.Now;
            var expiry = new DateTime(y, m, DateTime.DaysInMonth(y, m));
            return expiry >= now;
        }

        public static bool IsValidCVV(string cvv)
        {
            return Regex.IsMatch(cvv, @"^\d{3,4}$");
        }

        public static string ConvertToTurkishStatus(string statusText)
        {
            switch (statusText)
            {
                case "Pending":
                    return "Beklemede";
                case "Approved":
                    return "Hazırlanıyor";
                case "Shipped":
                    return "Kargolandı";
                case "Delivered":
                    return "Teslim Edildi";
                case "BankTransfer":
                    return "Havale/EFT Bekleniyor";
                case "Cancelled":
                    return "İptal Edildi";
                default:
                    return statusText; // Return original if no match
            }
        }
    }
}
