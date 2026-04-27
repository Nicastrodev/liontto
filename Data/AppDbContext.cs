// =============================================================
// Data/AppDbContext.cs
// DbContext do Entity Framework Core - substitui o IMongoDatabase
// =============================================================

using Microsoft.EntityFrameworkCore;
using LionttoMoveis.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LionttoMoveis.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Cada DbSet<T> corresponde a uma tabela no MySQL
        public DbSet<Material> Materiais { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<MaterialDoProduto> MateriaisDoProduto { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemDoPedido> ItensDoPedido { get; set; }
        public DbSet<Movimentacao> Movimentacoes { get; set; }

        public override int SaveChanges()
        {
            ValidarEntidadesRastreadas();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ValidarEntidadesRastreadas();
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Material
            modelBuilder.Entity<Material>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Nome).IsRequired();
                e.Property(m => m.Unidade).IsRequired();
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_materiais_nome_not_blank", "CHAR_LENGTH(TRIM(nome)) > 0");
                    t.HasCheckConstraint("CK_materiais_unidade_not_blank", "CHAR_LENGTH(TRIM(unidade)) > 0");
                });

                e.HasMany(m => m.Movimentacoes)
                 .WithOne(mv => mv.Material)
                 .HasForeignKey(mv => mv.MaterialId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Cliente
            modelBuilder.Entity<Cliente>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Nome).IsRequired();
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_clientes_nome_not_blank", "CHAR_LENGTH(TRIM(nome)) > 0");
                });

                e.HasMany(c => c.Pedidos)
                 .WithOne(p => p.Cliente)
                 .HasForeignKey(p => p.ClienteId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Produto
            modelBuilder.Entity<Produto>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Nome).IsRequired();
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_produtos_nome_not_blank", "CHAR_LENGTH(TRIM(nome)) > 0");
                });

                e.HasMany(p => p.Materiais)
                 .WithOne(mp => mp.Produto)
                 .HasForeignKey(mp => mp.ProdutoId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // MaterialDoProduto
            modelBuilder.Entity<MaterialDoProduto>(e =>
            {
                e.HasKey(mp => mp.Id);
                e.Property(mp => mp.Nome).IsRequired();
                e.Property(mp => mp.Unidade).IsRequired();
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_materiais_do_produto_nome_not_blank", "CHAR_LENGTH(TRIM(nome_material)) > 0");
                    t.HasCheckConstraint("CK_materiais_do_produto_unidade_not_blank", "CHAR_LENGTH(TRIM(unidade)) > 0");
                });

                // FK para Material (sem cascade para nao apagar materiais ao editar produto)
                e.HasOne(mp => mp.Material)
                 .WithMany()
                 .HasForeignKey(mp => mp.MaterialId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Pedido
            modelBuilder.Entity<Pedido>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.ClienteNome).IsRequired();
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_pedidos_cliente_nome_not_blank", "CHAR_LENGTH(TRIM(cliente_nome)) > 0");
                });

                // Status como int no MySQL (enum C# -> inteiro)
                e.Property(p => p.Status).HasConversion<int>();
                e.HasMany(p => p.Itens)
                 .WithOne(i => i.Pedido)
                 .HasForeignKey(i => i.PedidoId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ItemDoPedido
            modelBuilder.Entity<ItemDoPedido>(e =>
            {
                e.HasKey(i => i.Id);
                e.Property(i => i.ProdutoNome).IsRequired();
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_itens_do_pedido_produto_nome_not_blank", "CHAR_LENGTH(TRIM(produto_nome)) > 0");
                });

                e.HasOne(i => i.Produto)
                 .WithMany()
                 .HasForeignKey(i => i.ProdutoId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Movimentacao
            modelBuilder.Entity<Movimentacao>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.NomeMaterial).IsRequired();
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_movimentacoes_nome_material_not_blank", "CHAR_LENGTH(TRIM(nome_material)) > 0");
                });
                e.Property(m => m.Tipo).HasConversion<int>();
            });
        }

        private void ValidarEntidadesRastreadas()
        {
            var entradas = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entrada in entradas)
            {
                ApararStrings(entrada);

                var resultados = new List<ValidationResult>();
                var contexto = new ValidationContext(entrada.Entity);
                var valido = Validator.TryValidateObject(
                    entrada.Entity,
                    contexto,
                    resultados,
                    validateAllProperties: true);

                if (valido)
                    continue;

                var erros = string.Join("; ",
                    resultados
                        .Select(r => r.ErrorMessage)
                        .Where(m => !string.IsNullOrWhiteSpace(m)));

                throw new ValidationException(string.IsNullOrWhiteSpace(erros)
                    ? "Dados invalidos para gravacao."
                    : erros);
            }
        }

        private static void ApararStrings(EntityEntry entrada)
        {
            foreach (var propriedade in entrada.Properties)
            {
                if (propriedade.Metadata.ClrType != typeof(string))
                    continue;

                if (propriedade.CurrentValue is string texto)
                    propriedade.CurrentValue = texto.Trim();
            }
        }
    }
}
