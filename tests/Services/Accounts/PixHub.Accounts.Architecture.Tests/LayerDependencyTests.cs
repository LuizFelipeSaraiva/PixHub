using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Architecture.Tests;

/// <summary>
/// Garante em CI as regras de dependência do Clean Architecture do serviço Accounts: a seta de
/// dependência aponta sempre para dentro (Api/Infrastructure → Application → Domain), nunca o
/// contrário. Um teste falhando aqui é um vazamento de camada — não um detalhe de implementação.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "PixHub.Accounts.Domain";
    private const string ApplicationNamespace = "PixHub.Accounts.Application";
    private const string InfrastructureNamespace = "PixHub.Accounts.Infrastructure";
    private const string ApiNamespace = "PixHub.Accounts.Api";

    // Ancorados em um tipo de cada assembly e carregados via InAssemblies (não InCurrentDomain):
    // o AppDomain só contém os assemblies já carregados por reflexão, e nada nestes testes força o
    // carregamento da Api/Infrastructure — um vazamento de camada lá passaria batido em silêncio.
    private static readonly Assembly[] Assemblies =
    [
        typeof(PixHub.Accounts.Domain.Accounts.Account).Assembly,
        typeof(PixHub.Accounts.Application.DependencyInjection).Assembly,
        typeof(PixHub.Accounts.Infrastructure.DependencyInjection).Assembly,
        typeof(PixHub.Accounts.Api.Program).Assembly
    ];

    [Fact]
    public void Domain_NaoDeveDependerDeNenhumaOutraCamada()
    {
        var result = Types.InAssemblies(Assemblies)
            .That().ResideInNamespace(DomainNamespace)
            .ShouldNot().HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FormatFailures(result));
    }

    [Fact]
    public void Application_NaoDeveDependerDeInfrastructureOuApi()
    {
        var result = Types.InAssemblies(Assemblies)
            .That().ResideInNamespace(ApplicationNamespace)
            .ShouldNot().HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FormatFailures(result));
    }

    [Fact]
    public void Infrastructure_NaoDeveDependerDeApi()
    {
        var result = Types.InAssemblies(Assemblies)
            .That().ResideInNamespace(InfrastructureNamespace)
            .ShouldNot().HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FormatFailures(result));
    }

    [Fact]
    public void Handlers_DevemSerInternal()
    {
        // Só o pipeline do Mediator resolve os handlers; internal evita que a superfície pública da
        // Application seja maior do que ela precisa ser.
        var result = Types.InAssemblies(Assemblies)
            .That().ResideInNamespace(ApplicationNamespace)
            .And().HaveNameEndingWith("CommandHandler").Or().HaveNameEndingWith("QueryHandler")
            .Should().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FormatFailures(result));
    }

    [Fact]
    public void Repositorios_DevemSerInternal()
    {
        // Só a composition root da Infrastructure resolve as implementações — o contrato público é a
        // interface, definida na Application.
        var result = Types.InAssemblies(Assemblies)
            .That().ResideInNamespace(InfrastructureNamespace)
            .And().HaveNameEndingWith("Repository")
            .And().AreClasses()
            .Should().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FormatFailures(result));
    }

    private static string FormatFailures(NetArchTest.Rules.TestResult result) =>
        result.FailingTypes is null
            ? "Falha sem tipos reportados."
            : string.Join(Environment.NewLine, result.FailingTypes.Select(type => type.FullName));
}
