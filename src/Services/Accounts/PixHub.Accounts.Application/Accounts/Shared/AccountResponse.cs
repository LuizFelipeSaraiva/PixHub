namespace PixHub.Accounts.Application.Accounts.Shared;

/// <summary>
/// Representação de leitura de uma conta. É um contrato próprio do lado de leitura — montado pelo
/// Dapper a partir de uma projeção SQL — e não o agregado, de forma que a modelagem de escrita possa
/// evoluir sem quebrar quem consome a API.
/// </summary>
/// <param name="HolderCpfMasked">
/// CPF sempre mascarado (<c>123.***.***-09</c>). É dado pessoal: o valor completo nunca sai do
/// serviço, e o nome do campo torna essa regra explícita no contrato.
/// </param>
public sealed record AccountResponse(
    Guid Id,
    string HolderCpfMasked,
    string HolderName,
    decimal Balance,
    string Currency,
    string Status,
    DateTimeOffset OpenedAtUtc);

/// <summary>Projeção reduzida usada em listagens, para não trafegar campos desnecessários.</summary>
public sealed record AccountSummaryResponse(
    Guid Id,
    string HolderName,
    decimal Balance,
    string Status);
