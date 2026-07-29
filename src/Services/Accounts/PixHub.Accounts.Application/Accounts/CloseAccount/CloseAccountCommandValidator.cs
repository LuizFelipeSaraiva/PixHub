using FluentValidation;

namespace PixHub.Accounts.Application.Accounts.CloseAccount;

internal sealed class CloseAccountCommandValidator : AbstractValidator<CloseAccountCommand>
{
    public CloseAccountCommandValidator() =>
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("O identificador da conta é obrigatório.");
}
