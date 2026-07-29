namespace PixHub.BuildingBlocks.Domain.Results;

/// <summary>
/// Um <see cref="Error"/> que agrega várias falhas individuais — normalmente uma por campo
/// inválido, produzidas pelo pipeline behavior de validação. Ele continua <i>sendo</i> um
/// <see cref="Error"/>, então trafega pelo <see cref="Result"/> sem alteração; a camada de API pode
/// reconhecê-lo por pattern matching e emitir um dicionário <c>errors</c> por campo (RFC 9457) em
/// vez de uma única mensagem.
/// </summary>
public sealed record ValidationError : Error
{
    public const string ErrorCode = "General.Validation";

    public ValidationError(IReadOnlyList<Error> errors)
        : base(ErrorCode, "Uma ou mais falhas de validação ocorreram.", ErrorType.Validation) =>
        Errors = errors;

    /// <summary>As falhas individuais, na ordem em que foram reportadas.</summary>
    public IReadOnlyList<Error> Errors { get; }
}
