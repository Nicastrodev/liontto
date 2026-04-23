// =============================================================
// Models/Material.cs
// CONCEITO POO: ENCAPSULAMENTO + HERANÇA + POLIMORFISMO
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LionttoMoveis.Models
{
    /// <summary>
    /// Representa uma matéria-prima usada na fabricação de móveis.
    /// HERANÇA: herda Id e CriadoEm de EntidadeBase.
    /// </summary>
    [Table("materiais")]
    public class Material : EntidadeBase
    {
        [Required]
        [Column("nome")]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [Column("unidade")]
        [MaxLength(30)]
        public string Unidade { get; set; } = string.Empty;

        [Column("quantidade")]
        public double Quantidade { get; set; } = 0;

        [Column("quantidade_minima")]
        public double QuantidadeMinima { get; set; } = 5;

        [Column("preco_unitario", TypeName = "decimal(10,2)")]
        public decimal PrecoUnitario { get; set; } = 0;

        // Navegação: histórico de movimentações deste material
        public List<Movimentacao> Movimentacoes { get; set; } = new();

        // -------------------------------------------------------
        // ENCAPSULAMENTO: propriedade calculada — não é coluna no banco
        // -------------------------------------------------------
        [NotMapped]
        public string StatusEstoque
        {
            get
            {
                if (Quantidade <= QuantidadeMinima)       return "critico";
                if (Quantidade <= QuantidadeMinima * 1.5) return "baixo";
                return "ok";
            }
        }

        [NotMapped]
        public bool EstoqueAbaixoDoMinimo => Quantidade <= QuantidadeMinima;

        public override string Descricao() =>
            $"{Nome} — {Quantidade} {Unidade} em estoque";
    }
}
