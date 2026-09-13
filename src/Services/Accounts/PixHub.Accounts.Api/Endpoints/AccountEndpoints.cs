using Mediator;
using PixHub.Accounts.Api.Contracts;
using PixHub.Accounts.Api.Extensions;
using PixHub.Accounts.Application.Accounts.BlockAccount;
using PixHub.Accounts.Application.Accounts.CloseAccount;
using PixHub.Accounts.Application.Accounts.CreditAccount;
using PixHub.Accounts.Application.Accounts.DebitAccount;
using PixHub.Accounts.Application.Accounts.GetAccountById;
using PixHub.Accounts.Application.Accounts.GetAccounts;
using PixHub.Accounts.Application.Accounts.OpenAccount;
using PixHub.Accounts.Application.Accounts.UnblockAccount;
using PixHub.Accounts.Domain.Accounts;

namespace PixHub.Accounts.Api.Endpoints;

/// <summary>Mapeia o recurso REST <c>/api/accounts</c> — cada rota só traduz HTTP em uma mensagem para o <see cref="ISender"/>.</summary>
internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapPost("/", OpenAccountAsync)
            .WithName("OpenAccount")
            .WithSummary("Abre uma nova conta digital.");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAccountById")
            .WithSummary("Consulta uma conta pelo identificador.");

        group.MapGet("/", GetPagedAsync)
            .WithName("GetAccounts")
            .WithSummary("Lista contas paginadas, opcionalmente filtrando por situação.");

        group.MapPost("/{id:guid}/credit", CreditAsync)
            .WithName("CreditAccount")
            .WithSummary("Credita um valor na conta.");

        group.MapPost("/{id:guid}/debit", DebitAsync)
            .WithName("DebitAccount")
            .WithSummary("Debita um valor da conta.");

        group.MapPost("/{id:guid}/block", BlockAsync)
            .WithName("BlockAccount")
            .WithSummary("Bloqueia a conta, impedindo novas movimentações.");

        group.MapPost("/{id:guid}/unblock", UnblockAsync)
            .WithName("UnblockAccount")
            .WithSummary("Reativa uma conta bloqueada.");

        group.MapPost("/{id:guid}/close", CloseAsync)
            .WithName("CloseAccount")
            .WithSummary("Encerra a conta (exige saldo zero).");

        return app;
    }

    private static async Task<IResult> OpenAccountAsync(
        OpenAccountCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToApiResult(id => Results.CreatedAtRoute("GetAccountById", new { id }, new { id }));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountByIdQuery(id), cancellationToken);
        return result.ToApiResult(Results.Ok);
    }

    private static async Task<IResult> GetPagedAsync(
        ISender sender,
        CancellationToken cancellationToken,
        AccountStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetAccountsQuery(status, page, pageSize), cancellationToken);
        return result.ToApiResult(Results.Ok);
    }

    private static async Task<IResult> CreditAsync(
        Guid id, AmountRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreditAccountCommand(id, request.Amount), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> DebitAsync(
        Guid id, AmountRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DebitAccountCommand(id, request.Amount), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> BlockAsync(
        Guid id, BlockRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new BlockAccountCommand(id, request.Reason), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> UnblockAsync(
        Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnblockAccountCommand(id), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> CloseAsync(
        Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CloseAccountCommand(id), cancellationToken);
        return result.ToApiResult();
    }
}
