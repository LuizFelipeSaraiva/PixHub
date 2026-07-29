namespace PixHub.Accounts.Domain.ValueObjects;

/// <summary>
/// Identificador fortemente tipado de uma conta. Um record struct dá igualdade por valor de graça e
/// evita passar por engano um <see cref="Guid"/> cru onde se espera o id de uma conta.
/// Usa GUID v7 (ordenado no tempo) para manter os índices do banco com boa localidade.
/// </summary>
public readonly record struct AccountId(Guid Value)
{
    public static AccountId New() => new(Guid.CreateVersion7());

    public static AccountId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
