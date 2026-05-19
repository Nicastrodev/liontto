// =============================================================
// Repository/Repositorios.cs
// =============================================================

using Microsoft.EntityFrameworkCore;
using LionttoMoveis.Data;
using LionttoMoveis.Models;

namespace LionttoMoveis.Repository
{
    public class MaterialRepository : MySqlRepository<Material>
    {
        public MaterialRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<Material>> ObterOrdenadosAsync()
            => await _ctx.Materiais
                .AsNoTracking()
                .OrderBy(m => m.Nome)
                .ToListAsync();

        public async Task<List<Material>> ObterEstoqueBaixoAsync()
            => await _ctx.Materiais
                .AsNoTracking()
                .Where(m => m.Quantidade <= m.QuantidadeMinima)
                .OrderBy(m => m.Quantidade)
                .ToListAsync();

        public async Task AtualizarQuantidadeAsync(int id, double delta)
        {
            await _ctx.Materiais
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(m => m.Quantidade, m => m.Quantidade + delta));
        }
    }

    public class ClienteRepository : MySqlRepository<Cliente>
    {
        public ClienteRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<Cliente>> ObterOrdenadosAsync()
            => await _ctx.Clientes
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                .ToListAsync();
    }

    public class ProdutoRepository : MySqlRepository<Produto>
    {
        public ProdutoRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<Produto>> ObterOrdenadosAsync()
            => await _ctx.Produtos
                .AsNoTracking()
                .OrderBy(p => p.Nome)
                .ToListAsync();

        public async Task<Produto?> ObterComMateriaisAsync(int id)
            => await _ctx.Produtos
                .AsNoTracking()
                .Include(p => p.Materiais)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<Produto>> ObterTodosComMateriaisAsync()
            => await _ctx.Produtos
                .AsNoTracking()
                .Include(p => p.Materiais)
                .OrderBy(p => p.Nome)
                .ToListAsync();

        public async Task<bool> AtualizarComMateriaisAsync(int id, Produto dadosAtualizados, List<MaterialDoProduto> materiais)
        {
            var produto = await _ctx.Produtos
                .Include(p => p.Materiais)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (produto is null)
                return false;

            if (dadosAtualizados.RowVersion is null || dadosAtualizados.RowVersion.Length == 0)
                throw new DbUpdateConcurrencyException("Token de concorrencia ausente para atualizar o produto.");

            _ctx.Entry(produto)
                .Property(nameof(EntidadeBase.RowVersion))
                .OriginalValue = dadosAtualizados.RowVersion;

            produto.Nome = dadosAtualizados.Nome;
            produto.Descricao_ = dadosAtualizados.Descricao_;
            produto.PrecoBase = dadosAtualizados.PrecoBase;
            produto.TempoProducaoDias = dadosAtualizados.TempoProducaoDias;

            _ctx.MateriaisDoProduto.RemoveRange(produto.Materiais);

            foreach (var material in materiais)
                material.ProdutoId = id;

            produto.Materiais = materiais;

            await _ctx.SaveChangesAsync();
            return true;
        }
    }

    public class PedidoRepository : MySqlRepository<Pedido>
    {
        public PedidoRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<Pedido>> ObterTodosOrdenadosAsync()
            => await _ctx.Pedidos
                .AsNoTracking()
                .Include(p => p.Itens)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

        public async Task<Pedido?> ObterComItensAsync(int id)
            => await _ctx.Pedidos
                .AsNoTracking()
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<Pedido>> ObterPorStatusAsync(StatusPedido status)
            => await _ctx.Pedidos
                .AsNoTracking()
                .Include(p => p.Itens)
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

        public async Task<List<Pedido>> ObterPorClienteAsync(int clienteId)
            => await _ctx.Pedidos
                .AsNoTracking()
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

        public async Task InserirComItensAsync(Pedido pedido)
        {
            pedido.CriadoEm = DateTime.Now;
            _ctx.Pedidos.Add(pedido);
            await _ctx.SaveChangesAsync();
        }

        public async Task<bool> AtualizarStatusAsync(int pedidoId, StatusPedido status, DateTime? dataEntregaReal, byte[] rowVersion)
        {
            var pedido = await _ctx.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId);
            if (pedido is null)
                return false;

            if (rowVersion is null || rowVersion.Length == 0)
                throw new DbUpdateConcurrencyException("Token de concorrencia ausente para atualizar o pedido.");

            _ctx.Entry(pedido)
                .Property(nameof(EntidadeBase.RowVersion))
                .OriginalValue = rowVersion;

            pedido.Status = status;
            pedido.DataEntregaReal = dataEntregaReal;
            await _ctx.SaveChangesAsync();
            return true;
        }
    }

    public class MovimentacaoRepository : MySqlRepository<Movimentacao>
    {
        public MovimentacaoRepository(AppDbContext ctx) : base(ctx) { }

        public async Task<List<Movimentacao>> ObterPorMaterialAsync(int materialId, int limite = 10)
            => await _ctx.Movimentacoes
                .AsNoTracking()
                .Where(m => m.MaterialId == materialId)
                .OrderByDescending(m => m.DataMovimentacao)
                .Take(limite)
                .ToListAsync();
    }
}

