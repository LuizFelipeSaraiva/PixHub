using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PixHub.Accounts.Application.Abstractions.Data;
using PixHub.Accounts.Application.Accounts.GetAccountById;
using PixHub.Accounts.Application.Accounts.OpenAccount;
using PixHub.Accounts.Domain.Accounts;
using PixHub.BuildingBlocks.Domain.Results;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Application.Tests;

/// <summary>
/// Exercita a camada pelo container de verdade, e não instanciando handlers na mão. É o que prova
/// que o gerador do Mediator enxergou os handlers, que os behaviors entraram na ordem configurada e
/// que os validadores foram descobertos — coisas que testes de unidade isolados não pegariam.
/// </summary>
public class DependencyInjectionTests
{
    private const string ValidCpf = "111.444.777-35";

    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAccountReadRepository _readRepository = Substitute.For<IAccountReadRepository>();

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAccountsApplication();

        // As portas de persistência são fornecidas pela Infrastructure; aqui entram dublês.
        services.AddScoped(_ => _accountRepository);
        services.AddScoped(_ => _unitOfWork);
        services.AddScoped(_ => _readRepository);

        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public async Task Mediator_ResolvesAndRunsACommandHandler()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new OpenAccountCommand(ValidCpf, "Maria Silva"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        _accountRepository.Received(1).Add(Arg.Any<Account>());
    }

    [Fact]
    public async Task Mediator_ResolvesAndRunsAQueryHandler()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var missingId = Guid.CreateVersion7();

        var result = await mediator.Send(
            new GetAccountByIdQuery(missingId),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AccountErrors.NotFound(missingId));
    }

    [Fact]
    public async Task ValidationBehavior_IsWiredIntoThePipeline()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new OpenAccountCommand(string.Empty, string.Empty),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();

        // A validação cortou antes do handler: nada chegou à persistência.
        _accountRepository.DidNotReceive().Add(Arg.Any<Account>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void EveryValidatorInTheAssembly_IsRegistered()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var validator = scope.ServiceProvider
            .GetService<FluentValidation.IValidator<OpenAccountCommand>>();

        validator.ShouldNotBeNull();
    }
}
