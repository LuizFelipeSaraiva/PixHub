namespace PixHub.BuildingBlocks.Domain.Results;

/// <summary>
/// Railway-oriented result. Domain and application operations return a <see cref="Result"/>
/// (or <see cref="Result{TValue}"/>) instead of throwing for expected failures, keeping the
/// control flow explicit and cheap. Exceptions remain for truly exceptional/unrecoverable cases.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        switch (isSuccess)
        {
            case true when error != Error.None:
                throw new InvalidOperationException("A successful result cannot contain an error.");
            case false when error == Error.None:
                throw new InvalidOperationException("A failure result must contain a non-empty error.");
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

    /// <summary>Wraps a possibly-null value: null becomes a <see cref="Error.NullValue"/> failure.</summary>
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    /// <summary>Lets a method returning <see cref="Result"/> simply <c>return someError;</c>.</summary>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>A <see cref="Result"/> that also carries a value on success.</summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    /// <summary>The success value. Accessing it on a failure result is a programming error.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
