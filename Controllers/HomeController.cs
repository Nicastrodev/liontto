// =============================================================
// Controllers/HomeController.cs
// =============================================================

using Microsoft.AspNetCore.Mvc;
using LionttoMoveis.Repository;
using LionttoMoveis.Models;

namespace LionttoMoveis.Controllers
{
    // ViewModel inline do Dashboard (pode ser movido para ViewModels.cs)
    public class DashboardViewModel
    {
        public int TotalClientes  { get; set; }
        public int TotalProdutos  { get; set; }
        public int TotalMateriais { get; set; }
        public Dictionary<string, int> PedidosPorStatus { get; set; } = new();
        public List<Material> AlertasEstoque  { get; set; } = new();
        public List<Pedido>   UltimosPedidos  { get; set; } = new();
        public List<Material> EstoqueGrafico  { get; set; } = new();
    }

    public class HomeController : Controller
    {
        private readonly MaterialRepository _materiais;
        private readonly ProdutoRepository  _produtos;
        private readonly ClienteRepository  _clientes;
        private readonly PedidoRepository   _pedidos;

        public HomeController(
            MaterialRepository mat,
            ProdutoRepository  prod,
            ClienteRepository  cli,
            PedidoRepository   ped)
        {
            _materiais = mat; _produtos = prod;
            _clientes  = cli; _pedidos  = ped;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalClientes    = (int)await _clientes.ContarAsync(),
                TotalProdutos    = (int)await _produtos.ContarAsync(),
                TotalMateriais   = (int)await _materiais.ContarAsync(),
                PedidosPorStatus = await _pedidos.ContarPorStatusAsync(),
                AlertasEstoque   = await _materiais.ObterEstoqueBaixoAsync(),
                UltimosPedidos   = (await _pedidos.ObterTodosOrdenadosAsync()).Take(5).ToList(),
                EstoqueGrafico   = (await _materiais.ObterOrdenadosAsync()).Take(8).ToList(),
            };

            return View(vm);
        }
    }
}
