using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TurnitoCL.Data;
using TurnitoCL.Models;
using TurnitoCL.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace TurnitoCL.Controllers
{
    [Authorize(Policy = "RequireProveedor")]
    public class ServiciosController : Controller
    {
        private readonly TurnitoDbContext _context;
        private readonly ILogger<ServiciosController> _logger;

        public ServiciosController(TurnitoDbContext context, ILogger<ServiciosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Servicios
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var proveedor = await _context.Proveedores
                .Include(p => p.Usuario)
                .Include(p => p.Servicios)
                .FirstOrDefaultAsync(p => p.UsuarioId == userId);

            if (proveedor == null)
            {
                return NotFound("Proveedor no encontrado");
            }

            var model = new GestionServiciosViewModel
            {
                Proveedor = proveedor,
                Servicios = proveedor.Servicios.OrderByDescending(s => s.FechaCreacion).ToList(),
                ServiciosActivos = proveedor.Servicios.Count(s => s.Activo),
                ServiciosInactivos = proveedor.Servicios.Count(s => !s.Activo),
                TotalServicios = proveedor.Servicios.Count
            };

            return View(model);
        }

        // GET: /Servicios/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            _logger.LogInformation("Accediendo a GET Crear servicio");

            var model = new ServicioViewModel
            {
                Activo = true,
                DuracionMinutos = 30
            };

            _logger.LogInformation("Modelo creado: {Activo}, {Duracion}", model.Activo, model.DuracionMinutos);

            return View(model);
        }

        // POST: /Servicios/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ServicioViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                    var proveedor = await _context.Proveedores
                        .FirstOrDefaultAsync(p => p.UsuarioId == userId);

                    if (proveedor == null)
                    {
                        return NotFound("Proveedor no encontrado");
                    }

                    var servicio = new Servicio
                    {
                        ProveedorId = proveedor.Id,
                        Nombre = model.Nombre,
                        Descripcion = model.Descripcion,
                        Precio = model.Precio,
                        DuracionMinutos = model.DuracionMinutos,
                        Activo = model.Activo,
                        FechaCreacion = DateTime.Now
                    };

                    _context.Servicios.Add(servicio);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Nuevo servicio creado: {ServicioId} - {Nombre} por proveedor {ProveedorId}",
                        servicio.Id, servicio.Nombre, proveedor.Id);

                    TempData["SuccessMessage"] = "Servicio creado exitosamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear servicio");
                    ModelState.AddModelError("", "Ocurrió un error al crear el servicio. Inténtalo nuevamente.");
                }
            }

            return View(model);
        }

        // GET: /Servicios/Editar/5
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var servicio = await _context.Servicios
                .Include(s => s.Proveedor)
                .FirstOrDefaultAsync(s => s.Id == id && s.Proveedor.UsuarioId == userId);

            if (servicio == null)
            {
                return NotFound();
            }

            var model = new ServicioViewModel
            {
                Id = servicio.Id,
                Nombre = servicio.Nombre,
                Descripcion = servicio.Descripcion,
                Precio = servicio.Precio,
                DuracionMinutos = servicio.DuracionMinutos,
                Activo = servicio.Activo
            };

            return View(model);
        }

        // POST: /Servicios/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, ServicioViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                    var servicio = await _context.Servicios
                        .Include(s => s.Proveedor)
                        .FirstOrDefaultAsync(s => s.Id == id && s.Proveedor.UsuarioId == userId);

                    if (servicio == null)
                    {
                        return NotFound();
                    }

                    servicio.Nombre = model.Nombre;
                    servicio.Descripcion = model.Descripcion;
                    servicio.Precio = model.Precio;
                    servicio.DuracionMinutos = model.DuracionMinutos;
                    servicio.Activo = model.Activo;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Servicio actualizado: {ServicioId} - {Nombre}", servicio.Id, servicio.Nombre);

                    TempData["SuccessMessage"] = "Servicio actualizado exitosamente.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar servicio {ServicioId}", id);
                    ModelState.AddModelError("", "Ocurrió un error al actualizar el servicio. Inténtalo nuevamente.");
                }
            }

            return View(model);
        }

        // GET: /Servicios/Detalle/5
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var servicio = await _context.Servicios
                .Include(s => s.Proveedor)
                .ThenInclude(p => p.Usuario)
                .Include(s => s.Reservas.Where(r => r.Estado != EstadosReserva.Cancelada))
                .ThenInclude(r => r.Consumidor)
                .FirstOrDefaultAsync(s => s.Id == id && s.Proveedor.UsuarioId == userId);

            if (servicio == null)
            {
                return NotFound();
            }

            var model = new DetalleServicioViewModel
            {
                Servicio = servicio,
                TotalReservas = servicio.Reservas.Count,
                ReservasEsteMes = servicio.Reservas.Count(r =>
                    r.FechaHora.Month == DateTime.Now.Month &&
                    r.FechaHora.Year == DateTime.Now.Year),
                IngresosTotales = servicio.Reservas
                    .Where(r => r.Estado == EstadosReserva.Completada)
                    .Sum(r => r.Servicio.Precio),
                UltimasReservas = servicio.Reservas
                    .OrderByDescending(r => r.FechaHora)
                    .Take(10)
                    .ToList()
            };

            return View(model);
        }

        // POST: /Servicios/CambiarEstado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var servicio = await _context.Servicios
                .Include(s => s.Proveedor)
                .FirstOrDefaultAsync(s => s.Id == id && s.Proveedor.UsuarioId == userId);

            if (servicio == null)
            {
                return NotFound();
            }

            // Verificar si hay reservas futuras activas
            var tieneCitasFuturas = await _context.Reservas
                .AnyAsync(r => r.ServicioId == id &&
                              r.FechaHora > DateTime.Now &&
                              r.Estado == EstadosReserva.Pendiente);

            if (servicio.Activo && tieneCitasFuturas)
            {
                TempData["ErrorMessage"] = "No puedes desactivar este servicio porque tiene citas futuras programadas.";
            }
            else
            {
                servicio.Activo = !servicio.Activo;
                await _context.SaveChangesAsync();

                var estadoTexto = servicio.Activo ? "activado" : "desactivado";
                TempData["SuccessMessage"] = $"Servicio {estadoTexto} exitosamente.";

                _logger.LogInformation("Servicio {ServicioId} {Estado} por proveedor {ProveedorId}",
                    servicio.Id, estadoTexto, servicio.ProveedorId);
            }

            return RedirectToAction("Index");
        }

        // POST: /Servicios/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var servicio = await _context.Servicios
                .Include(s => s.Proveedor)
                .Include(s => s.Reservas)
                .FirstOrDefaultAsync(s => s.Id == id && s.Proveedor.UsuarioId == userId);

            if (servicio == null)
            {
                return NotFound();
            }

            // Verificar si tiene reservas
            if (servicio.Reservas.Any())
            {
                TempData["ErrorMessage"] = "No puedes eliminar este servicio porque tiene reservas asociadas. Puedes desactivarlo en su lugar.";
            }
            else
            {
                _context.Servicios.Remove(servicio);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Servicio eliminado exitosamente.";

                _logger.LogInformation("Servicio {ServicioId} eliminado por proveedor {ProveedorId}",
                    servicio.Id, servicio.ProveedorId);
            }

            return RedirectToAction("Index");
        }
    }
}