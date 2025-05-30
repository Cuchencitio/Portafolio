using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TurnitoCL.Models
{
    public class DisponibilidadSemanal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProveedorId { get; set; }

        [Required]
        [Range(0, 6)] // 0=Domingo, 6=Sábado
        public int DiaSemana { get; set; }

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFin { get; set; }

        public bool Activo { get; set; } = true;

        // Propiedades de navegación
        [ForeignKey("ProveedorId")]
        public virtual Proveedor Proveedor { get; set; } = null!;

        // Propiedades calculadas
        [NotMapped]
        public string NombreDia => DiaSemana switch
        {
            0 => "Domingo",
            1 => "Lunes",
            2 => "Martes",
            3 => "Miércoles",
            4 => "Jueves",
            5 => "Viernes",
            6 => "Sábado",
            _ => "Desconocido"
        };

        [NotMapped]
        public string HorarioFormateado => $"{HoraInicio:hh\\:mm} - {HoraFin:hh\\:mm}";
    }
}