using System.Reflection;
using FluentValidation;
using Mediator;
using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Application.Behaviors;

/// <summary>
/// Executa os validadores do FluentValidation registrados para a mensagem antes de o handler rodar.
/// As falhas viram um <see cref="ValidationError"/> devolvido pelo trilho do <see cref="Result"/> —
/// e não uma exceção: validação de entrada é uma falha <i>esperada</i>, então não deve custar o
/// preço de uma exceção nem depender de um middleware para ser traduzida.
/// </summary>
/// <remarks>
/// A restrição <c>TResponse : Result</c> faz o gerador do Mediator registrar este behavior apenas
/// para mensagens que de fato retornam <see cref="Result"/>; as demais são ignoradas em tempo de
/// compilação.
/// </remarks>
internal sealed class ValidationPipelineBehavior<TMessage, TResponse>(
    IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        // Materializa uma vez: o container entrega um array, mas o contrato é IEnumerable.
        var applicable = validators as IValidator<TMessage>[] ?? [.. validators];
        if (applicable.Length == 0)
        {
            return await next(message, cancellationToken);
        }

        List<Error>? errors = null;

        foreach (var validator in applicable)
        {
            // Um contexto novo por validador: o ValidationContext acumula as falhas registradas
            // nele, então reaproveitá-lo faria o segundo validador redevolver as do primeiro.
            var context = new ValidationContext<TMessage>(message);
            var validation = await validator.ValidateAsync(context, cancellationToken);
            if (validation.IsValid)
            {
                continue;
            }

            errors ??= [];
            foreach (var failure in validation.Errors)
            {
                // O nome da propriedade vira o código do erro: é o que a API usa como chave do
                // dicionário "errors" do ProblemDetails (RFC 9457).
                errors.Add(Error.Validation(failure.PropertyName, failure.ErrorMessage));
            }
        }

        return errors is null
            ? await next(message, cancellationToken)
            : CreateFailure(new ValidationError(errors));
    }

    /// <summary>
    /// Constrói o <typeparamref name="TResponse"/> de falha. <c>TResponse</c> pode ser
    /// <see cref="Result"/> ou <see cref="Result{TValue}"/>, e só descobrimos qual em tempo de
    /// execução — mas como campos estáticos de um tipo genérico existem uma vez por tipo fechado,
    /// a reflexão acontece apenas na primeira vez e depois vira uma chamada de delegate.
    /// </summary>
    private static readonly Func<Error, TResponse> CreateFailure = BuildFailureFactory();

    private static Func<Error, TResponse> BuildFailureFactory()
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return error => (TResponse)Result.Failure(error);
        }

        // TResponse é Result<TValue>: fecha o Result.Failure<TValue>(Error) para esse TValue.
        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true })
            .MakeGenericMethod(valueType);

        return failureMethod.CreateDelegate<Func<Error, TResponse>>();
    }
}
