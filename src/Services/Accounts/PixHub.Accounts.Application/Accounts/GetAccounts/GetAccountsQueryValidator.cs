using FluentValidation;

namespace PixHub.Accounts.Application.Accounts.GetAccounts;

/// <summary>
/// Limita a paginação. O teto de <see cref="MaxPageSize"/> é uma proteção: sem ele, um cliente
/// poderia pedir a tabela inteira em uma única chamada e derrubar o lado de leitura.
/// </summary>
internal sealed class GetAccountsQueryValidator : AbstractValidator<GetAccountsQuery>
{
    public const int MaxPageSize = 100;

    public GetAccountsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MaxPageSize)
                .WithMessage($"O tamanho da página deve estar entre 1 e {MaxPageSize}.");

        RuleFor(query => query.Status!.Value)
            .IsInEnum().WithMessage("A situação informada é inválida.")
            .When(query => query.Status.HasValue);
    }
}
