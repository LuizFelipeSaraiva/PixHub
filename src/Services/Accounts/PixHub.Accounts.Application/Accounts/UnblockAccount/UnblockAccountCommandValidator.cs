using FluentValidation;

namespace PixHub.Accounts.Application.Accounts.UnblockAccount;

internal sealed class UnblockAccountCommandValidator : AbstractValidator<UnblockAccountCommand>
{
    public UnblockAccountCommandValidator() =>
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("O identificador da conta é obrigatório.");
}
