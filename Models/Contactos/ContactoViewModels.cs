using System.ComponentModel.DataAnnotations;

namespace TuVaca.Models.Contactos
{
    /// <summary>CU-06 paso 3: formulario de mensaje con los datos de la publicación.</summary>
    public class ContactarViewModel
    {
        public int PublicacionVacaId { get; set; }

        // RN-2-CU06: el mensaje no puede ir vacío
        [Required(ErrorMessage = "Debes escribir un mensaje antes de enviarlo.")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "El mensaje debe tener entre 5 y 1000 caracteres.")]
        [Display(Name = "Mensaje para el vendedor")]
        public string Mensaje { get; set; } = string.Empty;

        // Datos de la publicación (solo lectura)
        public string Resumen { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string VendedorNombre { get; set; } = string.Empty;
    }

    /// <summary>RN-4-CU06: el vendedor ve nombre y medio de contacto del comprador.</summary>
    public class ContactoRecibidoItem
    {
        public int ContactoId { get; set; }
        public int PublicacionVacaId { get; set; }
        public string Publicacion { get; set; } = string.Empty;
        public string CompradorNombre { get; set; } = string.Empty;
        public string CompradorCorreo { get; set; } = string.Empty;
        public string? CompradorTelefono { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public DateTime FechaContacto { get; set; }
    }
}
