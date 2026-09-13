using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using PixHub.Accounts.Infrastructure.Repositories;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Infrastructure.Tests.Repositories;

/// <summary>
/// Exercita <see cref="AccountRepository"/> e <see cref="UnitOfWork"/> contra um Postgres real: o
/// que se prova aqui é a tradução do EF Core (mapeamento, índice único, conversões de VO) — as
/// invariantes de negócio já estão cobertas nos testes de domínio/application.
/// </summary>
public sealed class AccountRepositoryTests(AccountsDatabaseFixture fixture)
    : IClassFixture<AccountsDatabaseFixture>
{
    [Fact]
    public async Task AddESaveChanges_DevePersistirAConta()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new AccountRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);

        var account = Account.Open(TestCpfGenerator.NewValid(), "Ana Beatriz", Money.Create(100m).Value).Value;
        repository.Add(account);
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var readContext = fixture.CreateDbContext();
        var reloaded = await new AccountRepository(readContext)
            .GetByIdAsync(account.Id, TestContext.Current.CancellationToken);

        reloaded.ShouldNotBeNull();
        reloaded.HolderName.ShouldBe("Ana Beatriz");
        reloaded.Balance.Amount.ShouldBe(100m);
        reloaded.Status.ShouldBe(AccountStatus.Active);
    }

    [Fact]
    public async Task GetByIdAsync_QuandoContaNaoExiste_DeveDevolverNull()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new AccountRepository(dbContext);

        var reloaded = await repository.GetByIdAsync(AccountId.New(), TestContext.Current.CancellationToken);

        reloaded.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByCpfAsync_ReflenteInsercoesConfirmadas()
    {
        var cpf = TestCpfGenerator.NewValid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new AccountRepository(dbContext);
            (await repository.ExistsByCpfAsync(cpf, TestContext.Current.CancellationToken)).ShouldBeFalse();

            repository.Add(Account.Open(cpf, "Carlos Eduardo").Value);
            await new UnitOfWork(dbContext).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = fixture.CreateDbContext();
        (await new AccountRepository(verifyContext)
            .ExistsByCpfAsync(cpf, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task Add_ComCpfDuplicado_DeveViolarIndiceUnicoNoSaveChanges()
    {
        var cpf = TestCpfGenerator.NewValid();

        await using var firstContext = fixture.CreateDbContext();
        new AccountRepository(firstContext).Add(Account.Open(cpf, "Titular Um").Value);
        await new UnitOfWork(firstContext).SaveChangesAsync(TestContext.Current.CancellationToken);

        // A checagem antecipada de unicidade é responsabilidade do handler, na Application — aqui se
        // prova a última linha de defesa: o índice único do banco, sob duas contas com o mesmo CPF.
        await using var secondContext = fixture.CreateDbContext();
        new AccountRepository(secondContext).Add(Account.Open(cpf, "Titular Dois").Value);

        await Should.ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
            new UnitOfWork(secondContext).SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}
