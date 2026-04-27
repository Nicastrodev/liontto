// =============================================================
// Controllers/ClientesController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
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
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Preencha os campos obrigatorios.";
                return View(cliente);
            }

            await _clientes.InserirAsync(cliente);
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
                TempData["Erro"] = ObterPrimeiroErroModelState() ?? "Preencha os campos obrigatorios.";
                cliente.Id = id;
                cliente.CriadoEm = existente.CriadoEm;
                return View(cliente);
            }

            cliente.Id = id;
            cliente.CriadoEm = existente.CriadoEm;

            await _clientes.AtualizarAsync(cliente);
            TempData["Sucesso"] = "Cliente atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var temPedidos = (await _pedidos.ObterPorClienteAsync(id)).Any();
            if (temPedidos)
            {
                TempData["Erro"] = "Nao e possivel excluir: cliente possui pedidos!";
            }
            else
            {
                await _clientes.ExcluirAsync(id);
                TempData["Sucesso"] = "Cliente removido.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static void NormalizarCliente(Cliente cliente)
        {
            cliente.Nome = (cliente.Nome ?? string.Empty).Trim();
            cliente.Telefone = (cliente.Telefone ?? string.Empty).Trim();
            cliente.Email = (cliente.Email ?? string.Empty).Trim();
            cliente.Endereco = (cliente.Endereco ?? string.Empty).Trim();
        }

        private string? ObterPrimeiroErroModelState()
            => ModelStateErrorHelper.ObterPrimeiroErroAmigavel(ModelState);
    }
}
