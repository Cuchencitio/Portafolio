using System.ComponentModel.DataAnnotations;
using TurnitoCL.Models;

namespace TurnitoCL.ViewModels
{
    // ViewModel para gestión de disponibilidad (vista principal)
    public class GestionDisponibilidadViewModel
    {
        public Proveedor Proveedor { get; set; } = new Proveedor();
        public List<DisponibilidadSemanal> DisponibilidadesActuales { get; set; } = new List<DisponibilidadSemanal>();
        public DisponibilidadViewModel NuevaDisponibilidad { get; set; } = new DisponibilidadViewModel();

        // Estadísticas
        public int TotalHorarios { get; set; }
        public TimeSpan HorasMasTempranas { get; set; }
        public TimeSpan HorasMasTardias { get; set; }

        // Lista de días para el dropdown
        public List<SelectListItem> DiasSemana { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "Lunes" },
            new SelectListItem { Value = "2", Text = "Martes" },
            new SelectListItem { Value = "3", Text = "Miércoles" },
            new SelectListItem { Value = "4", Text = "Jueves" },
            new SelectListItem { Value = "5", Text = "Viernes" },
            new SelectListItem { Value = "6", Text = "Sábado" },
            new SelectListItem { Value = "0", Text = "Domingo" }
        };

        // Propiedades calculadas
        public string HorarioMasTempranoFormateado => HorasMasTempranas.ToString(@"hh\:mm");
        public string HorarioMasTardioFormateado => HorasMasTardias.ToString(@"hh\:mm");
        public decimal HorasSemanales => (decimal)DisponibilidadesActuales.Sum(d => (d.HoraFin - d.HoraInicio).TotalHours);
    }

    // ViewModel para crear/editar disponibilidad
    public class DisponibilidadViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un día")]
        [Range(0, 6, ErrorMessage = "Día no válido")]
        [Display(Name = "Día de la semana")]
        public int DiaSemana { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        [Display(Name = "Hora de inicio")]
        public TimeSpan HoraInicio { get; set; } = new TimeSpan(9, 0, 0); // 9:00 AM por defecto

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        [Display(Name = "Hora de fin")]
        public TimeSpan HoraFin { get; set; } = new TimeSpan(18, 0, 0); // 6:00 PM por defecto

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        // Propiedades calculadas para mostrar en vista
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

        public string HorarioFormateado => $"{HoraInicio:hh\\:mm} - {HoraFin:hh\\:mm}";
        public decimal HorasDuracion => (decimal)(HoraFin - HoraInicio).TotalHours;
    }

    // ViewModel para configuración rápida
    public class ConfiguracionRapidaViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar al menos un día")]
        [Display(Name = "Días de trabajo")]
        public List<int> DiasSemana { get; set; } = new List<int> { 1, 2, 3, 4, 5 }; // Lunes a Viernes

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        [Display(Name = "Hora de inicio")]
        public TimeSpan HoraInicio { get; set; } = new TimeSpan(9, 0, 0);

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        [Display(Name = "Hora de fin")]
        public TimeSpan HoraFin { get; set; } = new TimeSpan(18, 0, 0);

        // Opciones predefinidas
        public List<OpcionHorario> OpcionesPredefinidas { get; set; } = new List<OpcionHorario>
        {
            new OpcionHorario { Nombre = "Horario Comercial", Inicio = new TimeSpan(9, 0, 0), Fin = new TimeSpan(18, 0, 0) },
            new OpcionHorario { Nombre = "Media Jornada Mañana", Inicio = new TimeSpan(8, 0, 0), Fin = new TimeSpan(13, 0, 0) },
            new OpcionHorario { Nombre = "Media Jornada Tarde", Inicio = new TimeSpan(14, 0, 0), Fin = new TimeSpan(19, 0, 0) },
            new OpcionHorario { Nombre = "Horario Extendido", Inicio = new TimeSpan(8, 0, 0), Fin = new TimeSpan(20, 0, 0) }
        };
    }

    // Clase auxiliar para opciones de horario
    public class OpcionHorario
    {
        public string Nombre { get; set; } = string.Empty;
        public TimeSpan Inicio { get; set; }
        public TimeSpan Fin { get; set; }
        public string HorarioFormateado => $"{Inicio:hh\\:mm} - {Fin:hh\\:mm}";
    }

    // Clase auxiliar para SelectListItem (si no existe)
    public class SelectListItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool Selected { get; set; } = false;
    }
}