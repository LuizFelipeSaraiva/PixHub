using NSubstitute;
using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Accounts.GetAccountById;
using PixHub.Accounts.Application.Accounts.GetAccounts;
using PixHub.Accounts.Application.Accounts.Shared;
using PixHub.Accounts.Domain.Accounts;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Application.Tests.Accounts;

public class QueryHandlerTests
{
    private readonly IAccountReadRepository _readRepository = Substitute.For<IAccountReadRepository>();

    [Fact]
    public async Task GetAccountById_WhenAccountExists_ReturnsTheProjection()
    {
        var accountId = Guid.CreateVersion7();
        var response = new AccountResponse(
            accountId,
            "111.***.***-35",
            "Maria Silva",
            1_250.40m,
            "BRL",
            nameof(AccountStatus.Active),
            DateTimeOffset.UtcNow);
        _readRepository
            .GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(response);
        var handler = new GetAccountByIdQueryHandler(_readRepository);

        var result = await handler.Handle(
            new GetAccountByIdQuery(accountId),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(response);
    }

    [Fact]
    public async Task GetAccountById_WhenAccountIsMissing_FailsWithNotFound()
    {
        var accountId = Guid.CreateVersion7();
        _readRepository
            .GetByIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns((AccountResponse?)null);
        var handler = new GetAccountByIdQueryHandler(_readRepository);

        var result = await handler.Handle(
            new GetAccountByIdQuery(accountId),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.NotFound(accountId));
    }

    [Fact]
    public async Task GetAccounts_ForwardsFilterAndPagingToTheReadSide()
    {
        var page = new PagedList<AccountSummaryResponse>(
            [new AccountSummaryResponse(Guid.CreateVersion7(), "Maria Silva", 10m, nameof(AccountStatus.Active))],
            Page: 2,
            PageSize: 25,
            TotalCount: 30);
        _readRepository
            .GetPagedAsync(AccountStatus.Active, 2, 25, Arg.Any<CancellationToken>())
            .Returns(page);
        var handler = new GetAccountsQueryHandler(_readRepository);

        var result = await handler.Handle(
            new GetAccountsQuery(AccountStatus.Active, 2, 25),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(page);
        await _readRepository.Received(1).GetPagedAsync(AccountStatus.Active, 2, 25, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAccounts_WithNoMatches_SucceedsWithAnEmptyPage()
    {
        // Lista vazia é resultado válido — não deve virar falha.
        _readRepository
            .GetPagedAsync(null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedList<AccountSummaryResponse>([], 1, 20, 0));
        var handler = new GetAccountsQueryHandler(_readRepository);

        var result = await handler.Handle(new GetAccountsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalPages.ShouldBe(0);
        result.Value.HasNextPage.ShouldBeFalse();
    }

    [Theory]
    [InlineData(30, 25, 2)]
    [InlineData(50, 25, 2)]
    [InlineData(51, 25, 3)]
    [InlineData(0, 25, 0)]
    public void PagedList_ComputesTotalPagesByRoundingUp(int totalCount, int pageSize, int expectedTotalPages)
    {
        var page = new PagedList<AccountSummaryResponse>([], 1, pageSize, totalCount);

        page.TotalPages.ShouldBe(expectedTotalPages);
    }
}
