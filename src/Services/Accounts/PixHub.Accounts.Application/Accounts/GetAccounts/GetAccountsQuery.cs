using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Application.Accounts.Shared;
using PixHub.Accounts.Domain.Accounts;

namespace PixHub.Accounts.Application.Accounts.GetAccounts;

/// <summary>
/// Lista contas de forma paginada, opcionalmente filtrando pela situação.
/// </summary>
/// <param name="Status">Situação a filtrar; <c>null</c> traz todas.</param>
/// <param name="Page">Página desejada, começando em 1.</param>
/// <param name="PageSize">Tamanho da página; limitado pelo validador para proteger o banco.</param>
public sealed record GetAccountsQuery(
    AccountStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedList<AccountSummaryResponse>>;
