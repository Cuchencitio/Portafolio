using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TurnitoCL.Models
{
    public class Asistencia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReservaId { get; set; }

        public DateTime FechaEscaneo { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string MetodoRegistro { get; set; } = "QR";

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Propiedades de navegación
        [ForeignKey("ReservaId")]
        public virtual Reserva Reserva { get; set; } = null!;

        // Propiedad calculada
        [NotMapped]
        public string FechaEscaneoFormateada => FechaEscaneo.ToString("dd/MM/yyyy HH:mm:ss");
    }
}