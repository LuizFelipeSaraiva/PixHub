using PixHub.BuildingBlocks.Domain.Results;

namespace PixHub.Accounts.Api.Extensions;

/// <summary>
/// Traduz o trilho <see cref="Result"/> da Application para HTTP: sucesso vira a resposta que o
/// endpoint escolher, falha vira <c>ProblemDetails</c> (RFC 9457) — com um dicionário por campo
/// quando o erro é um <see cref="ValidationError"/>.
/// </summary>
internal static class ResultExtensions
{
    public static IResult ToApiResult(this Result result, Func<IResult>? onSuccess = null) =>
        result.IsSuccess ? onSuccess?.Invoke() ?? Results.NoContent() : result.Error.ToProblem();

    public static IResult ToApiResult<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem();

    private static IResult ToProblem(this Error error)
    {
        if (error is ValidationError validationError)
        {
            var errors = validationError.Errors
                .GroupBy(fieldError => fieldError.Code)
                .ToDictionary(group => group.Key, group => group.Select(fieldError => fieldError.Description).ToArray());

            return Results.ValidationProblem(errors, title: error.Description);
        }

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(title: error.Code, detail: error.Description, statusCode: statusCode);
    }
}
