using Microsoft.EntityFrameworkCore;

namespace PixHub.Identity.Infrastructure;

/// <summary>
/// Persistência do servidor de autorização. Não modela nenhum agregado de domínio próprio — as
/// entidades (aplicações, escopos, autorizações, tokens) são inteiramente do OpenIddict, registradas
/// via <see cref="Microsoft.EntityFrameworkCore.OpenIddictEntityFrameworkCoreExtensions.UseOpenIddict"/>.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseOpenIddict();
    }
}
