using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using PixHub.Accounts.Infrastructure.Repositories;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Infrastructure.Tests.Repositories;

/// <summary>Exercita <see cref="AccountReadRepository"/> — o SQL de mão própria (Dapper) do lado de leitura do CQRS.</summary>
public sealed class AccountReadRepositoryTests(AccountsDatabaseFixture fixture)
    : IClassFixture<AccountsDatabaseFixture>
{
    [Fact]
    public async Task GetByIdAsync_DeveProjetarAContaComCpfMascarado()
    {
        var cpf = Cpf.Create("111.444.777-35").Value;
        var account = Account.Open(cpf, "Fernanda Lima", Money.Create(250.50m).Value).Value;

        await using (var dbContext = fixture.CreateDbContext())
        {
            new PixHub.Accounts.Infrastructure.Repositories.AccountRepository(dbContext).Add(account);
            await new UnitOfWork(dbContext).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var readRepository = new AccountReadRepository(fixture.DataSource);
        var response = await readRepository.GetByIdAsync(account.Id.Value, TestContext.Current.CancellationToken);

        response.ShouldNotBeNull();
        response.HolderName.ShouldBe("Fernanda Lima");
        response.HolderCpfMasked.ShouldBe("111.***.***-35");
        response.Balance.ShouldBe(250.50m);
        response.Status.ShouldBe(nameof(AccountStatus.Active));
    }

    [Fact]
    public async Task GetByIdAsync_QuandoContaNaoExiste_DeveDevolverNull()
    {
        var readRepository = new AccountReadRepository(fixture.DataSource);

        var response = await readRepository.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        response.ShouldBeNull();
    }

    [Fact]
    public async Task GetPagedAsync_DeveFiltrarPorSituacaoEPaginar()
    {
        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new PixHub.Accounts.Infrastructure.Repositories.AccountRepository(dbContext);
            for (var i = 0; i < 3; i++)
            {
                repository.Add(Account.Open(TestCpfGenerator.NewValid(), $"Cliente Ativo {i}").Value);
            }

            var blocked = Account.Open(TestCpfGenerator.NewValid(), "Cliente Bloqueado").Value;
            blocked.Block("teste");
            repository.Add(blocked);

            await new UnitOfWork(dbContext).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var readRepository = new AccountReadRepository(fixture.DataSource);

        var blockedPage = await readRepository.GetPagedAsync(
            AccountStatus.Blocked, page: 1, pageSize: 20, TestContext.Current.CancellationToken);
        blockedPage.TotalCount.ShouldBe(1);
        blockedPage.Items.ShouldHaveSingleItem();
        blockedPage.Items[0].HolderName.ShouldBe("Cliente Bloqueado");

        var firstPage = await readRepository.GetPagedAsync(
            status: null, page: 1, pageSize: 2, TestContext.Current.CancellationToken);
        firstPage.Items.Count.ShouldBe(2);
        firstPage.HasNextPage.ShouldBeTrue();
        firstPage.HasPreviousPage.ShouldBeFalse();
    }
}
