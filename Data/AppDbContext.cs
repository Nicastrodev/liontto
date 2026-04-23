// =============================================================
// Data/AppDbContext.cs
// DbContext do Entity Framework Core — substitui o IMongoDatabase
// =============================================================

using Microsoft.EntityFrameworkCore;
using LionttoMoveis.Models;

namespace LionttoMoveis.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Cada DbSet<T> corresponde a uma tabela no MySQL
        public DbSet<Material>        Materiais       { get; set; }
        public DbSet<Cliente>         Clientes        { get; set; }
        public DbSet<Produto>         Produtos        { get; set; }
        public DbSet<MaterialDoProduto> MateriaisDoProduto { get; set; }
        public DbSet<Pedido>          Pedidos         { get; set; }
        public DbSet<ItemDoPedido>    ItensDoPedido   { get; set; }
        public DbSet<Movimentacao>    Movimentacoes   { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Material ────────────────────────────────────────────
            modelBuilder.Entity<Material>(e =>
            {
                e.HasKey(m => m.Id);
                // Relação: um Material tem muitas Movimentações
                e.HasMany(m => m.Movimentacoes)
                 .WithOne(mv => mv.Material)
                 .HasForeignKey(mv => mv.MaterialId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Cliente ─────────────────────────────────────────────
            modelBuilder.Entity<Cliente>(e =>
            {
                e.HasKey(c => c.Id);
                e.HasMany(c => c.Pedidos)
                 .WithOne(p => p.Cliente)
                 .HasForeignKey(p => p.ClienteId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Produto ─────────────────────────────────────────────
            modelBuilder.Entity<Produto>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasMany(p => p.Materiais)
                 .WithOne(mp => mp.Produto)
                 .HasForeignKey(mp => mp.ProdutoId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── MaterialDoProduto ────────────────────────────────────
            modelBuilder.Entity<MaterialDoProduto>(e =>
            {
                e.HasKey(mp => mp.Id);
                // FK para Material (sem cascade para não apagar materiais ao editar produto)
                e.HasOne(mp => mp.Material)
                 .WithMany()
                 .HasForeignKey(mp => mp.MaterialId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Pedido ───────────────────────────────────────────────
            modelBuilder.Entity<Pedido>(e =>
            {
                e.HasKey(p => p.Id);
                // Status como int no MySQL (enum C# → inteiro)
                e.Property(p => p.Status).HasConversion<int>();
                e.HasMany(p => p.Itens)
                 .WithOne(i => i.Pedido)
                 .HasForeignKey(i => i.PedidoId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── ItemDoPedido ─────────────────────────────────────────
            modelBuilder.Entity<ItemDoPedido>(e =>
            {
                e.HasKey(i => i.Id);
                e.HasOne(i => i.Produto)
                 .WithMany()
                 .HasForeignKey(i => i.ProdutoId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Movimentacao ─────────────────────────────────────────
            modelBuilder.Entity<Movimentacao>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Tipo).HasConversion<int>();
            });
        }
    }
}
