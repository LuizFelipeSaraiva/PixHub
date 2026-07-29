using PixHub.Accounts.Application.Abstractions.Messaging;
using PixHub.Accounts.Application.Accounts.Shared;

namespace PixHub.Accounts.Application.Accounts.GetAccountById;

/// <summary>Consulta uma conta pelo identificador.</summary>
public sealed record GetAccountByIdQuery(Guid AccountId) : IQuery<AccountResponse>;
