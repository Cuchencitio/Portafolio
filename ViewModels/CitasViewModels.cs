using System.ComponentModel.DataAnnotations;
using TurnitoCL.Models;

namespace TurnitoCL.ViewModels
{
    // ViewModel para agendar cita
    public class AgendarCitaViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un proveedor")]
        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un servicio")]
        [Display(Name = "Servicio")]
        public int ServicioId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una fecha")]
        [Display(Name = "Fecha")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una hora")]
        [Display(Name = "Hora")]
        public string Hora { get; set; } = string.Empty;

        [Display(Name = "Observaciones")]
        [StringLength(1000)]
        public string? Observaciones { get; set; }

        // Datos para cargar en la vista
        public List<Proveedor> Proveedores { get; set; } = new List<Proveedor>();
        public List<Servicio> Servicios { get; set; } = new List<Servicio>();
        public List<string> HorasDisponibles { get; set; } = new List<string>();
    }

    // ViewModel para mis citas
    public class MisCitasViewModel
    {
        public List<Reserva> ProximasCitas { get; set; } = new List<Reserva>();
        public List<Reserva> CitasCompletadas { get; set; } = new List<Reserva>();
        public List<Reserva> CitasCanceladas { get; set; } = new List<Reserva>();

        public int TotalProximas => ProximasCitas.Count;
        public int TotalCompletadas => CitasCompletadas.Count;
        public int TotalCanceladas => CitasCanceladas.Count;
    }

    // ViewModel para detalle de reserva
    public class DetalleReservaViewModel
    {
        public Reserva Reserva { get; set; } = new Reserva();
        public string QRCodeUrl { get; set; } = string.Empty;
        public bool PuedeGenerarQR { get; set; }
        public List<Asistencia> HistorialAsistencias { get; set; } = new List<Asistencia>();
    }
}