using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TurnitoCL.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Telefono { get; set; }

        [Required]
        [StringLength(20)]
        public string Rol { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;

        // Propiedades de navegación
        public virtual Proveedor? Proveedor { get; set; }
        public virtual ICollection<Reserva> ReservasComoConsumidor { get; set; } = new List<Reserva>();

        // Propiedad calculada
        [NotMapped]
        public string NombreCompleto => $"{Nombre} {Apellido}";
    }

    // Constantes para roles
    public static class Roles
    {
        public const string Consumidor = "Consumidor";
        public const string Proveedor = "Proveedor";
    }
}