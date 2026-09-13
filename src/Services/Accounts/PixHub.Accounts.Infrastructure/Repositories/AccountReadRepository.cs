using Dapper;
using Npgsql;
using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Accounts.Shared;
using PixHub.Accounts.Domain.Accounts;

namespace PixHub.Accounts.Infrastructure.Repositories;

/// <summary>
/// Implementação Dapper da porta de leitura: SQL de mão própria, projetado direto em DTOs, sem
/// passar pelo agregado nem pelo change tracker do EF — o outro lado do CQRS.
/// </summary>
internal sealed class AccountReadRepository(NpgsqlDataSource dataSource) : IAccountReadRepository
{
    public async Task<AccountResponse?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                id as "Id",
                holder_cpf as "HolderCpf",
                holder_name as "HolderName",
                balance_amount as "Balance",
                balance_currency as "Currency",
                status as "Status",
                opened_at_utc as "OpenedAtUtc"
            from accounts.accounts
            where id = @AccountId
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { AccountId = accountId }, cancellationToken: cancellationToken);
        var row = await connection.QueryFirstOrDefaultAsync<AccountRow>(command);

        return row?.ToResponse();
    }

    public async Task<PagedList<AccountSummaryResponse>> GetPagedAsync(
        AccountStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // count(*) over() traz o total junto de cada linha da página, em uma única ida ao banco —
        // dispensa uma segunda query "select count(*)" com o mesmo filtro.
        const string sql = """
            select
                id as "Id",
                holder_name as "HolderName",
                balance_amount as "Balance",
                status as "Status",
                count(*) over() as "TotalCount"
            from accounts.accounts
            where @Status is null or status = @Status
            order by opened_at_utc desc
            offset @Offset limit @PageSize
            """;

        var parameters = new
        {
            Status = status?.ToString(),
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var rows = (await connection.QueryAsync<AccountSummaryRow>(command)).AsList();

        var totalCount = rows.Count > 0 ? checked((int)rows[0].TotalCount) : 0;
        var items = rows.ConvertAll(row => row.ToResponse());

        return new PagedList<AccountSummaryResponse>(items, page, pageSize, totalCount);
    }

    /// <summary>Máscara de CPF replicada do value object — o lado de leitura não toca em tipos de domínio de propósito.</summary>
    private static string MaskCpf(string digits) => $"{digits[..3]}.***.***-{digits[9..]}";

    private sealed record AccountRow(
        Guid Id,
        string HolderCpf,
        string HolderName,
        decimal Balance,
        string Currency,
        string Status,
        DateTime OpenedAtUtc) // Npgsql materializa "timestamptz" como DateTime (Kind Utc); o binding
                              // por construtor do Dapper exige o tipo exato devolvido pelo reader.
    {
        public AccountResponse ToResponse() => new(
            Id,
            MaskCpf(HolderCpf),
            HolderName,
            Balance,
            Currency,
            Status,
            new DateTimeOffset(OpenedAtUtc, TimeSpan.Zero));
    }

    private sealed record AccountSummaryRow(Guid Id, string HolderName, decimal Balance, string Status, long TotalCount)
    // count(*) over() devolve bigint no Postgres; Npgsql materializa como long, e o binding por
    // construtor do Dapper exige o tipo exato devolvido pelo reader (mesmo motivo do DateTime acima).
    {
        public AccountSummaryResponse ToResponse() => new(Id, HolderName, Balance, Status);
    }
}
