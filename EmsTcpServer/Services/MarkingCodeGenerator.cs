using EmsTcpServer.Interfaces;

namespace EmsTcpServer.Services;

public class MarkingCodeGenerator : IMarkingCodeGenerator
{
    private readonly Random _random = new();

    public string Generate()
    {
        return $"{GenerateGtin()}{GenerateSerial()}";
    }

    private string GenerateGtin()
    {
        var digits = new char[13];
        for (var i = 0; i < digits.Length; i++)
        {
            digits[i] = (char)('0' + _random.Next(10));
        }
        
        return "01" + new string(digits) + CalculateCheckDigit(digits);
    }

    private string GenerateSerial()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        return "21" + new string(
            Enumerable.Range(0, 10)
                .Select(_ => chars[_random.Next(chars.Length)])
                .ToArray());
    }

    private static char CalculateCheckDigit(ReadOnlySpan<char> digits)
    {
        var sum = 0;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';
            var positionFromRight = digits.Length - i;
            sum += digit * (positionFromRight % 2 == 1 ? 3 : 1);
        }
        return (char)('0' + (10 - sum % 10) % 10);
    }
}
