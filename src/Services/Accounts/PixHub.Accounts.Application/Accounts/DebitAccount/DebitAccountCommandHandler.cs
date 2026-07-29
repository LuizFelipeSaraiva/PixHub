using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Accounts.DebitAccount;

internal sealed class DebitAccountCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DebitAccountCommand>
{
    public async ValueTask<Result> Handle(DebitAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(AccountId.From(command.AccountId), cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        var moneyResult = Money.Create(command.Amount);
        if (moneyResult.IsFailure)
        {
            return Result.Failure(moneyResult.Error);
        }

        // A checagem de saldo pertence ao agregado: é a invariante que protege a conta.
        var debitResult = account.Debit(moneyResult.Value);
        if (debitResult.IsFailure)
        {
            return debitResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
