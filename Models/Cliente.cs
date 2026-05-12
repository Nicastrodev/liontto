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
        [Required(ErrorMessage = "Informe o nome do cliente.")]
        [RequiredTrimmed(ErrorMessage = "Informe o nome do cliente.")]
        [Column("nome")]
        [MaxLength(150, ErrorMessage = "O nome pode ter no maximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Column("telefone")]
        [MaxLength(30, ErrorMessage = "O telefone pode ter no maximo 30 caracteres.")]
        public string? Telefone { get; set; }

        [Column("email")]
        [MaxLength(150, ErrorMessage = "O e-mail pode ter no maximo 150 caracteres.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
        public string? Email { get; set; }

        [Column("endereco")]
        [MaxLength(300, ErrorMessage = "O endereco pode ter no maximo 300 caracteres.")]
        public string? Endereco { get; set; }

        public List<Pedido> Pedidos { get; set; } = new();

        public override string Descricao() =>
            $"{Nome} - {(string.IsNullOrWhiteSpace(Telefone) ? "sem telefone" : Telefone)} / {(string.IsNullOrWhiteSpace(Email) ? "sem e-mail" : Email)}";
    }
}
