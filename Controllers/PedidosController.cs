// =============================================================
// Controllers/PedidosController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using LionttoMoveis.Models;
using LionttoMoveis.Repository;
using LionttoMoveis.ViewModels;

namespace LionttoMoveis.Controllers
{
    public class PedidosController : Controller
    {
        private readonly PedidoRepository  _pedidos;
        private readonly ClienteRepository _clientes;
        private readonly ProdutoRepository _produtos;

        public PedidosController(PedidoRepository ped, ClienteRepository cli, ProdutoRepository prod)
        { _pedidos = ped; _clientes = cli; _produtos = prod; }

        public async Task<IActionResult> Index(string? status)
        {
            List<Pedido> lista;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusPedido>(status, out var s))
                lista = await _pedidos.ObterPorStatusAsync(s);
            else
                lista = await _pedidos.ObterTodosOrdenadosAsync();

            ViewBag.StatusFiltro = status;
            return View(lista);
        }

        public async Task<IActionResult> Novo() => View(new NovoPedidoViewModel
        {
            Clientes = await _clientes.ObterOrdenadosAsync(),
            Produtos = await _produtos.ObterOrdenadosAsync()
        });

        [HttpPost]
        public async Task<IActionResult> Novo(NovoPedidoViewModel vm)
        {
            if (vm.ClienteId == 0)
            { TempData["Erro"] = "Selecione um cliente."; return RedirectToAction(nameof(Novo)); }

            var cliente = await _clientes.ObterPorIdAsync(vm.ClienteId);

            var pedido = new Pedido
            {
                ClienteId           = vm.ClienteId,
                ClienteNome         = cliente?.Nome ?? "",
                Observacoes         = vm.Observacoes ?? "",
                DataEntregaPrevista = string.IsNullOrEmpty(vm.DataEntregaPrevista)
                    ? null
                    : DateTime.Parse(vm.DataEntregaPrevista),
            };

            // Monta os itens a partir das listas paralelas do formulário
            for (int i = 0; i < vm.ProdIds.Count; i++)
            {
                if (vm.ProdIds[i] == 0) continue;
                var prod = await _produtos.ObterPorIdAsync(vm.ProdIds[i]);
                if (prod is null) continue;

                pedido.Itens.Add(new ItemDoPedido
                {
                    ProdutoId       = prod.Id,
                    ProdutoNome     = prod.Nome,
                    Quantidade      = i < vm.ProdQtds.Count ? vm.ProdQtds[i] : 1,
                    PrecoUnitario   = prod.PrecoBase,
                    Personalizacoes = i < vm.ProdPers.Count ? vm.ProdPers[i] : ""
                });
            }

            pedido.RecalcularTotal();
            await _pedidos.InserirComItensAsync(pedido);

            TempData["Sucesso"] = "Pedido criado com sucesso!";
            return RedirectToAction(nameof(Ver), new { id = pedido.Id });
        }

        public async Task<IActionResult> Ver(int id)
        {
            var pedido = await _pedidos.ObterComItensAsync(id);
            if (pedido is null) return NotFound();
            return View(new DetalhesPedidoViewModel { Pedido = pedido });
        }

        [HttpPost]
        public async Task<IActionResult> AtualizarStatus(int id, string acao)
        {
            var pedido = await _pedidos.ObterComItensAsync(id);
            if (pedido is null) return NotFound();

            if (acao == "avancar")
                pedido.AvancarStatus();
            else if (acao == "voltar" && pedido.Status != StatusPedido.Aguardando)
                pedido.Status = (StatusPedido)((int)pedido.Status - 1);

            await _pedidos.AtualizarStatusAsync(pedido);
            TempData["Sucesso"] = $"Status atualizado: {pedido.StatusLabel}";
            return RedirectToAction(nameof(Ver), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            var pedido = await _pedidos.ObterComItensAsync(id);
            if (pedido?.Status == StatusPedido.Entregue)
            { TempData["Erro"] = "Não é possível excluir pedido já entregue."; return RedirectToAction(nameof(Ver), new { id }); }

            await _pedidos.ExcluirAsync(id);
            TempData["Sucesso"] = "Pedido removido.";
            return RedirectToAction(nameof(Index));
        }
    }
}
