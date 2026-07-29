using FluentValidation;

namespace PixHub.Accounts.Application.Accounts.BlockAccount;

internal sealed class BlockAccountCommandValidator : AbstractValidator<BlockAccountCommand>
{
    private const int MaxReasonLength = 500;

    public BlockAccountCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty().WithMessage("O identificador da conta é obrigatório.");

        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("O motivo do bloqueio é obrigatório.")
            .MaximumLength(MaxReasonLength)
                .WithMessage($"O motivo do bloqueio deve ter no máximo {MaxReasonLength} caracteres.");
    }
}
