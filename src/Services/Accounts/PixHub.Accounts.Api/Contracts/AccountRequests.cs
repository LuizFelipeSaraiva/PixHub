namespace PixHub.Accounts.Api.Contracts;

// Corpos de requisição próprios da API: os comandos de movimentação/bloqueio carregam o AccountId,
// mas na rota REST ele já vem do path — então o corpo traz só o que falta.

/// <summary>Corpo de <c>POST /api/accounts/{id}/credit</c> e <c>.../debit</c>.</summary>
public sealed record AmountRequest(decimal Amount);

/// <summary>Corpo de <c>POST /api/accounts/{id}/block</c>.</summary>
public sealed record BlockRequest(string Reason);
