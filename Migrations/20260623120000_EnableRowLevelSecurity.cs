using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinimalApi.Migrations
{
    /// <inheritdoc />
    [Migration("20260623120000_EnableRowLevelSecurity")]
    public partial class EnableRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public."__EFMigrationsHistory" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public."Dragoes" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public."QuizTentativas" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public."Usuarios" ENABLE ROW LEVEL SECURITY;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public."__EFMigrationsHistory" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public."Dragoes" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public."QuizTentativas" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public."Usuarios" DISABLE ROW LEVEL SECURITY;
                """);
        }
    }
}
