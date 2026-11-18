using System.ComponentModel.DataAnnotations;
using TuVaca.Seguridad;

namespace TuVaca.Models.Cuentas
{
    /// <summary>Formulario de registro (CU-01, paso 2): nombre, documento, correo, contraseña y rol.</summary>
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "Escribe tu nombre completo.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        // RN-1-CU01: único en el sistema (se revisa en el controlador)
        [Required(ErrorMessage = "Escribe tu número de cédula.")]
        [RegularExpression(@"^[0-9]{5,15}$", ErrorMessage = "La cédula solo puede tener números (entre 5 y 15).")]
        [Display(Name = "Número de cédula")]
        public string NumeroDocumento { get; set; } = string.Empty;

        // RN-2-CU01: único en el sistema (se revisa en el controlador)
        [Required(ErrorMessage = "Escribe tu correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

        [RegularExpression(@"^\+?[0-9 ]{7,15}$", ErrorMessage = "El teléfono solo puede tener números (7 a 15).")]
        [Display(Name = "Teléfono (opcional)")]
        public string? Telefono { get; set; }

        // RN-4-CU01: solo Productor/Vendedor o Comprador/Visitante
        [Required(ErrorMessage = "Selecciona tu rol.")]
        [Display(Name = "¿Cómo vas a usar Tu Vaca?")]
        public string Rol { get; set; } = string.Empty;

        // RN-3-CU01
        [Required(ErrorMessage = "Escribe una contraseña.")]
        [RegularExpression(Contrasenas.Patron, ErrorMessage = Contrasenas.MensajeRegla)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma tu contraseña.")]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
