using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Accounts.CloseAccount;

internal sealed class CloseAccountCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseAccountCommand>
{
    public async ValueTask<Result> Handle(CloseAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(AccountId.From(command.AccountId), cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        var closeResult = account.Close();
        if (closeResult.IsFailure)
        {
            return closeResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
