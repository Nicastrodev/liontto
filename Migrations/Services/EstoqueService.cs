// =============================================================
// Services/EstoqueService.cs
// CONCEITO POO: ENCAPSULAMENTO DA LÓGICA DE NEGÓCIO
// Regras de negócio de estoque — não mudam com a troca de banco
// =============================================================

using LionttoMoveis.Data;
using LionttoMoveis.Models;
using LionttoMoveis.Repository;
using Microsoft.EntityFrameworkCore;

namespace LionttoMoveis.Services
{
    public class EstoqueService
    {
        private readonly AppDbContext _ctx;
        private readonly MaterialRepository     _materiais;
        private readonly MovimentacaoRepository _movimentacoes;

        public EstoqueService(
            AppDbContext ctx,
            MaterialRepository materiais,
            MovimentacaoRepository movimentacoes)
        {
            _ctx            = ctx;
            _materiais      = materiais;
            _movimentacoes  = movimentacoes;
        }

        /// <summary>
        /// Registra uma movimentação de estoque (entrada ou saída).
        /// Valida, atualiza o estoque e grava o histórico.
        /// </summary>
        /// <returns>Mensagem de erro, ou null se OK.</returns>
        public async Task<string?> MovimentarAsync(
            int materialId,
            TipoMovimentacao tipo,
            double quantidade,
            string? motivo)
        {
            var material = await _materiais.ObterPorIdAsync(materialId);
            if (material is null)
                return "Material não encontrado.";

            if (quantidade <= 0)
                return "A quantidade deve ser maior que zero.";

            // REGRA DE NEGÓCIO: não permite saída além do estoque disponível
            if (tipo == TipoMovimentacao.Saida && quantidade > material.Quantidade)
                return $"Estoque insuficiente. Disponível: {material.Quantidade} {material.Unidade}.";

            double delta = tipo == TipoMovimentacao.Entrada ? quantidade : -quantidade;

            // UPDATE atômico diretamente no banco
            await _materiais.AtualizarQuantidadeAsync(materialId, delta);

            // Registra histórico
            await _movimentacoes.InserirAsync(new Movimentacao
            {
                MaterialId       = materialId,
                NomeMaterial     = material.Nome,
                Tipo             = tipo,
                Quantidade       = quantidade,
                Motivo           = motivo ?? string.Empty,
                DataMovimentacao = DateTime.Now
            });

            return null; // null = sucesso
        }

        /// <summary>
        /// Cria o pedido e baixa automaticamente do estoque os materiais usados
        /// na composição de cada produto.
        /// </summary>
        public async Task<string?> RegistrarPedidoAsync(Pedido pedido)
        {
            if (pedido.Itens.Count == 0)
                return "Adicione pelo menos um produto valido ao pedido.";

            var consumo = await CalcularConsumoPedidoAsync(pedido.Itens);
            if (consumo.Erro is not null)
                return consumo.Erro;

            await using var transaction = await _ctx.Database.BeginTransactionAsync();

            try
            {
                var erroEstoque = await ValidarEstoqueAsync(consumo.Consumo);
                if (erroEstoque is not null)
                {
                    await transaction.RollbackAsync();
                    return erroEstoque;
                }

                pedido.CriadoEm = DateTime.Now;
                _ctx.Pedidos.Add(pedido);
                await _ctx.SaveChangesAsync();

                foreach (var item in consumo.Consumo.Values)
                {
                    await _materiais.AtualizarQuantidadeAsync(item.MaterialId, -item.QuantidadeTotal);

                    await _movimentacoes.InserirAsync(new Movimentacao
                    {
                        MaterialId = item.MaterialId,
                        NomeMaterial = item.NomeMaterial,
                        Tipo = TipoMovimentacao.Saida,
                        Quantidade = item.QuantidadeTotal,
                        Motivo = $"Consumo do pedido #{pedido.Id}",
                        DataMovimentacao = DateTime.Now
                    });
                }

                await transaction.CommitAsync();
                return null;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<(Dictionary<int, ConsumoMaterial> Consumo, string? Erro)> CalcularConsumoPedidoAsync(
            IReadOnlyCollection<ItemDoPedido> itens)
        {
            var consumo = new Dictionary<int, ConsumoMaterial>();

            foreach (var item in itens)
            {
                if (item.Quantidade <= 0)
                    continue;

                var produto = await _ctx.Produtos
                    .Include(p => p.Materiais)
                    .FirstOrDefaultAsync(p => p.Id == item.ProdutoId);

                if (produto is null)
                    return (consumo, $"Produto com id {item.ProdutoId} nao foi encontrado.");

                foreach (var material in produto.Materiais)
                {
                    var quantidadeTotal = material.QuantidadeNecessaria * item.Quantidade;

                    if (consumo.TryGetValue(material.MaterialId, out var atual))
                    {
                        atual.QuantidadeTotal += quantidadeTotal;
                        consumo[material.MaterialId] = atual;
                    }
                    else
                    {
                        consumo[material.MaterialId] = new ConsumoMaterial(
                            material.MaterialId,
                            material.Nome,
                            material.Unidade,
                            quantidadeTotal);
                    }
                }
            }

            if (consumo.Count == 0)
                return (consumo, "Nao foi possivel calcular os materiais do pedido.");

            return (consumo, null);
        }

        private async Task<string?> ValidarEstoqueAsync(Dictionary<int, ConsumoMaterial> consumo)
        {
            foreach (var item in consumo.Values)
            {
                var material = await _materiais.ObterPorIdAsync(item.MaterialId);
                if (material is null)
                    return $"Material \"{item.NomeMaterial}\" nao foi encontrado.";

                if (material.Quantidade < item.QuantidadeTotal)
                {
                    return
                        $"Estoque insuficiente para \"{material.Nome}\". " +
                        $"Necessario: {item.QuantidadeTotal:0.##} {material.Unidade}. " +
                        $"Disponivel: {material.Quantidade:0.##} {material.Unidade}.";
                }
            }

            return null;
        }

        private sealed class ConsumoMaterial
        {
            public ConsumoMaterial(int materialId, string nomeMaterial, string unidade, double quantidadeTotal)
            {
                MaterialId = materialId;
                NomeMaterial = nomeMaterial;
                Unidade = unidade;
                QuantidadeTotal = quantidadeTotal;
            }

            public int MaterialId { get; }
            public string NomeMaterial { get; }
            public string Unidade { get; }
            public double QuantidadeTotal { get; set; }
        }
    }
}
