using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PixHub.Accounts.Application.Behaviors;

namespace PixHub.Accounts.Application;

/// <summary>
/// Composition root da camada de Application: quem hospeda o serviço (API ou Worker) chama apenas
/// <see cref="AddAccountsApplication"/> e não precisa conhecer handlers nem validadores.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAccountsApplication(this IServiceCollection services)
    {
        // O AddMediator precisa ser chamado no mesmo projeto que referencia o Mediator.SourceGenerator:
        // o gerador lê estas opções em tempo de compilação e emite os registros já fechados por tipo
        // de mensagem (sem varredura por reflexão em runtime e compatível com Native AOT).
        services.AddMediator(options =>
        {
            // Escopo por requisição: os handlers dependem do repositório/DbContext do EF Core, que
            // é scoped. Singleton renderia mais, mas capturaria um DbContext entre requisições.
            options.ServiceLifetime = ServiceLifetime.Scoped;

            // A ordem é a ordem de execução: o log envolve tudo (inclusive as falhas de validação),
            // e a validação corta antes de o handler tocar o banco.
            options.PipelineBehaviors =
            [
                typeof(LoggingPipelineBehavior<,>),
                typeof(ValidationPipelineBehavior<,>)
            ];
        });

        // includeInternalTypes: os validadores são internal, pois só o pipeline os utiliza.
        services.AddValidatorsFromAssemblyContaining<IAccountsApplicationMarker>(includeInternalTypes: true);

        return services;
    }
}

/// <summary>
/// Âncora de assembly para varreduras. É preferível a <c>typeof(DependencyInjection).Assembly</c>
/// por deixar explícito que o tipo existe apenas para localizar este assembly.
/// </summary>
public interface IAccountsApplicationMarker;
