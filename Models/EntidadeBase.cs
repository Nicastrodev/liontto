// =============================================================
// Models/EntidadeBase.cs
// CONCEITO POO: HERANÇA
// Classe base abstrata para todas as entidades do sistema.
// =============================================================

namespace LionttoMoveis.Models
{
    /// <summary>
    /// Classe base abstrata para todas as entidades do sistema.
    /// HERANÇA: todas as entidades herdam Id e CriadoEm automaticamente.
    /// </summary>
    public abstract class EntidadeBase
    {
        public int Id { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.Now;

        // POLIMORFISMO: método virtual que cada subclasse pode sobrescrever
        public virtual string Descricao() => $"Entidade [{Id}]";
    }
}
