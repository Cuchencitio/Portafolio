using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TurnitoCL.Data;
using TurnitoCL.Models;
using TurnitoCL.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TurnitoCL.Controllers
{
    [Authorize(Policy = "RequireConsumidor")]
    public class CitasController : Controller
    {
        private readonly TurnitoDbContext _context;
        private readonly ILogger<CitasController> _logger;

        public CitasController(TurnitoDbContext context, ILogger<CitasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Citas/Agendar
        [HttpGet]
        public async Task<IActionResult> Agendar()
        {
            var model = new AgendarCitaViewModel
            {
                Proveedores = await _context.Proveedores
                    .Include(p => p.Usuario)
                    .Include(p => p.Servicios.Where(s => s.Activo))
                    .Where(p => p.Usuario.Activo)
                    .ToListAsync(),
                Fecha = DateTime.Today.AddDays(1) // Mínimo mañana
            };

            return View(model);
        }

        // POST: /Citas/Agendar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agendar(AgendarCitaViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var consumidorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                    // Validar que la fecha sea futura
                    var fechaHoraReserva = model.Fecha.Date.Add(TimeSpan.Parse(model.Hora));
                    if (fechaHoraReserva <= DateTime.Now)
                    {
                        ModelState.AddModelError("", "La fecha y hora deben ser futuras.");
                        await CargarDatosFormulario(model);
                        return View(model);
                    }

                    // Validar disponibilidad del proveedor
                    var diaSemana = (int)fechaHoraReserva.DayOfWeek;
                    var horaReserva = fechaHoraReserva.TimeOfDay;

                    var disponibilidad = await _context.DisponibilidadSemanal
                        .Where(d => d.ProveedorId == model.ProveedorId &&
                                   d.DiaSemana == diaSemana &&
                                   d.Activo &&
                                   horaReserva >= d.HoraInicio &&
                                   horaReserva < d.HoraFin)
                        .FirstOrDefaultAsync();

                    if (disponibilidad == null)
                    {
                        ModelState.AddModelError("", "El proveedor no está disponible en el horario seleccionado.");
                        await CargarDatosFormulario(model);
                        return View(model);
                    }

                    // Verificar que no haya conflicto con otras reservas
                    var servicio = await _context.Servicios.FindAsync(model.ServicioId);
                    if (servicio == null)
                    {
                        ModelState.AddModelError("", "Servicio no encontrado.");
                        await CargarDatosFormulario(model);
                        return View(model);
                    }

                    var finReserva = fechaHoraReserva.AddMinutes(servicio.DuracionMinutos);

                    var conflicto = await _context.Reservas
                        .Where(r => r.ProveedorId == model.ProveedorId &&
                                   r.Estado != EstadosReserva.Cancelada &&
                                   ((r.FechaHora < finReserva && r.FechaHora.AddMinutes(r.Servicio.DuracionMinutos) > fechaHoraReserva)))
                        .Include(r => r.Servicio)
                        .AnyAsync();

                    if (conflicto)
                    {
                        ModelState.AddModelError("", "Ya existe una cita en ese horario. Por favor selecciona otro horario.");
                        await CargarDatosFormulario(model);
                        return View(model);
                    }

                    // Crear la reserva
                    var reserva = new Reserva
                    {
                        ConsumidorId = consumidorId,
                        ProveedorId = model.ProveedorId,
                        ServicioId = model.ServicioId,
                        FechaHora = fechaHoraReserva,
                        Estado = EstadosReserva.Pendiente,
                        Observaciones = model.Observaciones,
                        CodigoQR = GenerarCodigoQR(),
                        FechaCreacion = DateTime.Now,
                        FechaModificacion = DateTime.Now
                    };

                    _context.Reservas.Add(reserva);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Nueva cita agendada: Reserva {ReservaId} para el consumidor {ConsumidorId}",
                        reserva.Id, consumidorId);

                    TempData["SuccessMessage"] = "¡Cita agendada exitosamente! Te hemos enviado los detalles.";
                    return RedirectToAction("Detalle", new { id = reserva.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al agendar cita");
                    ModelState.AddModelError("", "Ocurrió un error al agendar la cita. Inténtalo nuevamente.");
                }
            }

            await CargarDatosFormulario(model);
            return View(model);
        }

        // GET: /Citas/MisCitas
        [HttpGet]
        public async Task<IActionResult> MisCitas()
        {
            var consumidorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var model = new MisCitasViewModel
            {
                ProximasCitas = await _context.Reservas
                    .Include(r => r.Proveedor).ThenInclude(p => p.Usuario)
                    .Include(r => r.Servicio)
                    .Where(r => r.ConsumidorId == consumidorId &&
                               r.FechaHora > DateTime.Now &&
                               r.Estado != EstadosReserva.Cancelada)
                    .OrderBy(r => r.FechaHora)
                    .ToListAsync(),

                CitasCompletadas = await _context.Reservas
                    .Include(r => r.Proveedor).ThenInclude(p => p.Usuario)
                    .Include(r => r.Servicio)
                    .Where(r => r.ConsumidorId == consumidorId &&
                               (r.Estado == EstadosReserva.Completada || r.FechaHora <= DateTime.Now))
                    .OrderByDescending(r => r.FechaHora)
                    .Take(10)
                    .ToListAsync(),

                CitasCanceladas = await _context.Reservas
                    .Include(r => r.Proveedor).ThenInclude(p => p.Usuario)
                    .Include(r => r.Servicio)
                    .Where(r => r.ConsumidorId == consumidorId && r.Estado == EstadosReserva.Cancelada)
                    .OrderByDescending(r => r.FechaHora)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        // GET: /Citas/Detalle/{id}
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var consumidorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reserva = await _context.Reservas
                .Include(r => r.Consumidor)
                .Include(r => r.Proveedor).ThenInclude(p => p.Usuario)
                .Include(r => r.Servicio)
                .Include(r => r.Asistencias)
                .FirstOrDefaultAsync(r => r.Id == id && r.ConsumidorId == consumidorId);

            if (reserva == null)
            {
                return NotFound();
            }

            var model = new DetalleReservaViewModel
            {
                Reserva = reserva,
                PuedeGenerarQR = reserva.Estado == EstadosReserva.Pendiente &&
                                reserva.FechaHora > DateTime.Now,
                QRCodeUrl = !string.IsNullOrEmpty(reserva.CodigoQR) ?
                    $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString($"{Request.Scheme}://{Request.Host}/api/qrapi/verificar/{reserva.CodigoQR}")}" :
                    string.Empty,
                HistorialAsistencias = reserva.Asistencias.ToList()
            };

            return View(model);
        }

        // POST: /Citas/Cancelar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var consumidorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.Id == id && r.ConsumidorId == consumidorId);

            if (reserva == null)
            {
                return NotFound();
            }

            if (!reserva.PuedeSerCancelada)
            {
                TempData["ErrorMessage"] = "Esta cita no puede ser cancelada.";
                return RedirectToAction("Detalle", new { id });
            }

            reserva.Estado = EstadosReserva.Cancelada;
            reserva.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cita cancelada: Reserva {ReservaId} por el consumidor {ConsumidorId}",
                reserva.Id, consumidorId);

            TempData["SuccessMessage"] = "Cita cancelada exitosamente.";
            return RedirectToAction("MisCitas");
        }

        // AJAX: GET /Citas/GetServicios?proveedorId={id}
        [HttpGet]
        [Route("Citas/GetServicios")]
        public async Task<IActionResult> GetServicios([FromQuery] int proveedorId)
        {
            // Debug logging
            _logger.LogInformation("GetServicios llamado con proveedorId: {ProveedorId}", proveedorId);

            if (proveedorId <= 0)
            {
                _logger.LogWarning("ProveedorId inválido: {ProveedorId}", proveedorId);
                return Json(new List<object>());
            }

            var servicios = await _context.Servicios
                .Where(s => s.ProveedorId == proveedorId && s.Activo)
                .Select(s => new {
                    id = s.Id,
                    nombre = s.Nombre,
                    precio = s.Precio,
                    duracion = s.DuracionMinutos,
                    descripcion = s.Descripcion
                })
                .ToListAsync();

            _logger.LogInformation("Encontrados {Count} servicios para proveedor {ProveedorId}", servicios.Count, proveedorId);

            return Json(servicios);
        }

        // AJAX: GET /Citas/GetHorasDisponibles
        [HttpGet]
        public async Task<IActionResult> GetHorasDisponibles(int proveedorId, int servicioId, DateTime fecha)
        {
            var diaSemana = (int)fecha.DayOfWeek;

            // Obtener disponibilidad del proveedor para ese día
            var disponibilidades = await _context.DisponibilidadSemanal
                .Where(d => d.ProveedorId == proveedorId && d.DiaSemana == diaSemana && d.Activo)
                .ToListAsync();

            if (!disponibilidades.Any())
            {
                return Json(new List<object>());
            }

            // Obtener duración del servicio
            var servicio = await _context.Servicios.FindAsync(servicioId);
            if (servicio == null)
            {
                return Json(new List<object>());
            }

            // Obtener reservas existentes para ese día
            var reservasExistentes = await _context.Reservas
                .Include(r => r.Servicio)
                .Where(r => r.ProveedorId == proveedorId &&
                           r.FechaHora.Date == fecha.Date &&
                           r.Estado != EstadosReserva.Cancelada)
                .ToListAsync();

            var horasDisponibles = new List<object>();

            foreach (var disponibilidad in disponibilidades)
            {
                var horaActual = disponibilidad.HoraInicio;
                var horaFin = disponibilidad.HoraFin.Subtract(TimeSpan.FromMinutes(servicio.DuracionMinutos));

                while (horaActual <= horaFin)
                {
                    var fechaHoraCompleta = fecha.Date.Add(horaActual);

                    // Verificar que sea futura
                    if (fechaHoraCompleta > DateTime.Now)
                    {
                        // Verificar conflictos
                        var finPropuesta = fechaHoraCompleta.AddMinutes(servicio.DuracionMinutos);

                        var hayConflicto = reservasExistentes.Any(r =>
                            (r.FechaHora < finPropuesta &&
                             r.FechaHora.AddMinutes(r.Servicio.DuracionMinutos) > fechaHoraCompleta));

                        if (!hayConflicto)
                        {
                            horasDisponibles.Add(new
                            {
                                valor = horaActual.ToString(@"hh\:mm"),
                                texto = horaActual.ToString(@"HH\:mm")
                            });
                        }
                    }

                    horaActual = horaActual.Add(TimeSpan.FromMinutes(30)); // Intervalos de 30 minutos
                }
            }

            return Json(horasDisponibles);
        }

        // Métodos auxiliares
        private async Task CargarDatosFormulario(AgendarCitaViewModel model)
        {
            model.Proveedores = await _context.Proveedores
                .Include(p => p.Usuario)
                .Include(p => p.Servicios.Where(s => s.Activo))
                .Where(p => p.Usuario.Activo)
                .ToListAsync();
        }

        private string GenerarCodigoQR()
        {
            return Guid.NewGuid().ToString("N")[..12].ToUpper();
        }
    }
}