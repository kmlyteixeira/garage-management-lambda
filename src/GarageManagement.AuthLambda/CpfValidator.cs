using System.Text.RegularExpressions;

namespace GarageManagement.AuthLambda;

public static partial class CpfValidator
{
    public static string? NormalizeAndValidate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = NonDigitsRegex().Replace(value, string.Empty);
        if (normalized.Length != 11 || normalized.Distinct().Count() == 1)
        {
            return null;
        }

        if (!HasValidCheckDigits(normalized))
        {
            return null;
        }

        return normalized;
    }

    private static bool HasValidCheckDigits(string cpf)
    {
        var firstDigit = CalculateCheckDigit(cpf[..9], 10);
        var secondDigit = CalculateCheckDigit(cpf[..10], 11);
        return cpf[9] - '0' == firstDigit && cpf[10] - '0' == secondDigit;
    }

    private static int CalculateCheckDigit(string value, int weight)
    {
        var sum = value.Sum(character => (character - '0') * weight--);
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    [GeneratedRegex("[^0-9]")]
    private static partial Regex NonDigitsRegex();
}
