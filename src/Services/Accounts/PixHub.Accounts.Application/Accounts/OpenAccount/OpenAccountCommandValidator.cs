using FluentValidation;

namespace PixHub.Accounts.Application.Accounts.OpenAccount;

/// <summary>
/// Validação de formato da entrada. Cuida apenas do que dá para julgar olhando o comando isolado —
/// a validade do CPF (dígitos verificadores) fica no value object, e a unicidade, no handler, pois
/// dependem respectivamente do domínio e do repositório.
/// </summary>
internal sealed class OpenAccountCommandValidator : AbstractValidator<OpenAccountCommand>
{
    private const int MaxHolderNameLength = 200;

    public OpenAccountCommandValidator()
    {
        RuleFor(command => command.HolderCpf)
            .NotEmpty().WithMessage("O CPF é obrigatório.");

        RuleFor(command => command.HolderName)
            .NotEmpty().WithMessage("O nome do titular é obrigatório.")
            .MaximumLength(MaxHolderNameLength)
                .WithMessage($"O nome do titular deve ter no máximo {MaxHolderNameLength} caracteres.");

        RuleFor(command => command.OpeningBalance!.Value)
            .GreaterThanOrEqualTo(0m).WithMessage("O saldo de abertura não pode ser negativo.")
            .When(command => command.OpeningBalance.HasValue);
    }
}
