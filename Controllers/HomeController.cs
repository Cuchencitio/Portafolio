using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TurnitoCL.Models;
using TurnitoCL.Data;
using Microsoft.EntityFrameworkCore;

namespace TurnitoCL.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TurnitoDbContext _context;

        public HomeController(ILogger<HomeController> logger, TurnitoDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Obtener algunos datos para mostrar en landing
            var model = new HomeViewModel
            {
                TotalProveedores = await _context.Proveedores.CountAsync(),
                TotalServicios = await _context.Servicios.Where(s => s.Activo).CountAsync(),
                ServiciosDestacados = await _context.Servicios
                    .Where(s => s.Activo)
                    .Include(s => s.Proveedor)
                    .ThenInclude(p => p.Usuario)
                    .Take(6)
                    .ToListAsync()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // ViewModel para Home
    public class HomeViewModel
    {
        public int TotalProveedores { get; set; }
        public int TotalServicios { get; set; }
        public List<Servicio> ServiciosDestacados { get; set; } = new List<Servicio>();
    }
}