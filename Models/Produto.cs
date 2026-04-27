// =============================================================
// Models/Produto.cs
// CONCEITO POO: COMPOSIÇÃO + HERANÇA
// =============================================================
// No MongoDB, MaterialDoProduto era embedded dentro de Produto.
// No MySQL relacional, vira uma tabela separada com FK.
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LionttoMoveis.Validation;

namespace LionttoMoveis.Models
{
    /// <summary>
    /// Tabela de junção: quais materiais e quantidades um produto usa.
    /// COMPOSIÇÃO: Produto "tem" uma lista de MaterialDoProduto.
    /// </summary>
    [Table("materiais_do_produto")]
    public class MaterialDoProduto
    {
        public int Id { get; set; }

        // FK → Produto
        [Column("produto_id")]
        public int ProdutoId { get; set; }
        public Produto? Produto { get; set; }

        // FK → Material
        [Column("material_id")]
        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        // Nome desnormalizado para exibição rápida sem joins adicionais
        [Required]
        [RequiredTrimmed(ErrorMessage = "Nome do material e obrigatorio.")]
        [Column("nome_material")]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [RequiredTrimmed(ErrorMessage = "Unidade do material e obrigatoria.")]
        [Column("unidade")]
        [MaxLength(30)]
        public string Unidade { get; set; } = string.Empty;

        [Column("quantidade_necessaria")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantidade necessaria deve ser maior que zero.")]
        public double QuantidadeNecessaria { get; set; }
    }

    /// <summary>
    /// Representa um móvel fabricado pela empresa.
    /// HERANÇA: herda de EntidadeBase.
    /// COMPOSIÇÃO: contém lista de MaterialDoProduto.
    /// </summary>
    [Table("produtos")]
    public class Produto : EntidadeBase
    {
        [Required]
        [RequiredTrimmed(ErrorMessage = "Nome do produto e obrigatorio.")]
        [Column("nome")]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Column("descricao")]
        [MaxLength(500)]
        public string Descricao_ { get; set; } = string.Empty;

        [Column("preco_base", TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "Preco base invalido.")]
        public decimal PrecoBase { get; set; } = 0;

        [Column("tempo_producao_dias")]
        [Range(1, int.MaxValue, ErrorMessage = "Tempo de producao deve ser de pelo menos 1 dia.")]
        public int TempoProducaoDias { get; set; } = 7;

        // Navegação (substitui a lista embedded do MongoDB)
        public List<MaterialDoProduto> Materiais { get; set; } = new();

        public override string Descricao() =>
            $"{Nome} — R$ {PrecoBase:F2} ({TempoProducaoDias} dias)";
    }
}
