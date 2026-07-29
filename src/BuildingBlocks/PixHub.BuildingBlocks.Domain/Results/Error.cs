namespace PixHub.BuildingBlocks.Domain.Results;

/// <summary>
/// Classifica um <see cref="Error"/> para que as camadas superiores (por exemplo, a API) consigam
/// mapeá-lo à preocupação de transporte adequada (código HTTP) sem vazar detalhes do domínio.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}

/// <summary>
/// Erro estruturado, portador de código, usado pelo trilho do <see cref="Result"/>.
/// Erros são valores (records), então comparam por conteúdo e são baratos de passar adiante.
/// Não é sealed para que <see cref="ValidationError"/> possa carregar um conjunto de erros e ainda
/// assim trafegar pelo formato de um único <c>Error</c> do <see cref="Result"/>.
/// </summary>
public record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static readonly Error NullValue =
        new("General.Null", "Um valor nulo foi informado.", ErrorType.Failure);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
}
