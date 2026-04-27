// =============================================================
// Controllers/MateriaisController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using LionttoMoveis.Helpers;
using LionttoMoveis.Models;
using LionttoMoveis.Repository;
using LionttoMoveis.Services;
using LionttoMoveis.ViewModels;

namespace LionttoMoveis.Controllers
{
    public class MateriaisController : Controller
    {
        private readonly MaterialRepository _materiais;
        private readonly MovimentacaoRepository _movimentacoes;
        private readonly EstoqueService _estoqueService;

        public MateriaisController(
            MaterialRepository mat,
            MovimentacaoRepository mov,
            EstoqueService estoqueService)
        {
            _materiais = mat;
            _movimentacoes = mov;
            _estoqueService = estoqueService;
        }

        public async Task<IActionResult> Index()
            => View(await _materiais.ObterOrdenadosAsync());

        public IActionResult Novo() => View(new Material());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Novo(Material material)
        {
            NormalizarMaterial(material);

            if (!ModelState.IsValid)
            {
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Preencha os campos obrigatorios.";
                return View(material);
            }

            await _materiais.InserirAsync(material);
            TempData["Sucesso"] = $"Material \"{material.Nome}\" cadastrado!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Editar(int id)
        {
            var mat = await _materiais.ObterPorIdAsync(id);
            if (mat is null) return NotFound();
            return View(mat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Material material)
        {
            var existente = await _materiais.ObterPorIdAsync(id);
            if (existente is null) return NotFound();

            NormalizarMaterial(material);

            if (!ModelState.IsValid)
            {
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Preencha os campos obrigatorios.";
                material.Id = id;
                material.CriadoEm = existente.CriadoEm;
                material.Quantidade = existente.Quantidade;
                return View(material);
            }

            material.Id = id;
            material.CriadoEm = existente.CriadoEm;
            material.Quantidade = existente.Quantidade;

            await _materiais.AtualizarAsync(material);
            TempData["Sucesso"] = "Material atualizado!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Movimentar(int id)
        {
            var mat = await _materiais.ObterPorIdAsync(id);
            if (mat is null) return NotFound();

            var historico = await _movimentacoes.ObterPorMaterialAsync(id);
            return View(new MovimentacaoViewModel { Material = mat, Historico = historico });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Movimentar(int id, MovimentacaoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Dados invalidos para movimentacao.";
                return RedirectToAction(nameof(Movimentar), new { id });
            }

            if (!Enum.TryParse<TipoMovimentacao>(vm.Tipo, ignoreCase: true, out var tipo))
            {
                TempData["Erro"] = "Tipo de movimentacao invalido.";
                return RedirectToAction(nameof(Movimentar), new { id });
            }

            vm.Motivo = (vm.Motivo ?? string.Empty).Trim();

            var erro = await _estoqueService.MovimentarAsync(id, tipo, vm.Quantidade, vm.Motivo);

            if (erro is not null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction(nameof(Movimentar), new { id });
            }

            TempData["Sucesso"] = $"Movimentacao de {vm.Quantidade} registrada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var mat = await _materiais.ObterPorIdAsync(id);
            if (mat is not null)
            {
                await _materiais.ExcluirAsync(id);
                TempData["Sucesso"] = $"Material \"{mat.Nome}\" removido.";
            }
            return RedirectToAction(nameof(Index));
        }

        private static void NormalizarMaterial(Material material)
        {
            material.Nome = (material.Nome ?? string.Empty).Trim();
            material.Unidade = (material.Unidade ?? string.Empty).Trim();
        }

        private string? ObterPrimeiroErroModelState()
            => ModelStateErrorHelper.ObterPrimeiroErroAmigavel(ModelState);
    }
}
