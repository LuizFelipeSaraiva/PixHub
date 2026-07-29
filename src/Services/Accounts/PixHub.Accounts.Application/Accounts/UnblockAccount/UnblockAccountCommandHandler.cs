using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Accounts.UnblockAccount;

internal sealed class UnblockAccountCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UnblockAccountCommand>
{
    public async ValueTask<Result> Handle(UnblockAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(AccountId.From(command.AccountId), cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        var unblockResult = account.Unblock();
        if (unblockResult.IsFailure)
        {
            return unblockResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
