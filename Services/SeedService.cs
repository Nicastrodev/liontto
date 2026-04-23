// =============================================================
// Services/SeedService.cs
// Popula o banco com dados de exemplo na primeira execução
// Adaptado para MySQL: IDs são int gerados pelo banco (AUTO_INCREMENT)
// =============================================================

using LionttoMoveis.Models;
using LionttoMoveis.Repository;

namespace LionttoMoveis.Services
{
    public class SeedService
    {
        private readonly MaterialRepository _materiais;
        private readonly ProdutoRepository  _produtos;
        private readonly ClienteRepository  _clientes;

        public SeedService(MaterialRepository mat, ProdutoRepository prod, ClienteRepository cli)
        {
            _materiais = mat; _produtos = prod; _clientes = cli;
        }

        public async Task SeedAsync()
        {
            // Só insere se o banco estiver vazio
            if (await _materiais.ContarAsync() > 0) return;

            // ── Materiais ────────────────────────────────────────────
            var mdf15   = new Material { Nome="MDF 15mm Branco",      Unidade="m²",     Quantidade=50, QuantidadeMinima=10, PrecoUnitario=45.00m };
            var mdf18   = new Material { Nome="MDF 18mm Cru",         Unidade="m²",     Quantidade=30, QuantidadeMinima=8,  PrecoUnitario=38.00m };
            var fita    = new Material { Nome="Fita de Borda Branca", Unidade="metro",  Quantidade=200,QuantidadeMinima=50, PrecoUnitario=1.50m  };
            var dobrad  = new Material { Nome="Dobradiça 35mm",       Unidade="unidade",Quantidade=150,QuantidadeMinima=30, PrecoUnitario=2.80m  };
            var corred  = new Material { Nome="Corrediça 45cm",       Unidade="par",    Quantidade=40, QuantidadeMinima=10, PrecoUnitario=18.00m };
            var paraf   = new Material { Nome="Parafuso 3.5x16",      Unidade="caixa",  Quantidade=20, QuantidadeMinima=5,  PrecoUnitario=12.00m };
            var cola    = new Material { Nome="Cola PVA",             Unidade="litro",  Quantidade=8,  QuantidadeMinima=2,  PrecoUnitario=22.00m };
            var perfil  = new Material { Nome="Perfil de Alumínio",   Unidade="metro",  Quantidade=25, QuantidadeMinima=5,  PrecoUnitario=15.00m };

            await _materiais.InserirAsync(mdf15);
            await _materiais.InserirAsync(mdf18);
            await _materiais.InserirAsync(fita);
            await _materiais.InserirAsync(dobrad);
            await _materiais.InserirAsync(corred);
            await _materiais.InserirAsync(paraf);
            await _materiais.InserirAsync(cola);
            await _materiais.InserirAsync(perfil);

            // Após inserção, os objetos já têm Id preenchido pelo EF Core
            // (diferente do MongoDB onde precisávamos recarregar)

            // ── Produtos (com materiais via cascade) ─────────────────
            var produtos = new List<Produto>
            {
                new() {
                    Nome="Armário de Cozinha", Descricao_="Armário superior com 2 portas",
                    PrecoBase=850m, TempoProducaoDias=5,
                    Materiais = new() {
                        new() { MaterialId=mdf15.Id,  Nome="MDF 15mm Branco",      Unidade="m²",     QuantidadeNecessaria=4  },
                        new() { MaterialId=fita.Id,   Nome="Fita de Borda Branca", Unidade="metro",  QuantidadeNecessaria=10 },
                        new() { MaterialId=dobrad.Id, Nome="Dobradiça 35mm",       Unidade="unidade",QuantidadeNecessaria=4  },
                    }
                },
                new() {
                    Nome="Guarda-Roupa 3 Portas", Descricao_="Com gavetas internas",
                    PrecoBase=1800m, TempoProducaoDias=10,
                    Materiais = new() {
                        new() { MaterialId=mdf18.Id,  Nome="MDF 18mm Cru",   Unidade="m²",     QuantidadeNecessaria=8 },
                        new() { MaterialId=corred.Id, Nome="Corrediça 45cm", Unidade="par",    QuantidadeNecessaria=4 },
                        new() { MaterialId=dobrad.Id, Nome="Dobradiça 35mm", Unidade="unidade",QuantidadeNecessaria=6 },
                    }
                },
                new() {
                    Nome="Painel de TV", Descricao_="Painel com nichos e portas",
                    PrecoBase=1200m, TempoProducaoDias=7,
                    Materiais = new() {
                        new() { MaterialId=mdf15.Id, Nome="MDF 15mm Branco",      Unidade="m²",   QuantidadeNecessaria=6  },
                        new() { MaterialId=fita.Id,  Nome="Fita de Borda Branca", Unidade="metro",QuantidadeNecessaria=15 },
                    }
                },
                new() {
                    Nome="Estante de Livros", Descricao_="Prateleiras ajustáveis",
                    PrecoBase=480m, TempoProducaoDias=3,
                    Materiais = new() {
                        new() { MaterialId=mdf18.Id, Nome="MDF 18mm Cru",    Unidade="m²",   QuantidadeNecessaria=3 },
                        new() { MaterialId=paraf.Id, Nome="Parafuso 3.5x16", Unidade="caixa",QuantidadeNecessaria=1 },
                    }
                },
            };

            foreach (var p in produtos) await _produtos.InserirAsync(p);

            // ── Clientes ─────────────────────────────────────────────
            var clientes = new List<Cliente>
            {
                new() { Nome="Ana Silva",       Telefone="(11) 99999-1111", Email="ana@email.com",    Endereco="Rua das Flores, 100 - SP" },
                new() { Nome="Carlos Oliveira", Telefone="(11) 99999-2222", Email="carlos@email.com", Endereco="Av. Principal, 500 - SP"  },
                new() { Nome="Maria Santos",    Telefone="(11) 99999-3333", Email="maria@email.com",  Endereco="Rua do Comércio, 45 - SP" },
            };
            foreach (var c in clientes) await _clientes.InserirAsync(c);

            Console.WriteLine("✅ Dados de exemplo inseridos!");
        }
    }
}
