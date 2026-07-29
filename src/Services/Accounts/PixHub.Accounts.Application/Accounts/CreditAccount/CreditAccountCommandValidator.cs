using FluentValidation;

namespace PixHub.Accounts.Application.Accounts.CreditAccount;

internal sealed class CreditAccountCommandValidator : AbstractValidator<CreditAccountCommand>
{
    public CreditAccountCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("O identificador da conta é obrigatório.");

        RuleFor(command => command.Amount)
            .GreaterThan(0m).WithMessage("O valor do crédito deve ser positivo.");
    }
}
