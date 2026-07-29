using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Application.Accounts.Shared;
using PixHub.Accounts.Domain.Accounts;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Accounts.GetAccountById;

/// <summary>
/// Vai direto ao lado de leitura: nenhuma instância de <c>Account</c> é materializada, o que evita o
/// custo do change tracker em um caminho que não altera nada.
/// </summary>
internal sealed class GetAccountByIdQueryHandler(IAccountReadRepository accountReadRepository)
    : IQueryHandler<GetAccountByIdQuery, AccountResponse>
{
    public async ValueTask<Result<AccountResponse>> Handle(
        GetAccountByIdQuery query,
        CancellationToken cancellationToken)
    {
        var account = await accountReadRepository.GetByIdAsync(query.AccountId, cancellationToken);

        return account is null
            ? Result.Failure<AccountResponse>(AccountErrors.NotFound(query.AccountId))
            : account;
    }
}
