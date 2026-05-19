using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionttoMoveis.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "produtos",
                type: "binary(16)",
                nullable: false,
                defaultValue: new byte[] { });

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "pedidos",
                type: "binary(16)",
                nullable: false,
                defaultValue: new byte[] { });

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "movimentacoes",
                type: "binary(16)",
                nullable: false,
                defaultValue: new byte[] { });

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "materiais",
                type: "binary(16)",
                nullable: false,
                defaultValue: new byte[] { });

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "clientes",
                type: "binary(16)",
                nullable: false,
                defaultValue: new byte[] { });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "row_version",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "movimentacoes");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "materiais");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "clientes");
        }
    }
}
