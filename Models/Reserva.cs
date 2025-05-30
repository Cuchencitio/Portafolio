using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TurnitoCL.Models
{
    public class Reserva
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConsumidorId { get; set; }

        [Required]
        public int ProveedorId { get; set; }

        [Required]
        public int ServicioId { get; set; }

        [Required]
        public DateTime FechaHora { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        [StringLength(500)]
        public string? CodigoQR { get; set; }

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime FechaModificacion { get; set; } = DateTime.Now;

        // Propiedades de navegación
        [ForeignKey("ConsumidorId")]
        public virtual Usuario Consumidor { get; set; } = null!;

        [ForeignKey("ProveedorId")]
        public virtual Proveedor Proveedor { get; set; } = null!;

        [ForeignKey("ServicioId")]
        public virtual Servicio Servicio { get; set; } = null!;

        public virtual ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();

        // Propiedades calculadas
        [NotMapped]
        public string FechaFormateada => FechaHora.ToString("dd/MM/yyyy HH:mm");

        [NotMapped]
        public bool PuedeSerCancelada => Estado == "Pendiente" && FechaHora > DateTime.Now.AddHours(2);

        [NotMapped]
        public string EstadoColor => Estado switch
        {
            "Pendiente" => "warning",
            "Confirmada" => "info",
            "Completada" => "success",
            "Cancelada" => "danger",
            "NoShow" => "secondary",
            _ => "light"
        };
    }

    // Constantes para estados
    public static class EstadosReserva
    {
        public const string Pendiente = "Pendiente";
        public const string Confirmada = "Confirmada";
        public const string Cancelada = "Cancelada";
        public const string Completada = "Completada";
        public const string NoShow = "NoShow";
    }
}