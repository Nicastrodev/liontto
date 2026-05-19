// =============================================================
// Models/EntidadeBase.cs
// =============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LionttoMoveis.Models
{
    public abstract class EntidadeBase
    {
        public int Id { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.Now;

        [ConcurrencyCheck]
        [Column("row_version")]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public virtual string Descricao() => $"Entidade [{Id}]";
    }
}
