// =============================================================
// Models/Pedido.cs
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LionttoMoveis.Validation;

namespace LionttoMoveis.Models
{
    public enum StatusPedido
    {
        Aguardando,
        EmProducao,
        Pronto,
        Entregue
    }

    [Table("itens_do_pedido")]
    public class ItemDoPedido
    {
        public int Id { get; set; }

        [Column("pedido_id")]
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        [Column("produto_id")]
        public int ProdutoId { get; set; }
        public Produto? Produto { get; set; }

        [Required(ErrorMessage = "Produto obrigatorio.")]
        [RequiredTrimmed(ErrorMessage = "Produto obrigatorio.")]
        [Column("produto_nome")]
        [MaxLength(150, ErrorMessage = "O nome do produto pode ter no maximo 150 caracteres.")]
        public string ProdutoNome { get; set; } = string.Empty;

        [Column("quantidade")]
        [Range(1, int.MaxValue, ErrorMessage = "Digite uma quantidade valida.")]
        public int Quantidade { get; set; } = 1;

        [Column("preco_unitario", TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "Digite um preco unitario valido.")]
        public decimal PrecoUnitario { get; set; }

        [Column("personalizacoes")]
        [MaxLength(500, ErrorMessage = "A personalizacao pode ter no maximo 500 caracteres.")]
        public string? Personalizacoes { get; set; }

        [NotMapped]
        public decimal Subtotal => PrecoUnitario * Quantidade;
    }

    [Table("pedidos")]
    public class Pedido : EntidadeBase
    {
        [Column("cliente_id")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecione um cliente.")]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        [Required(ErrorMessage = "Nome do cliente obrigatorio.")]
        [RequiredTrimmed(ErrorMessage = "Nome do cliente obrigatorio.")]
        [Column("cliente_nome")]
        [MaxLength(150, ErrorMessage = "O nome do cliente pode ter no maximo 150 caracteres.")]
        public string ClienteNome { get; set; } = string.Empty;

        [Column("status")]
        public StatusPedido Status { get; set; } = StatusPedido.Aguardando;

        [Column("observacoes")]
        [MaxLength(500, ErrorMessage = "As observacoes podem ter no maximo 500 caracteres.")]
        public string? Observacoes { get; set; }

        [Column("valor_total", TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "Valor total invalido.")]
        public decimal ValorTotal { get; set; } = 0;

        [Column("data_pedido")]
        public DateTime DataPedido { get; set; } = DateTime.Now;

        [Column("data_entrega_prevista")]
        public DateTime? DataEntregaPrevista { get; set; }

        [Column("data_entrega_real")]
        public DateTime? DataEntregaReal { get; set; }

        public List<ItemDoPedido> Itens { get; set; } = new();

        public void RecalcularTotal()
        {
            ValorTotal = Itens.Sum(i => i.Subtotal);
        }

        public void AvancarStatus()
        {
            Status = Status switch
            {
                StatusPedido.Aguardando => StatusPedido.EmProducao,
                StatusPedido.EmProducao => StatusPedido.Pronto,
                StatusPedido.Pronto => StatusPedido.Entregue,
                _ => Status
            };

            if (Status == StatusPedido.Entregue)
                DataEntregaReal = DateTime.Now;
        }

        [NotMapped]
        public string StatusLabel => Status switch
        {
            StatusPedido.Aguardando => "Aguardando",
            StatusPedido.EmProducao => "Em producao",
            StatusPedido.Pronto => "Pronto",
            StatusPedido.Entregue => "Entregue",
            _ => Status.ToString()
        };

        [NotMapped]
        public string StatusCssClass => Status switch
        {
            StatusPedido.Aguardando => "badge-secondary",
            StatusPedido.EmProducao => "badge-info",
            StatusPedido.Pronto => "badge-warning",
            StatusPedido.Entregue => "badge-ok",
            _ => "badge-secondary"
        };

        public override string Descricao() =>
            $"Pedido #{Id} - {ClienteNome} - {StatusLabel} - R$ {ValorTotal:F2}";
    }
}
