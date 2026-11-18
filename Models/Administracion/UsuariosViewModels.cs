using System.ComponentModel.DataAnnotations;

namespace TuVaca.Models.Administracion
{
    /// <summary>CU-10 paso 2: nombre, correo, rol y estado de cada usuario.</summary>
    public class UsuarioListadoItem
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public int RolId { get; set; }
        public string Rol { get; set; } = string.Empty;
        public int EstadoId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }

    public class UsuariosIndexViewModel
    {
        // FA-2-CU10: filtros por nombre, rol o estado
        public string? Buscar { get; set; }
        public int? RolId { get; set; }
        public int? EstadoId { get; set; }
        public List<UsuarioListadoItem> Usuarios { get; set; } = new();
    }

    /// <summary>Acción "Editar datos" (el correo y el documento no se editan aquí).</summary>
    public class EditarUsuarioViewModel
    {
        public int UsuarioId { get; set; }
        public string Correo { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe el nombre.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
        [RegularExpression(@"^[0-9+\s-]*$", ErrorMessage = "El teléfono solo puede tener números, espacios, + y -.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [StringLength(200, ErrorMessage = "Máximo 200 caracteres.")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }
    }

    public class AuditoriaItem
    {
        public DateTime Fecha { get; set; }
        public string Administrador { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
    }
}
