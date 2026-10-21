using System.ComponentModel.DataAnnotations;

namespace TuVaca.Models.Cuentas
{
    /// <summary>Formulario de inicio de sesión (CU-01).</summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Escribe tu correo o tu número de cédula.")]
        [StringLength(150)]
        [Display(Name = "Correo o cédula")]
        public string CorreoODocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe tu contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [Display(Name = "Mantener sesión activa")]
        public bool MantenerSesion { get; set; }
    }
}
