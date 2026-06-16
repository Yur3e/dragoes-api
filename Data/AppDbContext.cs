using Microsoft.EntityFrameworkCore;
using MinimalApi.Models;

namespace MinimalApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Dragao> Dragoes => Set<Dragao>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<QuizTentativa> QuizTentativas => Set<QuizTentativa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>()
            .HasIndex(x => x.Login)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .Property(x => x.Nome)
            .HasMaxLength(120);

        modelBuilder.Entity<Usuario>()
            .Property(x => x.Login)
            .HasMaxLength(80);

        modelBuilder.Entity<Usuario>()
            .Property(x => x.SenhaHash)
            .HasMaxLength(256);

        modelBuilder.Entity<Usuario>()
            .Property(x => x.SenhaSalt)
            .HasMaxLength(64);

        modelBuilder.Entity<Dragao>()
            .Property(x => x.Nome)
            .HasMaxLength(120);

        modelBuilder.Entity<QuizTentativa>()
            .HasOne(x => x.Usuario)
            .WithMany(x => x.Tentativas)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuizTentativa>()
            .HasOne(x => x.Dragao)
            .WithMany(x => x.Tentativas)
            .HasForeignKey(x => x.DragaoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Dragao>().HasData(
            new Dragao
            {
                Id = 1,
                Nome = "Smaug",
                ImagemUrl = "https://placehold.co/600x400?text=Smaug"
            },
            new Dragao
            {
                Id = 2,
                Nome = "Drogon",
                ImagemUrl = "https://placehold.co/600x400?text=Drogon"
            },
            new Dragao
            {
                Id = 3,
                Nome = "Falkor",
                ImagemUrl = "https://placehold.co/600x400?text=Falkor"
            }
        );
    }
}
