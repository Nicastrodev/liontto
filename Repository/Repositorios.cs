// =============================================================
// Repository/Repositorios.cs
// CONCEITO POO: HERANÇA CONCRETA
// Cada repositório herda MySqlRepository<T> e adiciona
// apenas os métodos específicos da entidade.
// =============================================================

using Microsoft.EntityFrameworkCore;
using LionttoMoveis.Data;
using LionttoMoveis.Models;

namespace LionttoMoveis.Repository
{
    // ----------------------------------------------------------
    // MaterialRepository
    // ----------------------------------------------------------
    public class MaterialRepository : MySqlRepository<Material>
    {
        public MaterialRepository(AppDbContext ctx) : base(ctx) { }

        /// <summary>Materiais ordenados por nome.</summary>
        public async Task<List<Material>> ObterOrdenadosAsync()
            => await _ctx.Materiais
                .OrderBy(m => m.Nome)
                .ToListAsync();

        /// <summary>Materiais com estoque abaixo ou igual ao mínimo.</summary>
        public async Task<List<Material>> ObterEstoqueBaixoAsync()
            => await _ctx.Materiais
                .Where(m => m.Quantidade <= m.QuantidadeMinima)
                .OrderBy(m => m.Quantidade)
                .ToListAsync();

        /// <summary>
        /// Incrementa/decrementa a quantidade usando UPDATE direto.
        /// Mais seguro que buscar + salvar (evita race condition).
        /// </summary>
        public async Task AtualizarQuantidadeAsync(int id, double delta)
        {
            // ExecuteUpdateAsync: UPDATE materiais SET quantidade = quantidade + delta WHERE id = id
            await _ctx.Materiais
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(m => m.Quantidade, m => m.Quantidade + delta));
        }
    }

    // ----------------------------------------------------------
    // ClienteRepository
    // ----------------------------------------------------------
    public class ClienteRepository : MySqlRepository<Cliente>
    {
        public ClienteRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<Cliente>> ObterOrdenadosAsync()
            => await _ctx.Clientes
                .OrderBy(c => c.Nome)
                .ToListAsync();
    }

    // ----------------------------------------------------------
    // ProdutoRepository
    // ----------------------------------------------------------
    public class ProdutoRepository : MySqlRepository<Produto>
    {
        public ProdutoRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<Produto>> ObterOrdenadosAsync()
            => await _ctx.Produtos
                .OrderBy(p => p.Nome)
                .ToListAsync();

        /// <summary>Produto com a lista de materiais carregada (Include).</summary>
        public async Task<Produto?> ObterComMateriaisAsync(int id)
            => await _ctx.Produtos
                .Include(p => p.Materiais)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<Produto>> ObterTodosComMateriaisAsync()
            => await _ctx.Produtos
                .Include(p => p.Materiais)
                .OrderBy(p => p.Nome)
                .ToListAsync();
    }

    // ----------------------------------------------------------
    // PedidoRepository
    // ----------------------------------------------------------
    public class PedidoRepository : MySqlRepository<Pedido>
    {
        public PedidoRepository(AppDbContext ctx) : base(ctx) { }

        /// <summary>Todos os pedidos com itens, do mais recente ao mais antigo.</summary>
        public async Task<List<Pedido>> ObterTodosOrdenadosAsync()
            => await _ctx.Pedidos
                .Include(p => p.Itens)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

        /// <summary>Pedido completo com itens.</summary>
        public async Task<Pedido?> ObterComItensAsync(int id)
            => await _ctx.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<Pedido>> ObterPorStatusAsync(StatusPedido status)
            => await _ctx.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

        public async Task<List<Pedido>> ObterPorClienteAsync(int clienteId)
            => await _ctx.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.ClienteId == clienteId)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

        public async Task<Dictionary<string, int>> ContarPorStatusAsync()
        {
            return await _ctx.Pedidos
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        /// <summary>
        /// Insere pedido com itens (cascade automático pelo EF Core).
        /// </summary>
        public async Task InserirComItensAsync(Pedido pedido)
        {
            pedido.CriadoEm = DateTime.Now;
            _ctx.Pedidos.Add(pedido);
            await _ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Atualiza apenas campos do cabeçalho do pedido (sem tocar nos itens).
        /// </summary>
        public async Task AtualizarStatusAsync(Pedido pedido)
        {
            _ctx.Entry(pedido).State = EntityState.Modified;
            // Marca os itens como não modificados para não alterar itens existentes
            foreach (var item in pedido.Itens)
                _ctx.Entry(item).State = EntityState.Unchanged;
            await _ctx.SaveChangesAsync();
        }
    }

    // ----------------------------------------------------------
    // MovimentacaoRepository
    // ----------------------------------------------------------
    public class MovimentacaoRepository : MySqlRepository<Movimentacao>
    {
        public MovimentacaoRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<Movimentacao>> ObterPorMaterialAsync(int materialId, int limite = 10)
            => await _ctx.Movimentacoes
                .Where(m => m.MaterialId == materialId)
                .OrderByDescending(m => m.DataMovimentacao)
                .Take(limite)
                .ToListAsync();
    }
}
