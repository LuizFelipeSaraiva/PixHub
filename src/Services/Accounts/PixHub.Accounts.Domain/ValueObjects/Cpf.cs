using System.Text.RegularExpressions;
using PixHub.BuildingBlocks.Domain.Primitives;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Domain.ValueObjects;

/// <summary>
/// Brazilian taxpayer id (CPF). Constructed only through <see cref="Create"/>, which normalizes the
/// input to 11 digits and validates the two check digits — so an invalid CPF cannot exist in the domain.
/// </summary>
public sealed partial class Cpf : ValueObject
{
    private Cpf(string digits) => Value = digits;

    /// <summary>The 11 normalized digits (no punctuation).</summary>
    public string Value { get; }

    /// <summary>Formatted for display: <c>123.456.789-09</c>.</summary>
    public string Formatted => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    /// <summary>PII-safe partial mask for logs/audit: <c>123.***.***-09</c>.</summary>
    public string Masked => $"{Value[..3]}.***.***-{Value[9..]}";

    public static Result<Cpf> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return CpfErrors.Empty;
        }

        var digits = NonDigits().Replace(input, string.Empty);

        if (digits.Length != 11)
        {
            return CpfErrors.InvalidLength;
        }

        return IsValid(digits) ? new Cpf(digits) : CpfErrors.InvalidCheckDigits;
    }

    private static bool IsValid(string cpf)
    {
        // Reject sequences of a single repeated digit (000..., 111..., ...) which pass the math but are invalid.
        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var firstCheck = ComputeCheckDigit(cpf, 9, 10);
        if (firstCheck != cpf[9] - '0')
        {
            return false;
        }

        var secondCheck = ComputeCheckDigit(cpf, 10, 11);
        return secondCheck == cpf[10] - '0';
    }

    private static int ComputeCheckDigit(string cpf, int length, int startWeight)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += (cpf[i] - '0') * (startWeight - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Formatted;

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigits();
}

public static class CpfErrors
{
    public static readonly Error Empty = Error.Validation(
        "Cpf.Empty", "O CPF é obrigatório.");

    public static readonly Error InvalidLength = Error.Validation(
        "Cpf.InvalidLength", "O CPF deve conter 11 dígitos.");

    public static readonly Error InvalidCheckDigits = Error.Validation(
        "Cpf.InvalidCheckDigits", "O CPF informado é inválido.");
}
