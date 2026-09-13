using PixHub.Accounts.Domain.ValueObjects;

namespace PixHub.Accounts.Infrastructure.Tests;

/// <summary>
/// Gera CPFs matematicamente válidos (dígitos verificadores corretos) para os testes de integração,
/// que rodam contra um Postgres real com índice único em <c>holder_cpf</c> — testes em paralelo
/// precisam de CPFs distintos, e o value object <see cref="Cpf"/> rejeitaria um valor fixo reusado
/// ou uma sequência de dígitos repetidos.
/// </summary>
internal static class TestCpfGenerator
{
    public static Cpf NewValid()
    {
        var digits = new int[9];
        do
        {
            for (var i = 0; i < digits.Length; i++)
            {
                digits[i] = Random.Shared.Next(0, 10);
            }
        } while (digits.Distinct().Count() == 1); // evita sequências repetidas, rejeitadas pelo VO

        var firstCheck = ComputeCheckDigit(digits, 9, 10);
        var secondCheck = ComputeCheckDigit([.. digits, firstCheck], 10, 11);

        return Cpf.Create(string.Concat(digits) + firstCheck + secondCheck).Value;
    }

    private static int ComputeCheckDigit(int[] digits, int length, int startWeight)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += digits[i] * (startWeight - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
