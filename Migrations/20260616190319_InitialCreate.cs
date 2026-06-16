using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MinimalApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dragoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ImagemUrl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dragoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Login = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TotalAcertos = table.Column<int>(type: "integer", nullable: false),
                    PontuacaoTotal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuizTentativas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    DragaoId = table.Column<int>(type: "integer", nullable: false),
                    RespostaInformada = table.Column<string>(type: "text", nullable: false),
                    Acertou = table.Column<bool>(type: "boolean", nullable: false),
                    TempoRespostaSegundos = table.Column<int>(type: "integer", nullable: false),
                    PontosGanhos = table.Column<int>(type: "integer", nullable: false),
                    RespondidaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizTentativas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizTentativas_Dragoes_DragaoId",
                        column: x => x.DragaoId,
                        principalTable: "Dragoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizTentativas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Dragoes",
                columns: new[] { "Id", "ImagemUrl", "Nome" },
                values: new object[,]
                {
                    { 1, "https://placehold.co/600x400?text=Smaug", "Smaug" },
                    { 2, "https://placehold.co/600x400?text=Drogon", "Drogon" },
                    { 3, "https://placehold.co/600x400?text=Falkor", "Falkor" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizTentativas_DragaoId",
                table: "QuizTentativas",
                column: "DragaoId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizTentativas_UsuarioId",
                table: "QuizTentativas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Login",
                table: "Usuarios",
                column: "Login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizTentativas");

            migrationBuilder.DropTable(
                name: "Dragoes");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
