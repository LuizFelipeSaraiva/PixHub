using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Application.Accounts.Shared;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Accounts.GetAccounts;

internal sealed class GetAccountsQueryHandler(IAccountReadRepository accountReadRepository)
    : IQueryHandler<GetAccountsQuery, PagedList<AccountSummaryResponse>>
{
    public async ValueTask<Result<PagedList<AccountSummaryResponse>>> Handle(
        GetAccountsQuery query,
        CancellationToken cancellationToken)
    {
        // Uma lista vazia é um resultado legítimo, não uma falha — por isso não há "NotFound" aqui.
        var page = await accountReadRepository.GetPagedAsync(
            query.Status,
            query.Page,
            query.PageSize,
            cancellationToken);

        return page;
    }
}
