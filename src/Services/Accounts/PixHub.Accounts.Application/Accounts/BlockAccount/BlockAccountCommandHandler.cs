using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Accounts.BlockAccount;

internal sealed class BlockAccountCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<BlockAccountCommand>
{
    public async ValueTask<Result> Handle(BlockAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(AccountId.From(command.AccountId), cancellationToken);
        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(command.AccountId));
        }

        // Bloquear conta já bloqueada é sucesso (o agregado é idempotente aqui), então a gravação
        // segue normalmente — apenas não haverá alteração a persistir.
        var blockResult = account.Block(command.Reason);
        if (blockResult.IsFailure)
        {
            return blockResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
