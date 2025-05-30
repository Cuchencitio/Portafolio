using Microsoft.AspNetCore.Mvc;
using TurnitoCL.Data;
using TurnitoCL.Models;
using Microsoft.EntityFrameworkCore;

namespace TurnitoCL.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class QRApiController : ControllerBase
    {
        private readonly TurnitoDbContext _context;
        private readonly ILogger<QRApiController> _logger;

        public QRApiController(TurnitoDbContext context, ILogger<QRApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/qrapi/verificar/{codigo}
        [HttpGet("verificar/{codigo}")]
        public async Task<IActionResult> VerificarQR(string codigo)
        {
            try
            {
                var reserva = await _context.Reservas
                    .Include(r => r.Consumidor)
                    .Include(r => r.Proveedor)
                    .ThenInclude(p => p.Usuario)
                    .Include(r => r.Servicio)
                    .FirstOrDefaultAsync(r => r.CodigoQR == codigo);

                if (reserva == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        mensaje = "Código QR no válido"
                    });
                }

                // Verificar si ya fue escaneado
                var yaEscaneado = await _context.Asistencias
                    .AnyAsync(a => a.ReservaId == reserva.Id);

                if (yaEscaneado)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "Este código QR ya fue utilizado"
                    });
                }

                // Verificar fecha y hora (permitir 30 minutos antes/después)
                var ahora = DateTime.Now;
                var diferencia = Math.Abs((reserva.FechaHora - ahora).TotalMinutes);

                if (diferencia > 30)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "La cita no está en el horario permitido para check-in",
                        horaReserva = reserva.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                        horaActual = ahora.ToString("dd/MM/yyyy HH:mm")
                    });
                }

                return Ok(new
                {
                    success = true,
                    reserva = new
                    {
                        id = reserva.Id,
                        cliente = reserva.Consumidor.NombreCompleto,
                        clienteEmail = reserva.Consumidor.Email,
                        clienteTelefono = reserva.Consumidor.Telefono,
                        proveedor = reserva.Proveedor.Usuario.NombreCompleto,
                        negocio = reserva.Proveedor.NombreNegocio,
                        servicio = reserva.Servicio.Nombre,
                        fechaHora = reserva.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                        precio = reserva.Servicio.Precio,
                        duracion = reserva.Servicio.DuracionMinutos,
                        estado = reserva.Estado,
                        observaciones = reserva.Observaciones
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar código QR: {Codigo}", codigo);
                return StatusCode(500, new
                {
                    success = false,
                    mensaje = "Error interno del servidor"
                });
            }
        }

        // POST: api/qrapi/confirmar-asistencia
        [HttpPost("confirmar-asistencia")]
        public async Task<IActionResult> ConfirmarAsistencia([FromBody] ConfirmarAsistenciaRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CodigoQR))
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "Código QR es requerido"
                    });
                }

                var reserva = await _context.Reservas
                    .Include(r => r.Consumidor)
                    .Include(r => r.Servicio)
                    .FirstOrDefaultAsync(r => r.CodigoQR == request.CodigoQR);

                if (reserva == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        mensaje = "Código QR no válido"
                    });
                }

                // Verificar si ya fue confirmado
                var yaConfirmado = await _context.Asistencias
                    .AnyAsync(a => a.ReservaId == reserva.Id);

                if (yaConfirmado)
                {
                    return BadRequest(new
                    {
                        success = false,
                        mensaje = "La asistencia ya fue confirmada anteriormente"
                    });
                }

                // Registrar asistencia
                var asistencia = new Asistencia
                {
                    ReservaId = reserva.Id,
                    FechaEscaneo = DateTime.Now,
                    MetodoRegistro = "QR",
                    Observaciones = request.Observaciones
                };

                _context.Asistencias.Add(asistencia);

                // Actualizar estado de la reserva
                reserva.Estado = EstadosReserva.Completada;
                reserva.FechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Asistencia confirmada para reserva {ReservaId} del cliente {Cliente}",
                    reserva.Id, reserva.Consumidor.NombreCompleto);

                return Ok(new
                {
                    success = true,
                    mensaje = "Asistencia registrada exitosamente",
                    cliente = reserva.Consumidor.NombreCompleto,
                    servicio = reserva.Servicio.Nombre,
                    fechaConfirmacion = asistencia.FechaEscaneo.ToString("dd/MM/yyyy HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar asistencia para código: {Codigo}", request.CodigoQR);
                return StatusCode(500, new
                {
                    success = false,
                    mensaje = "Error interno del servidor"
                });
            }
        }

        // GET: api/qrapi/generar/{reservaId}
        [HttpGet("generar/{reservaId}")]
        public async Task<IActionResult> GenerarQR(int reservaId)
        {
            try
            {
                var reserva = await _context.Reservas
                    .Include(r => r.Consumidor)
                    .Include(r => r.Servicio)
                    .FirstOrDefaultAsync(r => r.Id == reservaId);

                if (reserva == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        mensaje = "Reserva no encontrada"
                    });
                }

                // Generar código QR único si no existe
                if (string.IsNullOrEmpty(reserva.CodigoQR))
                {
                    reserva.CodigoQR = Guid.NewGuid().ToString("N")[..12].ToUpper();
                    await _context.SaveChangesAsync();
                }

                // URL para verificación
                var qrContent = $"{Request.Scheme}://{Request.Host}/api/qrapi/verificar/{reserva.CodigoQR}";

                return Ok(new
                {
                    success = true,
                    reservaId = reserva.Id,
                    codigo = reserva.CodigoQR,
                    urlVerificacion = qrContent,
                    // URL para generar QR usando servicio externo
                    qrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(qrContent)}",
                    cliente = reserva.Consumidor.NombreCompleto,
                    servicio = reserva.Servicio.Nombre,
                    fechaHora = reserva.FechaHora.ToString("dd/MM/yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar código QR para reserva: {ReservaId}", reservaId);
                return StatusCode(500, new
                {
                    success = false,
                    mensaje = "Error al generar código QR"
                });
            }
        }

        // GET: api/qrapi/test
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                mensaje = "API QR funcionando correctamente",
                timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                endpoints = new[]
                {
                    "GET /api/qrapi/verificar/{codigo}",
                    "POST /api/qrapi/confirmar-asistencia",
                    "GET /api/qrapi/generar/{reservaId}",
                    "GET /api/qrapi/test"
                }
            });
        }
    }

    // Modelo para confirmar asistencia
    public class ConfirmarAsistenciaRequest
    {
        public string CodigoQR { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}