using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixHub.Accounts.Domain.Accounts;
using PixHub.Accounts.Domain.ValueObjects;

namespace PixHub.Accounts.Infrastructure.Configurations;

/// <summary>
/// Mapeamento EF Core do agregado <see cref="Account"/>. Os value objects viram ou uma conversão
/// escalar (<see cref="AccountId"/>, <see cref="Cpf"/>) ou um tipo possuído (<see cref="Money"/>) —
/// nos dois casos o EF materializa pelo construtor privado do VO, então nenhuma invariante de
/// domínio precisa ser reaberta só para servir ao ORM.
/// </summary>
internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id)
            .HasConversion(id => id.Value, value => AccountId.From(value))
            .HasColumnName("id")
            .ValueGeneratedNever();

        // O CPF já chega validado do domínio; Cpf.Create devolve Result só para reabrir a validação
        // na fronteira de entrada — aqui a conversão apenas desembrulha um valor que já é válido.
        builder.Property(account => account.HolderCpf)
            .HasConversion(cpf => cpf.Value, value => Cpf.Create(value).Value)
            .HasColumnName("holder_cpf")
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(account => account.HolderName)
            .HasColumnName("holder_name")
            .HasMaxLength(200)
            .IsRequired();

        // Tipo possuído (mesma tabela): o construtor privado Money(decimal amount, Currency currency)
        // é resolvido pelo EF via constructor binding, casando os parâmetros pelo nome com as
        // propriedades Amount/Currency.
        builder.OwnsOne(account => account.Balance, balance =>
        {
            balance.Property(money => money.Amount)
                .HasColumnName("balance_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            balance.Property(money => money.Currency)
                .HasConversion<string>()
                .HasColumnName("balance_currency")
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(account => account.Balance).IsRequired();

        builder.Property(account => account.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(account => account.OpenedAtUtc)
            .HasColumnName("opened_at_utc")
            .IsRequired();

        // Unicidade entre agregados: a checagem antecipada no handler evita a corrida na maior parte
        // dos casos, mas é este índice que garante a invariante sob concorrência real.
        builder.HasIndex(account => account.HolderCpf).IsUnique();

        // DomainEvents é uma projeção computada sobre uma lista privada, não uma coluna nem uma
        // navegação — o EF não deve tentar mapeá-la.
        builder.Ignore(account => account.DomainEvents);
    }
}
