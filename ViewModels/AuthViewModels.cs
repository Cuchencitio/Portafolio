using System.ComponentModel.DataAnnotations;

namespace TurnitoCL.ViewModels
{
    // ViewModel para Login
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email no válido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
    }

    // ViewModel para Registro
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email no válido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Número de teléfono no válido")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        [Display(Name = "Tipo de cuenta")]
        public string Rol { get; set; } = string.Empty;

        // Campos adicionales para proveedores
        [Display(Name = "Nombre del negocio")]
        [StringLength(200)]
        public string? NombreNegocio { get; set; }

        [Display(Name = "Dirección")]
        [StringLength(500)]
        public string? Direccion { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(1000)]
        public string? Descripcion { get; set; }
    }
}