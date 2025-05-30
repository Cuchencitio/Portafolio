using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TurnitoCL.Models
{
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [StringLength(200)]
        public string? NombreNegocio { get; set; }

        [StringLength(500)]
        public string? Direccion { get; set; }

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Propiedades de navegación
        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; } = null!;

        public virtual ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
        public virtual ICollection<DisponibilidadSemanal> Disponibilidades { get; set; } = new List<DisponibilidadSemanal>();
        public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}