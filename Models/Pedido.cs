// =============================================================
// Models/Pedido.cs
// CONCEITO POO: COMPOSIÇÃO + ENCAPSULAMENTO + POLIMORFISMO
// =============================================================
// No MongoDB, ItemDoPedido era embedded. No MySQL relacional,
// vira tabela separada itens_do_pedido com FK.
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LionttoMoveis.Models
{
    public enum StatusPedido
    {
        Aguardando,
        EmProducao,
        Pronto,
        Entregue
    }

    /// <summary>
    /// Tabela de itens do pedido: produto + quantidade + personalização.
    /// </summary>
    [Table("itens_do_pedido")]
    public class ItemDoPedido
    {
        public int Id { get; set; }

        // FK → Pedido
        [Column("pedido_id")]
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        // FK → Produto
        [Column("produto_id")]
        public int ProdutoId { get; set; }
        public Produto? Produto { get; set; }

        // Nome desnormalizado para histórico mesmo se produto for excluído
        [Column("produto_nome")]
        [MaxLength(150)]
        public string ProdutoNome { get; set; } = string.Empty;

        [Column("quantidade")]
        public int Quantidade { get; set; } = 1;

        [Column("preco_unitario", TypeName = "decimal(10,2)")]
        public decimal PrecoUnitario { get; set; }

        [Column("personalizacoes")]
        [MaxLength(500)]
        public string Personalizacoes { get; set; } = string.Empty;

        // ENCAPSULAMENTO: subtotal calculado, não salvo no banco
        [NotMapped]
        public decimal Subtotal => PrecoUnitario * Quantidade;
    }

    [Table("pedidos")]
    public class Pedido : EntidadeBase
    {
        // FK → Cliente
        [Column("cliente_id")]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        // Nome desnormalizado
        [Column("cliente_nome")]
        [MaxLength(150)]
        public string ClienteNome { get; set; } = string.Empty;

        [Column("status")]
        public StatusPedido Status { get; set; } = StatusPedido.Aguardando;

        [Column("observacoes")]
        [MaxLength(500)]
        public string Observacoes { get; set; } = string.Empty;

        [Column("valor_total", TypeName = "decimal(10,2)")]
        public decimal ValorTotal { get; set; } = 0;

        [Column("data_pedido")]
        public DateTime DataPedido { get; set; } = DateTime.Now;

        [Column("data_entrega_prevista")]
        public DateTime? DataEntregaPrevista { get; set; }

        [Column("data_entrega_real")]
        public DateTime? DataEntregaReal { get; set; }

        // Navegação
        public List<ItemDoPedido> Itens { get; set; } = new();

        // -------------------------------------------------------
        // ENCAPSULAMENTO: lógica de negócio no modelo
        // -------------------------------------------------------

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
                StatusPedido.Pronto     => StatusPedido.Entregue,
                _                       => Status
            };

            if (Status == StatusPedido.Entregue)
                DataEntregaReal = DateTime.Now;
        }

        [NotMapped]
        public string StatusLabel => Status switch
        {
            StatusPedido.Aguardando => "⏳ Aguardando",
            StatusPedido.EmProducao => "🔨 Em Produção",
            StatusPedido.Pronto     => "✅ Pronto",
            StatusPedido.Entregue   => "📦 Entregue",
            _                       => Status.ToString()
        };

        [NotMapped]
        public string StatusCssClass => Status switch
        {
            StatusPedido.Aguardando => "badge-secondary",
            StatusPedido.EmProducao => "badge-info",
            StatusPedido.Pronto     => "badge-warning",
            StatusPedido.Entregue   => "badge-ok",
            _                       => "badge-secondary"
        };

        public override string Descricao() =>
            $"Pedido #{Id} — {ClienteNome} — {StatusLabel} — R$ {ValorTotal:F2}";
    }
}
