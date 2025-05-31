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
    public class DisponibilidadController : Controller
    {
        private readonly TurnitoDbContext _context;
        private readonly ILogger<DisponibilidadController> _logger;

        public DisponibilidadController(TurnitoDbContext context, ILogger<DisponibilidadController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Disponibilidad
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var proveedor = await _context.Proveedores
                .Include(p => p.Usuario)
                .Include(p => p.Disponibilidades.Where(d => d.Activo))
                .FirstOrDefaultAsync(p => p.UsuarioId == userId);

            if (proveedor == null)
            {
                return NotFound("Proveedor no encontrado");
            }

            var model = new GestionDisponibilidadViewModel
            {
                Proveedor = proveedor,
                DisponibilidadesActuales = proveedor.Disponibilidades
                    .OrderBy(d => d.DiaSemana)
                    .ThenBy(d => d.HoraInicio)
                    .ToList(),
                NuevaDisponibilidad = new DisponibilidadViewModel
                {
                    Activo = true
                },
                TotalHorarios = proveedor.Disponibilidades.Count,
                HorasMasTempranas = proveedor.Disponibilidades.Any() ?
                    proveedor.Disponibilidades.Min(d => d.HoraInicio) : TimeSpan.Zero,
                HorasMasTardias = proveedor.Disponibilidades.Any() ?
                    proveedor.Disponibilidades.Max(d => d.HoraFin) : TimeSpan.Zero
            };

            return View(model);
        }

        // POST: /Disponibilidad/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(GestionDisponibilidadViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.UsuarioId == userId);

            if (proveedor == null)
            {
                return NotFound("Proveedor no encontrado");
            }

            // Validar el modelo solo para la nueva disponibilidad
            if (ModelState.IsValid || ValidarNuevaDisponibilidad(model.NuevaDisponibilidad))
            {
                try
                {
                    // Verificar que no exista conflicto con horarios existentes
                    var conflicto = await _context.DisponibilidadSemanal
                        .Where(d => d.ProveedorId == proveedor.Id &&
                                   d.DiaSemana == model.NuevaDisponibilidad.DiaSemana &&
                                   d.Activo &&
                                   ((d.HoraInicio <= model.NuevaDisponibilidad.HoraInicio && d.HoraFin > model.NuevaDisponibilidad.HoraInicio) ||
                                    (d.HoraInicio < model.NuevaDisponibilidad.HoraFin && d.HoraFin >= model.NuevaDisponibilidad.HoraFin) ||
                                    (d.HoraInicio >= model.NuevaDisponibilidad.HoraInicio && d.HoraFin <= model.NuevaDisponibilidad.HoraFin)))
                        .AnyAsync();

                    if (conflicto)
                    {
                        TempData["ErrorMessage"] = "Ya existe disponibilidad para ese día en ese horario. Verifica los horarios existentes.";
                        return RedirectToAction("Index");
                    }

                    // Validar que la hora de inicio sea menor que la hora de fin
                    if (model.NuevaDisponibilidad.HoraInicio >= model.NuevaDisponibilidad.HoraFin)
                    {
                        TempData["ErrorMessage"] = "La hora de inicio debe ser menor que la hora de fin.";
                        return RedirectToAction("Index");
                    }

                    var disponibilidad = new DisponibilidadSemanal
                    {
                        ProveedorId = proveedor.Id,
                        DiaSemana = model.NuevaDisponibilidad.DiaSemana,
                        HoraInicio = model.NuevaDisponibilidad.HoraInicio,
                        HoraFin = model.NuevaDisponibilidad.HoraFin,
                        Activo = model.NuevaDisponibilidad.Activo
                    };

                    _context.DisponibilidadSemanal.Add(disponibilidad);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Nueva disponibilidad creada: Proveedor {ProveedorId}, Día {DiaSemana}, {HoraInicio}-{HoraFin}",
                        proveedor.Id, disponibilidad.DiaSemana, disponibilidad.HoraInicio, disponibilidad.HoraFin);

                    TempData["SuccessMessage"] = "Horario de disponibilidad agregado exitosamente.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear disponibilidad");
                    TempData["ErrorMessage"] = "Ocurrió un error al agregar el horario. Inténtalo nuevamente.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Por favor verifica los datos ingresados.";
            }

            return RedirectToAction("Index");
        }

        // POST: /Disponibilidad/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, TimeSpan horaInicio, TimeSpan horaFin)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var disponibilidad = await _context.DisponibilidadSemanal
                .Include(d => d.Proveedor)
                .FirstOrDefaultAsync(d => d.Id == id && d.Proveedor.UsuarioId == userId);

            if (disponibilidad == null)
            {
                return NotFound();
            }

            try
            {
                // Validar horarios
                if (horaInicio >= horaFin)
                {
                    TempData["ErrorMessage"] = "La hora de inicio debe ser menor que la hora de fin.";
                    return RedirectToAction("Index");
                }

                // Verificar conflictos con otros horarios del mismo día (excluyendo el actual)
                var conflicto = await _context.DisponibilidadSemanal
                    .Where(d => d.ProveedorId == disponibilidad.ProveedorId &&
                               d.DiaSemana == disponibilidad.DiaSemana &&
                               d.Id != id &&
                               d.Activo &&
                               ((d.HoraInicio <= horaInicio && d.HoraFin > horaInicio) ||
                                (d.HoraInicio < horaFin && d.HoraFin >= horaFin) ||
                                (d.HoraInicio >= horaInicio && d.HoraFin <= horaFin)))
                    .AnyAsync();

                if (conflicto)
                {
                    TempData["ErrorMessage"] = "El nuevo horario se superpone con otro horario existente.";
                    return RedirectToAction("Index");
                }

                disponibilidad.HoraInicio = horaInicio;
                disponibilidad.HoraFin = horaFin;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Disponibilidad actualizada: ID {DisponibilidadId}, {HoraInicio}-{HoraFin}",
                    disponibilidad.Id, disponibilidad.HoraInicio, disponibilidad.HoraFin);

                TempData["SuccessMessage"] = "Horario actualizado exitosamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar disponibilidad {DisponibilidadId}", id);
                TempData["ErrorMessage"] = "Ocurrió un error al actualizar el horario.";
            }

            return RedirectToAction("Index");
        }

        // POST: /Disponibilidad/CambiarEstado/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var disponibilidad = await _context.DisponibilidadSemanal
                .Include(d => d.Proveedor)
                .FirstOrDefaultAsync(d => d.Id == id && d.Proveedor.UsuarioId == userId);

            if (disponibilidad == null)
            {
                return NotFound();
            }

            try
            {
                // Verificar si hay citas futuras en este horario
                if (disponibilidad.Activo)
                {
                    var fechaHoy = DateTime.Today;
                    var reservasFuturas = await _context.Reservas
                        .Where(r => r.ProveedorId == disponibilidad.ProveedorId &&
                                   r.FechaHora >= fechaHoy &&
                                   r.Estado == EstadosReserva.Pendiente)
                        .Select(r => new { r.Id, r.FechaHora })
                        .ToListAsync();

                    var tieneCitasFuturas = reservasFuturas.Any(r =>
                        ((int)r.FechaHora.DayOfWeek) == disponibilidad.DiaSemana &&
                        r.FechaHora.TimeOfDay >= disponibilidad.HoraInicio &&
                        r.FechaHora.TimeOfDay < disponibilidad.HoraFin);

                    if (tieneCitasFuturas)
                    {
                        TempData["ErrorMessage"] = "No puedes desactivar este horario porque tienes citas futuras programadas en él.";
                        return RedirectToAction("Index");
                    }
                }

                disponibilidad.Activo = !disponibilidad.Activo;
                await _context.SaveChangesAsync();

                var estadoTexto = disponibilidad.Activo ? "activado" : "desactivado";
                TempData["SuccessMessage"] = $"Horario {estadoTexto} exitosamente.";

                _logger.LogInformation("Disponibilidad {Estado}: ID {DisponibilidadId}",
                    estadoTexto, disponibilidad.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de disponibilidad {DisponibilidadId}", id);
                TempData["ErrorMessage"] = "Ocurrió un error al cambiar el estado del horario.";
            }

            return RedirectToAction("Index");
        }

        // POST: /Disponibilidad/Eliminar/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var disponibilidad = await _context.DisponibilidadSemanal
                .Include(d => d.Proveedor)
                .FirstOrDefaultAsync(d => d.Id == id && d.Proveedor.UsuarioId == userId);

            if (disponibilidad == null)
            {
                return NotFound();
            }

            try
            {
                // Verificar si hay citas futuras en este horario
                var fechaHoy = DateTime.Today;
                var reservasFuturas = await _context.Reservas
                    .Where(r => r.ProveedorId == disponibilidad.ProveedorId &&
                               r.FechaHora >= fechaHoy &&
                               r.Estado != EstadosReserva.Cancelada)
                    .Select(r => new { r.Id, r.FechaHora })
                    .ToListAsync();

                var tieneCitasFuturas = reservasFuturas.Any(r =>
                    ((int)r.FechaHora.DayOfWeek) == disponibilidad.DiaSemana &&
                    r.FechaHora.TimeOfDay >= disponibilidad.HoraInicio &&
                    r.FechaHora.TimeOfDay < disponibilidad.HoraFin);

                if (tieneCitasFuturas)
                {
                    TempData["ErrorMessage"] = "No puedes eliminar este horario porque tienes citas futuras programadas en él. Puedes desactivarlo en su lugar.";
                }
                else
                {
                    _context.DisponibilidadSemanal.Remove(disponibilidad);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Horario eliminado exitosamente.";

                    _logger.LogInformation("Disponibilidad eliminada: ID {DisponibilidadId}", disponibilidad.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar disponibilidad {DisponibilidadId}", id);
                TempData["ErrorMessage"] = "Ocurrió un error al eliminar el horario.";
            }

            return RedirectToAction("Index");
        }

        // AJAX: POST /Disponibilidad/CrearRapido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearRapido([FromBody] CrearRapidoRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var proveedor = await _context.Proveedores
                    .FirstOrDefaultAsync(p => p.UsuarioId == userId);

                if (proveedor == null)
                {
                    return Json(new { success = false, mensaje = "Proveedor no encontrado" });
                }

                // Crear disponibilidad para todos los días laborales
                var diasLaborales = request.DiasSemana ?? new List<int> { 1, 2, 3, 4, 5 }; // Lunes a Viernes por defecto
                var creados = 0;

                foreach (var dia in diasLaborales)
                {
                    // Verificar si ya existe disponibilidad para ese día
                    var existeConflicto = await _context.DisponibilidadSemanal
                        .AnyAsync(d => d.ProveedorId == proveedor.Id &&
                                      d.DiaSemana == dia &&
                                      d.Activo);

                    if (!existeConflicto)
                    {
                        var disponibilidad = new DisponibilidadSemanal
                        {
                            ProveedorId = proveedor.Id,
                            DiaSemana = dia,
                            HoraInicio = request.HoraInicio,
                            HoraFin = request.HoraFin,
                            Activo = true
                        };

                        _context.DisponibilidadSemanal.Add(disponibilidad);
                        creados++;
                    }
                }

                if (creados > 0)
                {
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, mensaje = $"Se crearon {creados} horarios de disponibilidad" });
                }
                else
                {
                    return Json(new { success = false, mensaje = "Ya existes horarios para todos los días seleccionados" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CrearRapido");
                return Json(new { success = false, mensaje = "Error interno del servidor" });
            }
        }

        // Método auxiliar para validación
        private bool ValidarNuevaDisponibilidad(DisponibilidadViewModel model)
        {
            return model.DiaSemana >= 0 && model.DiaSemana <= 6 &&
                   model.HoraInicio < model.HoraFin &&
                   model.HoraInicio >= TimeSpan.Zero &&
                   model.HoraFin <= TimeSpan.FromHours(24);
        }
    }

    // Clase para el request de crear rápido
    public class CrearRapidoRequest
    {
        public List<int>? DiasSemana { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
    }
}