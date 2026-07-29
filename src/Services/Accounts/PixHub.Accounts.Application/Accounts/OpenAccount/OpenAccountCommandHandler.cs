using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Accounts.OpenAccount;

/// <summary>
/// Orquestra a abertura da conta: converte a entrada primitiva em value objects, checa a regra que
/// atravessa agregados (CPF único) e deixa a decisão final com o agregado. O handler não contém
/// regra de negócio própria — ele apenas encadeia domínio e persistência.
/// </summary>
internal sealed class OpenAccountCommandHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<OpenAccountCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(OpenAccountCommand command, CancellationToken cancellationToken)
    {
        var cpfResult = Cpf.Create(command.HolderCpf);
        if (cpfResult.IsFailure)
        {
            return Result.Failure<Guid>(cpfResult.Error);
        }

        var cpf = cpfResult.Value;

        // Verificação antecipada para devolver um erro claro (409). Ela não elimina a corrida entre
        // duas aberturas simultâneas — quem garante isso é o índice único do banco, na Infrastructure.
        if (await accountRepository.ExistsByCpfAsync(cpf, cancellationToken))
        {
            return Result.Failure<Guid>(AccountErrors.DuplicateCpf);
        }

        Money? openingBalance = null;
        if (command.OpeningBalance is { } amount)
        {
            var moneyResult = Money.Create(amount);
            if (moneyResult.IsFailure)
            {
                return Result.Failure<Guid>(moneyResult.Error);
            }

            openingBalance = moneyResult.Value;
        }

        var accountResult = Account.Open(cpf, command.HolderName, openingBalance);
        if (accountResult.IsFailure)
        {
            return Result.Failure<Guid>(accountResult.Error);
        }

        var account = accountResult.Value;

        accountRepository.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account.Id.Value;
    }
}
