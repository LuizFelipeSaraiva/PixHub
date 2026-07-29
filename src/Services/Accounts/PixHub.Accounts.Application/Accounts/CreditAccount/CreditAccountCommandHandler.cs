using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Accounts.CreditAccount;

internal sealed class CreditAccountCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreditAccountCommand>
{
    public async ValueTask<Result> Handle(CreditAccountCommand command, CancellationToken cancellationToken)
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

        // Quem decide se a conta aceita a movimentação é o agregado (situação, sinal do valor).
        var creditResult = account.Credit(moneyResult.Value);
        if (creditResult.IsFailure)
        {
            return creditResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
