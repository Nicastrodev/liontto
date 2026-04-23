// =============================================================
// Repository/IRepository.cs
// CONCEITO POO: ABSTRAÇÃO + INTERFACE
// O contrato não muda — só a implementação troca de Mongo → MySQL
// =============================================================

namespace LionttoMoveis.Repository
{
    public interface IRepository<T> where T : Models.EntidadeBase
    {
        Task<List<T>> ObterTodosAsync();
        Task<T?> ObterPorIdAsync(int id);
        Task InserirAsync(T entidade);
        Task AtualizarAsync(T entidade);
        Task ExcluirAsync(int id);
        Task<long> ContarAsync();
    }
}
