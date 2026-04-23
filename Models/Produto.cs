// =============================================================
// Models/Produto.cs
// CONCEITO POO: COMPOSIÇÃO + HERANÇA
// =============================================================
// No MongoDB, MaterialDoProduto era embedded dentro de Produto.
// No MySQL relacional, vira uma tabela separada com FK.
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Column("nome_material")]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Column("unidade")]
        [MaxLength(30)]
        public string Unidade { get; set; } = string.Empty;

        [Column("quantidade_necessaria")]
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
        [Column("nome")]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Column("descricao")]
        [MaxLength(500)]
        public string Descricao_ { get; set; } = string.Empty;

        [Column("preco_base", TypeName = "decimal(10,2)")]
        public decimal PrecoBase { get; set; } = 0;

        [Column("tempo_producao_dias")]
        public int TempoProducaoDias { get; set; } = 7;

        // Navegação (substitui a lista embedded do MongoDB)
        public List<MaterialDoProduto> Materiais { get; set; } = new();

        public override string Descricao() =>
            $"{Nome} — R$ {PrecoBase:F2} ({TempoProducaoDias} dias)";
    }
}
