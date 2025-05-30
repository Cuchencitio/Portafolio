using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TurnitoCL.Models
{
    public class Servicio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProveedorId { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        public decimal Precio { get; set; }

        [Required]
        public int DuracionMinutos { get; set; } = 30;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Propiedades de navegación
        [ForeignKey("ProveedorId")]
        public virtual Proveedor Proveedor { get; set; } = null!;

        public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

        // Propiedades calculadas
        [NotMapped]
        public string PrecioFormateado => $"${Precio:N0}";

        [NotMapped]
        public string DuracionFormateada => $"{DuracionMinutos} min";
    }
}