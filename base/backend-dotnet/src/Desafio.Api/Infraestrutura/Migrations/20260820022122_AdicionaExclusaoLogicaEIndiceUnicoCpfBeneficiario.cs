using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desafio.Api.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaExclusaoLogicaEIndiceUnicoCpfBeneficiario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExcluidoEm",
                table: "Beneficiarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiarios_Cpf",
                table: "Beneficiarios",
                column: "Cpf",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Beneficiarios_Cpf",
                table: "Beneficiarios");

            migrationBuilder.DropColumn(
                name: "ExcluidoEm",
                table: "Beneficiarios");
        }
    }
}
