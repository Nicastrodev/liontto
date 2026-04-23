// =============================================================
// Services/EstoqueService.cs
// CONCEITO POO: ENCAPSULAMENTO DA LÓGICA DE NEGÓCIO
// Regras de negócio de estoque — não mudam com a troca de banco
// =============================================================

using LionttoMoveis.Models;
using LionttoMoveis.Repository;

namespace LionttoMoveis.Services
{
    public class EstoqueService
    {
        private readonly MaterialRepository     _materiais;
        private readonly MovimentacaoRepository _movimentacoes;

        public EstoqueService(MaterialRepository materiais, MovimentacaoRepository movimentacoes)
        {
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
            string motivo)
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
                Motivo           = motivo,
                DataMovimentacao = DateTime.Now
            });

            return null; // null = sucesso
        }
    }
}
