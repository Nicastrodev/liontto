// =============================================================
// Controllers/MateriaisController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using LionttoMoveis.Models;
using LionttoMoveis.Repository;
using LionttoMoveis.Services;
using LionttoMoveis.ViewModels;

namespace LionttoMoveis.Controllers
{
    public class MateriaisController : Controller
    {
        private readonly MaterialRepository     _materiais;
        private readonly MovimentacaoRepository _movimentacoes;
        private readonly EstoqueService         _estoqueService;

        public MateriaisController(
            MaterialRepository mat,
            MovimentacaoRepository mov,
            EstoqueService estoqueService)
        {
            _materiais      = mat;
            _movimentacoes  = mov;
            _estoqueService = estoqueService;
        }

        public async Task<IActionResult> Index()
            => View(await _materiais.ObterOrdenadosAsync());

        public IActionResult Novo() => View(new Material());

        [HttpPost]
        public async Task<IActionResult> Novo(Material material)
        {
            if (string.IsNullOrWhiteSpace(material.Nome) || string.IsNullOrWhiteSpace(material.Unidade))
            { TempData["Erro"] = "Nome e unidade são obrigatórios."; return View(material); }

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
        public async Task<IActionResult> Editar(int id, Material material)
        {
            material.Id = id;
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
        public async Task<IActionResult> Movimentar(int id, MovimentacaoViewModel vm)
        {
            var tipo = vm.Tipo == "Entrada" ? TipoMovimentacao.Entrada : TipoMovimentacao.Saida;
            var erro = await _estoqueService.MovimentarAsync(id, tipo, vm.Quantidade, vm.Motivo);

            if (erro is not null)
            { TempData["Erro"] = erro; return RedirectToAction(nameof(Movimentar), new { id }); }

            TempData["Sucesso"] = $"Movimentação de {vm.Quantidade} registrada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
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
    }
}
