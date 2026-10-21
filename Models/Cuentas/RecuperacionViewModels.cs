using System.ComponentModel.DataAnnotations;
using TuVaca.Seguridad;

namespace TuVaca.Models.Cuentas
{
    /// <summary>CU-23 paso 2: correo al que se manda el enlace.</summary>
    public class SolicitarRecuperacionViewModel
    {
        [Required(ErrorMessage = "Escribe el correo con el que te registraste.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150)]
        [Display(Name = "Correo electrónico registrado")]
        public string Correo { get; set; } = string.Empty;
    }

    /// <summary>CU-23 paso 7: nueva contraseña y su confirmación.</summary>
    public class RestablecerContrasenaViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        // RN-2-CU23: las mismas condiciones del registro (RN-3-CU01)
        [Required(ErrorMessage = "Escribe la nueva contraseña.")]
        [RegularExpression(Contrasenas.Patron, ErrorMessage = Contrasenas.MensajeRegla)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la nueva contraseña.")]
        [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
