// =============================================================
// Models/Cliente.cs
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LionttoMoveis.Validation;

namespace LionttoMoveis.Models
{
    [Table("clientes")]
    public class Cliente : EntidadeBase
    {
        [Required]
        [RequiredTrimmed(ErrorMessage = "Nome e obrigatorio.")]
        [Column("nome")]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [Column("telefone")]
        [MaxLength(30)]
        public string Telefone { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Column("endereco")]
        [MaxLength(300)]
        public string Endereco { get; set; } = string.Empty;

        // Navegação
        public List<Pedido> Pedidos { get; set; } = new();

        public override string Descricao() =>
            $"{Nome} — {Telefone} / {Email}";
    }
}
