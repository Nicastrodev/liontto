// =============================================================
// ViewModels/ViewModels.cs
// Adaptado para IDs inteiros (MySQL AUTO_INCREMENT)
// =============================================================

using LionttoMoveis.Models;
using System.ComponentModel.DataAnnotations;
using LionttoMoveis.Validation;

namespace LionttoMoveis.ViewModels
{
    /// <summary>ViewModel para a tela de novo pedido.</summary>
    public class NovoPedidoViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecione um cliente.")]
        public int ClienteId { get; set; }

        [MaxLength(500, ErrorMessage = "As observacoes podem ter no maximo 500 caracteres.")]
        public string? Observacoes { get; set; }

        public string? DataEntregaPrevista { get; set; }

        // Listas paralelas dos itens do formulário
        public List<int>    ProdIds  { get; set; } = new();
        public List<int>    ProdQtds { get; set; } = new();
        public List<string?> ProdPers { get; set; } = new();

        // Dados para popular selects
        public List<Cliente> Clientes { get; set; } = new();
        public List<Produto> Produtos { get; set; } = new();
    }

    /// <summary>ViewModel para a tela de detalhes do pedido.</summary>
    public class DetalhesPedidoViewModel
    {
        public Pedido Pedido { get; set; } = null!;
    }

    /// <summary>ViewModel para movimentação de estoque.</summary>
    public class MovimentacaoViewModel
    {
        public Material Material { get; set; } = null!;

        [RequiredTrimmed(ErrorMessage = "Selecione o tipo de movimentacao.")]
        public string Tipo { get; set; } = "Entrada";

        [Range(0.01, double.MaxValue, ErrorMessage = "Digite uma quantidade valida.")]
        public double Quantidade { get; set; }

        [MaxLength(300, ErrorMessage = "O motivo pode ter no maximo 300 caracteres.")]
        public string? Motivo { get; set; }

        public List<Movimentacao> Historico { get; set; } = new();
    }

    /// <summary>ViewModel para cadastro/edição de Produto com materiais.</summary>
    public class ProdutoViewModel
    {
        public Produto Produto { get; set; } = new();
        public List<Material> MateriaisDisponiveis { get; set; } = new();

        // Listas paralelas dos materiais do formulário
        public List<int>    MatIds  { get; set; } = new();
        public List<double> MatQtds { get; set; } = new();
    }
}
