using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixHub.Accounts.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "accounts");

        migrationBuilder.CreateTable(
            name: "accounts",
            schema: "accounts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                holder_cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                holder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                balance_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                balance_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                opened_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_accounts", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_accounts_holder_cpf",
            schema: "accounts",
            table: "accounts",
            column: "holder_cpf",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "accounts",
            schema: "accounts");
    }
}
