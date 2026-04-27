// =============================================================
// Controllers/PedidosController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using LionttoMoveis.Helpers;
using LionttoMoveis.Models;
using LionttoMoveis.Repository;
using LionttoMoveis.ViewModels;

namespace LionttoMoveis.Controllers
{
    public class PedidosController : Controller
    {
        private readonly PedidoRepository _pedidos;
        private readonly ClienteRepository _clientes;
        private readonly ProdutoRepository _produtos;

        public PedidosController(PedidoRepository ped, ClienteRepository cli, ProdutoRepository prod)
        {
            _pedidos = ped;
            _clientes = cli;
            _produtos = prod;
        }

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

        public async Task<IActionResult> Novo()
            => View(await MontarNovoPedidoVmAsync(new NovoPedidoViewModel()));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Novo(NovoPedidoViewModel vm)
        {
            NormalizarNovoPedido(vm);

            if (!ModelState.IsValid)
                return await RetornarNovoComErroAsync(vm, ObterPrimeiroErroModelState() ?? "Dados invalidos para criar pedido.");

            var cliente = await _clientes.ObterPorIdAsync(vm.ClienteId);
            if (cliente is null || string.IsNullOrWhiteSpace(cliente.Nome))
                return await RetornarNovoComErroAsync(vm, "Cliente selecionado e invalido.");

            DateTime? dataEntregaPrevista = null;
            if (!string.IsNullOrWhiteSpace(vm.DataEntregaPrevista))
            {
                if (!DateTime.TryParse(vm.DataEntregaPrevista, out var dataParseada))
                    return await RetornarNovoComErroAsync(vm, "Data de entrega prevista invalida.");

                dataEntregaPrevista = dataParseada;
            }

            var (itens, erroItens) = await MontarItensDoPedidoAsync(vm);
            if (erroItens is not null)
                return await RetornarNovoComErroAsync(vm, erroItens);

            var pedido = new Pedido
            {
                ClienteId = vm.ClienteId,
                ClienteNome = cliente.Nome.Trim(),
                Observacoes = vm.Observacoes ?? string.Empty,
                DataEntregaPrevista = dataEntregaPrevista,
                Itens = itens
            };

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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var pedido = await _pedidos.ObterComItensAsync(id);
            if (pedido?.Status == StatusPedido.Entregue)
            {
                TempData["Erro"] = "Nao e possivel excluir pedido ja entregue.";
                return RedirectToAction(nameof(Ver), new { id });
            }

            await _pedidos.ExcluirAsync(id);
            TempData["Sucesso"] = "Pedido removido.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<(List<ItemDoPedido> itens, string? erro)> MontarItensDoPedidoAsync(NovoPedidoViewModel vm)
        {
            var itens = new List<ItemDoPedido>();

            for (int i = 0; i < vm.ProdIds.Count; i++)
            {
                var produtoId = vm.ProdIds[i];
                if (produtoId <= 0)
                    continue;

                var quantidade = i < vm.ProdQtds.Count ? vm.ProdQtds[i] : 0;
                if (quantidade <= 0)
                    return (itens, "Informe quantidade valida para todos os produtos selecionados.");

                var prod = await _produtos.ObterPorIdAsync(produtoId);
                if (prod is null || string.IsNullOrWhiteSpace(prod.Nome))
                    return (itens, "Um dos produtos selecionados nao existe mais.");

                var personalizacao = i < vm.ProdPers.Count ? (vm.ProdPers[i] ?? string.Empty).Trim() : string.Empty;

                itens.Add(new ItemDoPedido
                {
                    ProdutoId = prod.Id,
                    ProdutoNome = prod.Nome.Trim(),
                    Quantidade = quantidade,
                    PrecoUnitario = prod.PrecoBase,
                    Personalizacoes = personalizacao
                });
            }

            if (!itens.Any())
                return (itens, "Adicione pelo menos um produto valido ao pedido.");

            return (itens, null);
        }

        private static void NormalizarNovoPedido(NovoPedidoViewModel vm)
        {
            vm.Observacoes = (vm.Observacoes ?? string.Empty).Trim();
            vm.DataEntregaPrevista = string.IsNullOrWhiteSpace(vm.DataEntregaPrevista)
                ? null
                : vm.DataEntregaPrevista.Trim();

            for (int i = 0; i < vm.ProdPers.Count; i++)
                vm.ProdPers[i] = (vm.ProdPers[i] ?? string.Empty).Trim();
        }

        private async Task<NovoPedidoViewModel> MontarNovoPedidoVmAsync(NovoPedidoViewModel vm)
        {
            vm.Clientes = await _clientes.ObterOrdenadosAsync();
            vm.Produtos = await _produtos.ObterOrdenadosAsync();
            return vm;
        }

        private async Task<IActionResult> RetornarNovoComErroAsync(NovoPedidoViewModel vm, string erro)
        {
            TempData["Erro"] = erro;
            var vmCompleto = await MontarNovoPedidoVmAsync(vm);
            return View("Novo", vmCompleto);
        }

        private string? ObterPrimeiroErroModelState()
            => ModelStateErrorHelper.ObterPrimeiroErroAmigavel(ModelState);
    }
}
