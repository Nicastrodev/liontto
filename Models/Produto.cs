// =============================================================
// Models/Produto.cs
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LionttoMoveis.Validation;

namespace LionttoMoveis.Models
{
    [Table("materiais_do_produto")]
    public class MaterialDoProduto
    {
        public int Id { get; set; }

        [Column("produto_id")]
        public int ProdutoId { get; set; }
        public Produto? Produto { get; set; }

        [Column("material_id")]
        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        [Required(ErrorMessage = "Nome do material e obrigatorio.")]
        [RequiredTrimmed(ErrorMessage = "Nome do material e obrigatorio.")]
        [Column("nome_material")]
        [MaxLength(150, ErrorMessage = "O nome do material pode ter no maximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unidade do material e obrigatoria.")]
        [RequiredTrimmed(ErrorMessage = "Unidade do material e obrigatoria.")]
        [Column("unidade")]
        [MaxLength(30, ErrorMessage = "A unidade pode ter no maximo 30 caracteres.")]
        public string Unidade { get; set; } = string.Empty;

        [Column("quantidade_necessaria")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantidade necessaria deve ser maior que zero.")]
        public double QuantidadeNecessaria { get; set; }
    }

    [Table("produtos")]
    public class Produto : EntidadeBase
    {
        [Required(ErrorMessage = "Informe o nome do produto.")]
        [RequiredTrimmed(ErrorMessage = "Informe o nome do produto.")]
        [Column("nome")]
        [MaxLength(150, ErrorMessage = "O nome do produto pode ter no maximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Column("descricao")]
        [MaxLength(500, ErrorMessage = "A descricao pode ter no maximo 500 caracteres.")]
        public string? Descricao_ { get; set; }

        [Column("preco_base", TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "Digite um preco base valido.")]
        public decimal PrecoBase { get; set; } = 0;

        [Column("tempo_producao_dias")]
        [Range(1, int.MaxValue, ErrorMessage = "Informe um tempo de producao de pelo menos 1 dia.")]
        public int TempoProducaoDias { get; set; } = 7;

        public List<MaterialDoProduto> Materiais { get; set; } = new();

        public override string Descricao() =>
            $"{Nome} - R$ {PrecoBase:F2} ({TempoProducaoDias} dias)";
    }
}
