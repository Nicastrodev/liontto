// =============================================================
// Controllers/ClientesController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using LionttoMoveis.Models;
using LionttoMoveis.Repository;

namespace LionttoMoveis.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ClienteRepository _clientes;
        private readonly PedidoRepository  _pedidos;

        public ClientesController(ClienteRepository cli, PedidoRepository ped)
        { _clientes = cli; _pedidos = ped; }

        public async Task<IActionResult> Index()
            => View(await _clientes.ObterOrdenadosAsync());

        public IActionResult Novo() => View(new Cliente());

        [HttpPost]
        public async Task<IActionResult> Novo(Cliente cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente.Nome))
            { TempData["Erro"] = "Nome é obrigatório."; return View(cliente); }

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
        public async Task<IActionResult> Editar(int id, Cliente cliente)
        {
            cliente.Id = id;
            await _clientes.AtualizarAsync(cliente);
            TempData["Sucesso"] = "Cliente atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            var temPedidos = (await _pedidos.ObterPorClienteAsync(id)).Any();
            if (temPedidos)
                TempData["Erro"] = "Não é possível excluir: cliente possui pedidos!";
            else
            {
                await _clientes.ExcluirAsync(id);
                TempData["Sucesso"] = "Cliente removido.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
