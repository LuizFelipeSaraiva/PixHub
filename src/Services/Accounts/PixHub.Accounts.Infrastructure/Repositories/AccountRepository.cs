using Microsoft.EntityFrameworkCore;
using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;

namespace PixHub.Accounts.Infrastructure.Repositories;

/// <summary>Implementação EF Core da porta de escrita — o agregado completo, rastreado pelo change tracker.</summary>
internal sealed class AccountRepository(AccountsDbContext dbContext) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        dbContext.Accounts
            .Include(account => account.Balance)
            .FirstOrDefaultAsync(account => account.Id == id, cancellationToken);

    public Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken = default) =>
        dbContext.Accounts.AnyAsync(account => account.HolderCpf == cpf, cancellationToken);

    public void Add(Account account) => dbContext.Accounts.Add(account);
}
