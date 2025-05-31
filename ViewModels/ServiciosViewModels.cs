using System.ComponentModel.DataAnnotations;
using TurnitoCL.Models;

namespace TurnitoCL.ViewModels
{
    // ViewModel para gestión de servicios (lista principal)
    public class GestionServiciosViewModel
    {
        public Proveedor Proveedor { get; set; } = new Proveedor();
        public List<Servicio> Servicios { get; set; } = new List<Servicio>();
        public int ServiciosActivos { get; set; }
        public int ServiciosInactivos { get; set; }
        public int TotalServicios { get; set; }
    }

    // ViewModel para crear/editar servicios
    public class ServicioViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        [Display(Name = "Nombre del servicio")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(100, 999999, ErrorMessage = "El precio debe estar entre $100 y $999,999")]
        [Display(Name = "Precio ($)")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "La duración es obligatoria")]
        [Range(15, 480, ErrorMessage = "La duración debe estar entre 15 y 480 minutos")]
        [Display(Name = "Duración (minutos)")]
        public int DuracionMinutos { get; set; } = 30;

        [Display(Name = "Servicio activo")]
        public bool Activo { get; set; } = true;

        // Propiedades calculadas para mostrar en vista
        public string PrecioFormateado => $"${Precio:N0}";
        public string DuracionFormateada => $"{DuracionMinutos} min";
    }

    // ViewModel para detalle de servicio
    public class DetalleServicioViewModel
    {
        public Servicio Servicio { get; set; } = new Servicio();
        public int TotalReservas { get; set; }
        public int ReservasEsteMes { get; set; }
        public decimal IngresosTotales { get; set; }
        public List<Reserva> UltimasReservas { get; set; } = new List<Reserva>();

        // Propiedades calculadas
        public string IngresosTotalesFormateados => $"${IngresosTotales:N0}";
        public decimal PromedioReservasPorMes => TotalReservas > 0 ? (decimal)TotalReservas / 12 : 0;
    }
}