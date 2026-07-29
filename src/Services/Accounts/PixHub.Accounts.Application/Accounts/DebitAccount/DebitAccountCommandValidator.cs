using FluentValidation;

namespace PixHub.Accounts.Application.Accounts.DebitAccount;

internal sealed class DebitAccountCommandValidator : AbstractValidator<DebitAccountCommand>
{
    public DebitAccountCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("O identificador da conta é obrigatório.");

        RuleFor(command => command.Amount)
            .GreaterThan(0m).WithMessage("O valor do débito deve ser positivo.");
    }
}
