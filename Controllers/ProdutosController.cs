// =============================================================
// Controllers/ProdutosController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using LionttoMoveis.Models;
using LionttoMoveis.Repository;
using LionttoMoveis.ViewModels;
using LionttoMoveis.Data;

namespace LionttoMoveis.Controllers
{
    // ViewModel para o formulário de produto (novo/editar)
    public class ProdutoFormViewModel
    {
        public Produto Produto { get; set; } = new();
        public List<Material> Materiais { get; set; } = new();
        public bool EhEdicao { get; set; }
    }

    public class ProdutosController : Controller
    {
        private readonly ProdutoRepository  _produtos;
        private readonly MaterialRepository _materiais;
        private readonly AppDbContext       _ctx;

        public ProdutosController(ProdutoRepository prod, MaterialRepository mat, AppDbContext ctx)
        { _produtos = prod; _materiais = mat; _ctx = ctx; }

        public async Task<IActionResult> Index()
            => View(await _produtos.ObterOrdenadosAsync());

        public async Task<IActionResult> Novo()
            => View(new ProdutoFormViewModel
            {
                Produto   = new Produto(),
                Materiais = await _materiais.ObterOrdenadosAsync(),
                EhEdicao  = false
            });

        [HttpPost]
        public async Task<IActionResult> Novo(Produto produto, List<int> matIds, List<double> matQtds)
        {
            produto.Materiais = await MontarMateriais(matIds, matQtds);
            await _produtos.InserirAsync(produto);
            TempData["Sucesso"] = $"Produto \"{produto.Nome}\" cadastrado!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Ver(int id)
        {
            var prod = await _produtos.ObterComMateriaisAsync(id);
            if (prod is null) return NotFound();

            var matsComEstoque = new List<(MaterialDoProduto mat, double estoqueAtual)>();
            foreach (var m in prod.Materiais)
            {
                var matDb = await _materiais.ObterPorIdAsync(m.MaterialId);
                matsComEstoque.Add((m, matDb?.Quantidade ?? 0));
            }
            ViewBag.MateriaisEstoque = matsComEstoque;
            return View(prod);
        }

        public async Task<IActionResult> Editar(int id)
        {
            var prod = await _produtos.ObterComMateriaisAsync(id);
            if (prod is null) return NotFound();
            return View(new ProdutoFormViewModel
            {
                Produto   = prod,
                Materiais = await _materiais.ObterOrdenadosAsync(),
                EhEdicao  = true
            });
        }

        [HttpPost]
        public async Task<IActionResult> Editar(int id, Produto produto, List<int> matIds, List<double> matQtds)
        {
            // No MySQL relacional, ao editar materiais do produto:
            // 1. Remove os materiais antigos (cascade já configurado no DbContext)
            // 2. Insere os novos
            var existente = await _produtos.ObterComMateriaisAsync(id);
            if (existente is null) return NotFound();

            // Atualiza campos do produto
            existente.Nome              = produto.Nome;
            existente.Descricao_        = produto.Descricao_;
            existente.PrecoBase         = produto.PrecoBase;
            existente.TempoProducaoDias = produto.TempoProducaoDias;

            // Remove materiais antigos
            _ctx.MateriaisDoProduto.RemoveRange(existente.Materiais);

            // Adiciona novos
            existente.Materiais = await MontarMateriais(matIds, matQtds);
            foreach (var m in existente.Materiais)
                m.ProdutoId = id;

            await _ctx.SaveChangesAsync();
            TempData["Sucesso"] = "Produto atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            await _produtos.ExcluirAsync(id);
            TempData["Sucesso"] = "Produto removido.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<MaterialDoProduto>> MontarMateriais(List<int> ids, List<double> qtds)
        {
            var lista = new List<MaterialDoProduto>();
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == 0) continue;
                var mat = await _materiais.ObterPorIdAsync(ids[i]);
                if (mat is null) continue;
                lista.Add(new MaterialDoProduto
                {
                    MaterialId           = ids[i],
                    Nome                 = mat.Nome,
                    Unidade              = mat.Unidade,
                    QuantidadeNecessaria = i < qtds.Count ? qtds[i] : 1
                });
            }
            return lista;
        }
    }
}
