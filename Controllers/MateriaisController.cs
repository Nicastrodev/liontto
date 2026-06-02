// =============================================================
// Controllers/MateriaisController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Preencha os campos destacados e tente novamente.";
                return View(material);
            }

            try
            {
                await _materiais.InserirAsync(material);
            }
            catch (DbUpdateException)
            {
                TempData["Erro"] = "Nao foi possivel salvar o material. Revise quantidades e preco.";
                return View(material);
            }

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
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Preencha os campos destacados e tente novamente.";
                material.Id = id;
                material.CriadoEm = existente.CriadoEm;
                material.Quantidade = existente.Quantidade;
                return View(material);
            }

            material.Id = id;
            material.CriadoEm = existente.CriadoEm;
            material.Quantidade = existente.Quantidade;

            try
            {
                await _materiais.AtualizarAsync(material);
            }
            catch (DbUpdateException)
            {
                TempData["Erro"] = "Nao foi possivel atualizar o material. Revise quantidades e preco.";
                return View(material);
            }

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
            ModelState.Remove(nameof(MovimentacaoViewModel.Material));
            ModelState.Remove(nameof(MovimentacaoViewModel.Historico));

            if (!ModelState.IsValid)
            {
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Confira os dados da movimentacao e tente novamente.";
                return RedirectToAction(nameof(Movimentar), new { id });
            }

            if (!Enum.TryParse<TipoMovimentacao>(vm.Tipo, ignoreCase: true, out var tipo))
            {
                TempData["Erro"] = "Tipo de movimentacao invalido.";
                return RedirectToAction(nameof(Movimentar), new { id });
            }

            vm.Motivo = NormalizarOpcional(vm.Motivo);

            var erro = await _estoqueService.MovimentarAsync(id, tipo, vm.Quantidade, vm.Motivo);

            if (erro is not null)
            {
                TempData["Erro"] = erro;
                return RedirectToAction(nameof(Movimentar), new { id });
            }

            TempData["Sucesso"] = $"Movimentacao de {vm.Quantidade} registrada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [ActionName("Excluir")]
        public IActionResult ExcluirGet(int id)
        {
            TempData["Erro"] = "Use o botao de excluir na listagem para remover um material com seguranca.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirPost(int id, bool confirmarVinculos = false)
        {
            var mat = await _materiais.ObterPorIdAsync(id);
            if (mat is not null)
            {
                if (confirmarVinculos)
                {
                    var removido = await _materiais.ExcluirComVinculosAsync(id);
                    TempData[removido ? "Sucesso" : "Erro"] = removido
                        ? $"Material \"{mat.Nome}\" removido junto com seus vinculos de produtos e movimentacoes."
                        : "Material nao encontrado para exclusao.";

                    return RedirectToAction(nameof(Index));
                }

                try
                {
                    await _materiais.ExcluirAsync(id);
                    TempData["Sucesso"] = $"Material \"{mat.Nome}\" removido.";
                }
                catch (DbUpdateException)
                {
                    var uso = await _materiais.ObterResumoUsoAsync(id);
                    TempData["Erro"] = "Este material esta vinculado a produtos ou movimentacoes. Confirme abaixo se deseja remover mesmo assim.";
                    TempData["ConfirmarExclusaoMaterialId"] = id;
                    TempData["ConfirmarExclusaoMaterialNome"] = mat.Nome;
                    TempData["ConfirmarExclusaoMaterialUso"] =
                        $"{uso.Produtos} produto(s) e {uso.Movimentacoes} movimentacao(oes) serao desvinculados.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private static void NormalizarMaterial(Material material)
        {
            material.Nome = (material.Nome ?? string.Empty).Trim();
            material.Unidade = (material.Unidade ?? string.Empty).Trim();
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
