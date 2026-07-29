namespace PixHub.BuildingBlocks.Domain.Results;

/// <summary>
/// Resultado no estilo "railway-oriented". Operações de domínio e de aplicação devolvem um
/// <see cref="Result"/> (ou <see cref="Result{TValue}"/>) em vez de lançar exceção para falhas
/// esperadas, mantendo o fluxo de controle explícito e barato. Exceções ficam reservadas aos casos
/// realmente excepcionais/irrecuperáveis.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        switch (isSuccess)
        {
            case true when error != Error.None:
                throw new InvalidOperationException("Um resultado de sucesso não pode conter erro.");
            case false when error == Error.None:
                throw new InvalidOperationException("Um resultado de falha precisa conter um erro.");
            default:
                IsSuccess = isSuccess;
                Error = error;
                break;
        }
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>Encapsula um valor possivelmente nulo: nulo vira uma falha <see cref="Error.NullValue"/>.</summary>
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    /// <summary>Permite que um método que devolve <see cref="Result"/> faça apenas <c>return algumErro;</c>.</summary>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Um <see cref="Result"/> que também carrega um valor em caso de sucesso.</summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    /// <summary>Valor do sucesso. Acessá-lo em um resultado de falha é erro de programação.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar o valor de um resultado de falha.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
