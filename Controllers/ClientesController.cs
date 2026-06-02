// =============================================================
// Controllers/ClientesController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LionttoMoveis.Helpers;
using LionttoMoveis.Models;
using LionttoMoveis.Repository;

namespace LionttoMoveis.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ClienteRepository _clientes;
        private readonly PedidoRepository _pedidos;

        public ClientesController(ClienteRepository cli, PedidoRepository ped)
        {
            _clientes = cli;
            _pedidos = ped;
        }

        public async Task<IActionResult> Index()
            => View(await _clientes.ObterOrdenadosAsync());

        public IActionResult Novo() => View(new Cliente());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Novo(Cliente cliente)
        {
            NormalizarCliente(cliente);

            if (!ModelState.IsValid)
            {
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Preencha os campos destacados e tente novamente.";
                return View(cliente);
            }

            try
            {
                await _clientes.InserirAsync(cliente);
            }
            catch (DbUpdateException)
            {
                TempData["Erro"] = "Nao foi possivel salvar o cliente. Revise os campos e tente novamente.";
                return View(cliente);
            }
            TempData["Sucesso"] = $"Cliente \"{cliente.Nome}\" cadastrado!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Editar(int id)
        {
            var cli = await _clientes.ObterPorIdAsync(id);
            if (cli is null) return NotFound();
            return View(cli);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Cliente cliente)
        {
            var existente = await _clientes.ObterPorIdAsync(id);
            if (existente is null) return NotFound();

            NormalizarCliente(cliente);

            if (!ModelState.IsValid)
            {
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Preencha os campos destacados e tente novamente.";
                cliente.Id = id;
                cliente.CriadoEm = existente.CriadoEm;
                return View(cliente);
            }

            cliente.Id = id;
            cliente.CriadoEm = existente.CriadoEm;

            try
            {
                await _clientes.AtualizarAsync(cliente);
            }
            catch (DbUpdateException)
            {
                TempData["Erro"] = "Nao foi possivel atualizar o cliente. Revise os campos e tente novamente.";
                return View(cliente);
            }
            TempData["Sucesso"] = "Cliente atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [ActionName("Excluir")]
        public IActionResult ExcluirGet(int id)
        {
            TempData["Erro"] = "Use o botao de excluir na listagem para remover um cliente com seguranca.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirPost(int id)
        {
            var temPedidos = (await _pedidos.ObterPorClienteAsync(id)).Any();
            if (temPedidos)
            {
                TempData["Erro"] = "Nao e possivel excluir: cliente possui pedidos!";
            }
            else
            {
                try
                {
                    await _clientes.ExcluirAsync(id);
                    TempData["Sucesso"] = "Cliente removido.";
                }
                catch (DbUpdateException)
                {
                    TempData["Erro"] = "Nao e possivel excluir este cliente porque ele esta vinculado a outros registros.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private static void NormalizarCliente(Cliente cliente)
        {
            cliente.Nome = (cliente.Nome ?? string.Empty).Trim();
            cliente.Telefone = NormalizarOpcional(cliente.Telefone);
            cliente.Email = NormalizarOpcional(cliente.Email);
            cliente.Endereco = NormalizarOpcional(cliente.Endereco);
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
