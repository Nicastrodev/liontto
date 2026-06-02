// =============================================================
// Controllers/ProdutosController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LionttoMoveis.Helpers;
using LionttoMoveis.Models;
using LionttoMoveis.Repository;
using LionttoMoveis.Data;

namespace LionttoMoveis.Controllers
{
    // ViewModel para o formulario de produto (novo/editar)
    public class ProdutoFormViewModel
    {
        public Produto Produto { get; set; } = new();
        public List<Material> Materiais { get; set; } = new();
        public bool EhEdicao { get; set; }
    }

    public class ProdutosController : Controller
    {
        private readonly ProdutoRepository _produtos;
        private readonly MaterialRepository _materiais;
        private readonly AppDbContext _ctx;

        public ProdutosController(ProdutoRepository prod, MaterialRepository mat, AppDbContext ctx)
        {
            _produtos = prod;
            _materiais = mat;
            _ctx = ctx;
        }

        public async Task<IActionResult> Index()
            => View(await _produtos.ObterTodosComMateriaisAsync());

        public async Task<IActionResult> Novo()
            => View(await MontarFormularioAsync(new Produto(), ehEdicao: false));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Novo(Produto produto, List<int>? matIds, List<double>? matQtds)
        {
            NormalizarProduto(produto);
            matIds ??= new List<int>();
            matQtds ??= new List<double>();

            if (!ModelState.IsValid)
                return await RetornarFormularioComErroAsync(
                    "Novo",
                    produto,
                    false,
                    ObterPrimeiroErroModelState() ?? "Preencha os campos destacados e tente novamente.");

            var (materiaisDoProduto, erroMateriais) = await MontarMateriaisAsync(matIds, matQtds);
            if (erroMateriais is not null)
                return await RetornarFormularioComErroAsync("Novo", produto, false, erroMateriais);

            produto.Materiais = materiaisDoProduto;
            try
            {
                await _produtos.InserirAsync(produto);
            }
            catch (DbUpdateException)
            {
                return await RetornarFormularioComErroAsync("Novo", produto, false, "Nao foi possivel salvar o produto. Revise preco, materiais e quantidades.");
            }

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

            return View(await MontarFormularioAsync(prod, ehEdicao: true));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Produto produto, List<int>? matIds, List<double>? matQtds)
        {
            var existente = await _produtos.ObterComMateriaisAsync(id);
            if (existente is null) return NotFound();

            NormalizarProduto(produto);
            matIds ??= new List<int>();
            matQtds ??= new List<double>();

            if (!ModelState.IsValid)
            {
                produto.Id = id;
                produto.CriadoEm = existente.CriadoEm;
                return await RetornarFormularioComErroAsync(
                    "Editar",
                    produto,
                    true,
                    ObterPrimeiroErroModelState() ?? "Preencha os campos destacados e tente novamente.");
            }

            var (materiaisDoProduto, erroMateriais) = await MontarMateriaisAsync(matIds, matQtds);
            if (erroMateriais is not null)
            {
                produto.Id = id;
                produto.CriadoEm = existente.CriadoEm;
                return await RetornarFormularioComErroAsync("Editar", produto, true, erroMateriais);
            }

            existente.Nome = produto.Nome;
            existente.Descricao_ = produto.Descricao_;
            existente.PrecoBase = produto.PrecoBase;
            existente.TempoProducaoDias = produto.TempoProducaoDias;

            _ctx.MateriaisDoProduto.RemoveRange(existente.Materiais);

            existente.Materiais = materiaisDoProduto;
            foreach (var m in existente.Materiais)
                m.ProdutoId = id;

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return await RetornarFormularioComErroAsync("Editar", produto, true, "Nao foi possivel salvar o produto. Revise preco, materiais e quantidades.");
            }
            TempData["Sucesso"] = "Produto atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [ActionName("Excluir")]
        public IActionResult ExcluirGet(int id)
        {
            TempData["Erro"] = "Use o botao de excluir na listagem para remover um produto com seguranca.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirPost(int id)
        {
            try
            {
                await _produtos.ExcluirAsync(id);
                TempData["Sucesso"] = "Produto removido.";
            }
            catch (DbUpdateException)
            {
                TempData["Erro"] = "Nao e possivel excluir este produto porque ele esta vinculado a pedidos ou outros registros.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<ProdutoFormViewModel> MontarFormularioAsync(Produto produto, bool ehEdicao)
            => new()
            {
                Produto = produto,
                Materiais = await _materiais.ObterOrdenadosAsync(),
                EhEdicao = ehEdicao
            };

        private async Task<IActionResult> RetornarFormularioComErroAsync(string viewName, Produto produto, bool ehEdicao, string erro)
        {
            TempData["Erro"] = erro;
            var vm = await MontarFormularioAsync(produto, ehEdicao);
            return View(viewName, vm);
        }

        private async Task<(List<MaterialDoProduto> materiais, string? erro)> MontarMateriaisAsync(List<int> ids, List<double> qtds)
        {
            var lista = new List<MaterialDoProduto>();
            var materiaisVistos = new HashSet<int>();

            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] <= 0)
                    continue;

                if (!materiaisVistos.Add(ids[i]))
                    return (lista, "Nao repita o mesmo material no produto.");

                var quantidade = i < qtds.Count ? qtds[i] : 0;
                if (quantidade <= 0)
                    return (lista, "Informe quantidade valida para todos os materiais selecionados.");

                var mat = await _materiais.ObterPorIdAsync(ids[i]);
                if (mat is null)
                    return (lista, "Um dos materiais selecionados nao existe mais.");

                lista.Add(new MaterialDoProduto
                {
                    MaterialId = ids[i],
                    Nome = mat.Nome,
                    Unidade = mat.Unidade,
                    QuantidadeNecessaria = quantidade
                });
            }

            if (!lista.Any())
                return (lista, "Adicione pelo menos um material valido ao produto.");

            return (lista, null);
        }

        private static void NormalizarProduto(Produto produto)
        {
            produto.Nome = (produto.Nome ?? string.Empty).Trim();
            produto.Descricao_ = NormalizarOpcional(produto.Descricao_);
        }

        private static string NormalizarOpcional(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            return texto.Trim();
        }

        private string? ObterPrimeiroErroModelState()
            => ModelStateErrorHelper.ObterPrimeiroErroAmigavel(ModelState);
    }
}
