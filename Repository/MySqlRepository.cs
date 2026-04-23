// =============================================================
// Repository/MySqlRepository.cs
// CONCEITO POO: HERANÇA + ABSTRAÇÃO + GENÉRICO
// Substitui MongoRepository<T> — mesma estrutura, agora com EF Core + MySQL
// =============================================================

using Microsoft.EntityFrameworkCore;
using LionttoMoveis.Data;
using LionttoMoveis.Models;

namespace LionttoMoveis.Repository
{
    /// <summary>
    /// Implementação genérica do repositório usando EF Core + MySQL.
    /// HERANÇA: repositórios específicos herdam desta classe.
    /// GENÉRICO: T = qualquer EntidadeBase (Material, Cliente...).
    /// </summary>
    public class MySqlRepository<T> : IRepository<T> where T : EntidadeBase
    {
        protected readonly AppDbContext _ctx;
        protected readonly DbSet<T> _set;

        public MySqlRepository(AppDbContext ctx)
        {
            _ctx = ctx;
            _set = ctx.Set<T>();
        }

        public virtual async Task<List<T>> ObterTodosAsync()
            => await _set.ToListAsync();

        public virtual async Task<T?> ObterPorIdAsync(int id)
            => await _set.FindAsync(id);

        public async Task InserirAsync(T entidade)
        {
            entidade.CriadoEm = DateTime.Now;
            _set.Add(entidade);
            await _ctx.SaveChangesAsync();
        }

        public async Task AtualizarAsync(T entidade)
        {
            _ctx.Entry(entidade).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
        }

        public async Task ExcluirAsync(int id)
        {
            var entidade = await _set.FindAsync(id);
            if (entidade is not null)
            {
                _set.Remove(entidade);
                await _ctx.SaveChangesAsync();
            }
        }

        public async Task<long> ContarAsync()
            => await _set.LongCountAsync();
    }
}
