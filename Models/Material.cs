// =============================================================
// Models/Material.cs
// CONCEITO POO: ENCAPSULAMENTO + HERANÇA + POLIMORFISMO
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LionttoMoveis.Validation;

namespace LionttoMoveis.Models
{
    /// <summary>
    /// Representa uma matéria-prima usada na fabricação de móveis.
    /// HERANÇA: herda Id e CriadoEm de EntidadeBase.
    /// </summary>
    [Table("materiais")]
    public class Material : EntidadeBase
    {
        [Required(ErrorMessage = "Informe o nome do material.")]
        [RequiredTrimmed(ErrorMessage = "Informe o nome do material.")]
        [Column("nome")]
        [MaxLength(150, ErrorMessage = "O nome do material pode ter no maximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione a unidade de medida.")]
        [RequiredTrimmed(ErrorMessage = "Selecione a unidade de medida.")]
        [Column("unidade")]
        [MaxLength(30, ErrorMessage = "A unidade pode ter no maximo 30 caracteres.")]
        public string Unidade { get; set; } = string.Empty;

        [Column("quantidade")]
        [Range(0, double.MaxValue, ErrorMessage = "Digite uma quantidade valida.")]
        public double Quantidade { get; set; } = 0;

        [Column("quantidade_minima")]
        [Range(0, double.MaxValue, ErrorMessage = "Digite uma quantidade minima valida.")]
        public double QuantidadeMinima { get; set; } = 5;

        [Column("preco_unitario", TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "Digite um preco unitario valido.")]
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
