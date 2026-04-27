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
        public string? Observacoes { get; set; }
        public string? DataEntregaPrevista { get; set; }

        // Listas paralelas dos itens do formulário
        public List<int>    ProdIds  { get; set; } = new();
        public List<int>    ProdQtds { get; set; } = new();
        public List<string> ProdPers { get; set; } = new();

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
        [RequiredTrimmed(ErrorMessage = "Tipo de movimentacao e obrigatorio.")]
        public string Tipo { get; set; } = "Entrada";
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero.")]
        public double Quantidade { get; set; }
        public string Motivo { get; set; } = string.Empty;
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
