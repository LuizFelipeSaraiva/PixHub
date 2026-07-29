namespace PixHub.Accounts.Application.Abstractions.Data;

/// <summary>
/// Uma página de resultados do lado de leitura, junto do total de itens — o suficiente para o
/// cliente montar a navegação sem uma segunda chamada.
/// </summary>
public sealed record PagedList<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Total de páginas para o <see cref="PageSize"/> atual (0 quando não há itens).</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
