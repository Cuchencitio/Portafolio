using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TurnitoCL.Data;
using TurnitoCL.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace TurnitoCL.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly TurnitoDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(TurnitoDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue("Rol");

            if (userRole == Roles.Proveedor)
            {
                return await ProveedorDashboard(userId);
            }
            else
            {
                return await ConsumidorDashboard(userId);
            }
        }

        private async Task<IActionResult> ProveedorDashboard(int userId)
        {
            var proveedor = await _context.Proveedores
                .Include(p => p.Usuario)
                .Include(p => p.Servicios)
                .Include(p => p.Reservas)
                    .ThenInclude(r => r.Consumidor)
                .Include(p => p.Reservas)
                    .ThenInclude(r => r.Servicio)
                .FirstOrDefaultAsync(p => p.UsuarioId == userId);

            if (proveedor == null)
            {
                return NotFound("Proveedor no encontrado");
            }

            var model = new ProveedorDashboardViewModel
            {
                Proveedor = proveedor,
                TotalServicios = proveedor.Servicios.Count(s => s.Activo),
                ReservasHoy = proveedor.Reservas
                    .Where(r => r.FechaHora.Date == DateTime.Today && r.Estado != EstadosReserva.Cancelada)
                    .OrderBy(r => r.FechaHora)
                    .ToList(),
                ProximasReservas = proveedor.Reservas
                    .Where(r => r.FechaHora > DateTime.Now && r.Estado == EstadosReserva.Pendiente)
                    .OrderBy(r => r.FechaHora)
                    .Take(5)
                    .ToList(),
                ReservasDelMes = proveedor.Reservas
                    .Count(r => r.FechaHora.Month == DateTime.Now.Month &&
                               r.FechaHora.Year == DateTime.Now.Year),
                IngresosMes = proveedor.Reservas
                    .Where(r => r.FechaHora.Month == DateTime.Now.Month &&
                               r.FechaHora.Year == DateTime.Now.Year &&
                               r.Estado == EstadosReserva.Completada)
                    .Sum(r => r.Servicio.Precio)
            };

            return View("ProveedorDashboard", model);
        }

        private async Task<IActionResult> ConsumidorDashboard(int userId)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.ReservasComoConsumidor)
                    .ThenInclude(r => r.Proveedor)
                    .ThenInclude(p => p.Usuario)
                .Include(u => u.ReservasComoConsumidor)
                    .ThenInclude(r => r.Servicio)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            var model = new ConsumidorDashboardViewModel
            {
                Usuario = usuario,
                ProximasReservas = usuario.ReservasComoConsumidor
                    .Where(r => r.FechaHora > DateTime.Now && r.Estado != EstadosReserva.Cancelada)
                    .OrderBy(r => r.FechaHora)
                    .Take(5)
                    .ToList(),
                HistorialReservas = usuario.ReservasComoConsumidor
                    .Where(r => r.FechaHora <= DateTime.Now || r.Estado == EstadosReserva.Completada)
                    .OrderByDescending(r => r.FechaHora)
                    .Take(10)
                    .ToList(),
                TotalReservas = usuario.ReservasComoConsumidor.Count,
                ReservasPendientes = usuario.ReservasComoConsumidor
                    .Count(r => r.Estado == EstadosReserva.Pendiente && r.FechaHora > DateTime.Now)
            };

            return View("ConsumidorDashboard", model);
        }
    }

    // ViewModels para los dashboards
    public class ProveedorDashboardViewModel
    {
        public Proveedor Proveedor { get; set; } = new Proveedor();
        public List<Reserva> ReservasHoy { get; set; } = new List<Reserva>();
        public List<Reserva> ProximasReservas { get; set; } = new List<Reserva>();
        public int TotalServicios { get; set; }
        public int ReservasDelMes { get; set; }
        public decimal IngresosMes { get; set; }
    }

    public class ConsumidorDashboardViewModel
    {
        public Usuario Usuario { get; set; } = new Usuario();
        public List<Reserva> ProximasReservas { get; set; } = new List<Reserva>();
        public List<Reserva> HistorialReservas { get; set; } = new List<Reserva>();
        public int TotalReservas { get; set; }
        public int ReservasPendientes { get; set; }
    }
}