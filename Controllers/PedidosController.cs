// =============================================================
// Controllers/PedidosController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                return await RetornarNovoComErroAsync(vm, ObterPrimeiroErroModelState() ?? "Preencha os campos destacados e tente novamente.");

            var cliente = await _clientes.ObterPorIdAsync(vm.ClienteId);
            if (cliente is null || string.IsNullOrWhiteSpace(cliente.Nome))
                return await RetornarNovoComErroAsync(vm, "Selecione um cliente valido.");

            DateTime? dataEntregaPrevista = null;
            if (!string.IsNullOrWhiteSpace(vm.DataEntregaPrevista))
            {
                if (!DateTime.TryParse(vm.DataEntregaPrevista, out var dataParseada))
                    return await RetornarNovoComErroAsync(vm, "Informe uma data de entrega prevista valida.");

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
            try
            {
                await _pedidos.InserirComItensAsync(pedido);
            }
            catch (DbUpdateException)
            {
                return await RetornarNovoComErroAsync(vm, "Nao foi possivel salvar o pedido. Revise cliente, produtos e quantidades.");
            }

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

        [HttpGet]
        [ActionName("Excluir")]
        public IActionResult ExcluirGet(int id)
        {
            TempData["Erro"] = "Use o botao de excluir na listagem para remover um pedido com seguranca.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ActionName("Excluir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirPost(int id)
        {
            var pedido = await _pedidos.ObterComItensAsync(id);
            if (pedido?.Status == StatusPedido.Entregue)
            {
                TempData["Erro"] = "Nao e possivel excluir pedido ja entregue.";
                return RedirectToAction(nameof(Ver), new { id });
            }

            try
            {
                await _pedidos.ExcluirAsync(id);
                TempData["Sucesso"] = "Pedido removido.";
            }
            catch (DbUpdateException)
            {
                TempData["Erro"] = "Nao e possivel excluir este pedido porque ele esta vinculado a outros registros.";
            }

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
                    return (itens, "Digite uma quantidade valida para todos os produtos selecionados.");

                var prod = await _produtos.ObterPorIdAsync(produtoId);
                if (prod is null || string.IsNullOrWhiteSpace(prod.Nome))
                    return (itens, "Um dos produtos selecionados nao existe mais.");

                var personalizacao = i < vm.ProdPers.Count ? (vm.ProdPers[i] ?? string.Empty).Trim() : string.Empty;
                var personalizacaoNormalizada = NormalizarOpcional(personalizacao);

                itens.Add(new ItemDoPedido
                {
                    ProdutoId = prod.Id,
                    ProdutoNome = prod.Nome.Trim(),
                    Quantidade = quantidade,
                    PrecoUnitario = prod.PrecoBase,
                    Personalizacoes = personalizacaoNormalizada
                });
            }

            if (!itens.Any())
                return (itens, "Adicione pelo menos um produto valido ao pedido.");

            return (itens, null);
        }

        private static void NormalizarNovoPedido(NovoPedidoViewModel vm)
        {
            vm.Observacoes = NormalizarOpcional(vm.Observacoes);
            vm.DataEntregaPrevista = string.IsNullOrWhiteSpace(vm.DataEntregaPrevista)
                ? null
                : vm.DataEntregaPrevista.Trim();

            for (int i = 0; i < vm.ProdPers.Count; i++)
                vm.ProdPers[i] = NormalizarOpcional(vm.ProdPers[i]);
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

        private static string NormalizarOpcional(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            return texto.Trim();
        }
    }
}
