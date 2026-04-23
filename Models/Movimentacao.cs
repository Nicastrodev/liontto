// =============================================================
// Models/Movimentacao.cs
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LionttoMoveis.Models
{
    public enum TipoMovimentacao { Entrada, Saida }

    [Table("movimentacoes")]
    public class Movimentacao : EntidadeBase
    {
        // FK → Material
        [Column("material_id")]
        public int MaterialId { get; set; }
        public Material? Material { get; set; }

        [Column("nome_material")]
        [MaxLength(150)]
        public string NomeMaterial { get; set; } = string.Empty;

        [Column("tipo")]
        public TipoMovimentacao Tipo { get; set; }

        [Column("quantidade")]
        public double Quantidade { get; set; }

        [Column("motivo")]
        [MaxLength(300)]
        public string Motivo { get; set; } = string.Empty;

        [Column("data_movimentacao")]
        public DateTime DataMovimentacao { get; set; } = DateTime.Now;

        [NotMapped]
        public string TipoLabel => Tipo == TipoMovimentacao.Entrada ? "📥 Entrada" : "📤 Saída";

        public override string Descricao() =>
            $"{TipoLabel} de {Quantidade} — {NomeMaterial} em {DataMovimentacao:dd/MM/yyyy}";
    }
}
