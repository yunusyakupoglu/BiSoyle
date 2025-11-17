using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace BiSoyle.Tenant.Service.Services;

public class VirtualPosService
{
    private static readonly Dictionary<string, string> BinBankMapping = new()
    {
        { "454360", "Garanti BBVA" },
        { "552608", "Garanti BBVA" },
        { "979206", "Garanti BBVA" },
        { "589004", "Akbank" },
        { "557113", "Akbank" },
        { "552633", "Akbank" },
        { "542119", "Yapı Kredi" },
        { "979202", "Yapı Kredi" },
        { "493854", "İş Bankası" },
        { "540134", "İş Bankası" },
        { "402277", "Ziraat Bankası" },
        { "979200", "Ziraat Bankası" },
        { "552879", "Halkbank" },
        { "676274", "Halkbank" },
        { "547961", "QNB Finansbank" },
        { "552830", "QNB Finansbank" },
        { "520977", "ING Bank" },
        { "454313", "DenizBank" },
        { "516949", "TEB" },
        { "639002", "VakıfBank" }
    };

    public IReadOnlyCollection<string> SupportedBanks =>
        BinBankMapping.Values
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

    public string NormalizeCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return string.Empty;

        return new string(cardNumber.Where(char.IsDigit).ToArray());
    }

    public bool IsValidCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return false;

        cardNumber = NormalizeCardNumber(cardNumber);

        if (cardNumber.Length < 12 || cardNumber.Length > 19)
            return false;

        var sum = 0;
        var alternate = false;

        for (var i = cardNumber.Length - 1; i >= 0; i--)
        {
            var digit = cardNumber[i] - '0';
            if (digit < 0 || digit > 9)
                return false;

            if (alternate)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    public void EnsureValidExpiry(string month, string year)
    {
        if (!int.TryParse(month, out var expiryMonth) || expiryMonth < 1 || expiryMonth > 12)
        {
            throw new ArgumentException("Geçersiz son kullanma ayı", nameof(month));
        }

        if (!int.TryParse(year, out var expiryYear))
        {
            throw new ArgumentException("Geçersiz son kullanma yılı", nameof(year));
        }

        if (expiryYear < 100)
        {
            expiryYear += 2000;
        }

        var lastDayOfMonth = new DateTime(expiryYear, expiryMonth, DateTime.DaysInMonth(expiryYear, expiryMonth), 23, 59, 59, DateTimeKind.Utc);
        if (lastDayOfMonth < DateTime.UtcNow)
        {
            throw new ArgumentException("Kartın son kullanma tarihi geçmiş", nameof(year));
        }
    }

    public void EnsureValidCvv(string cvv)
    {
        if (string.IsNullOrWhiteSpace(cvv) || cvv.Length < 3 || cvv.Length > 4 || !cvv.All(char.IsDigit))
        {
            throw new ArgumentException("CVV kodu 3 veya 4 haneli olmalıdır", nameof(cvv));
        }
    }

    public (decimal rate, decimal commission) CalculateCommission(decimal amount, int installment)
    {
        if (amount <= 0)
            throw new ArgumentException("Tutar 0'dan büyük olmalıdır", nameof(amount));

        if (installment < 1)
            installment = 1;

        decimal rate = installment switch
        {
            1 => 0.018m,
            2 => 0.0195m,
            3 => 0.0215m,
            4 => 0.023m,
            6 => 0.026m,
            9 => 0.0295m,
            12 => 0.0325m,
            _ => 0.03m
        };

        var commission = Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
        return (rate, commission);
    }

    public string DetectBank(string cardNumber)
    {
        cardNumber = NormalizeCardNumber(cardNumber);
        if (cardNumber.Length < 6)
            return "Bilinmeyen Banka";

        var bin = cardNumber.Substring(0, 6);
        return BinBankMapping.TryGetValue(bin, out var bank) ? bank : "Bilinmeyen Banka";
    }

    public string MaskCard(string cardNumber)
    {
        cardNumber = NormalizeCardNumber(cardNumber);
        if (cardNumber.Length < 8)
            return "**** **** **** ****";

        var first4 = cardNumber.Substring(0, 4);
        var last4 = cardNumber.Substring(cardNumber.Length - 4);
        return $"{first4} **** **** {last4}";
    }

    public string GenerateReference()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(4);
        var randomValue = BitConverter.ToUInt32(randomBytes, 0) % 100000;
        return $"VP{DateTime.UtcNow:yyyyMMddHHmmss}{randomValue:D5}";
    }

    public string GenerateProvisionCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(3);
        var code = BitConverter.ToUInt32(new[] { bytes[0], bytes[1], bytes[2], (byte)0 }, 0) % 1000000;
        return code.ToString("D6");
    }

    public string GenerateTransactionNumber()
    {
        var number = RandomNumberGenerator.GetBytes(6);
        return Convert.ToHexString(number);
    }
}















