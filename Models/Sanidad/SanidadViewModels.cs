using System.ComponentModel.DataAnnotations;

namespace TuVaca.Models.Sanidad
{
    /// <summary>CU-07 paso 3: tipo, nombre, fecha de aplicación, responsable, observaciones y próxima dosis.</summary>
    public class RegistrarSanitarioViewModel
    {
        public int VacaId { get; set; }
        public string VacaIdentificacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona el tipo.")]
        [Display(Name = "Tipo")]
        public int? TipoRegistroId { get; set; }

        // RN-2-CU07: nombre, fecha de aplicación y responsable son obligatorios
        [Required(ErrorMessage = "Escribe el nombre de la vacuna o tratamiento.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        [Display(Name = "Nombre de la vacuna / tratamiento")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe la fecha de aplicación.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de aplicación")]
        public DateTime? FechaAplicacion { get; set; }

        [Required(ErrorMessage = "Escribe quién la aplicó.")]
        [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        [Display(Name = "Responsable (veterinario o quien la aplicó)")]
        public string Responsable { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Máximo 1000 caracteres.")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        // RN-4-CU07 / FA-2: opcional; si se indica, se programa un recordatorio
        [DataType(DataType.Date)]
        [Display(Name = "Próxima dosis (opcional)")]
        public DateTime? ProximaDosis { get; set; }
    }

    /// <summary>CU-08: una fila del historial sanitario.</summary>
    public class RegistroSanitarioItem
    {
        public int RegistroId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public DateOnly FechaAplicacion { get; set; }
        public string Responsable { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public DateOnly? ProximaDosis { get; set; }
        public bool Anulado { get; set; }
    }

    public class HistorialSanitarioViewModel
    {
        public int VacaId { get; set; }
        public string VacaIdentificacion { get; set; } = string.Empty;
        public string VacaRaza { get; set; } = string.Empty;
        public bool VacaViva { get; set; }

        // FA-1-CU08: filtros
        public int? TipoRegistroId { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }

        public List<RegistroSanitarioItem> Registros { get; set; } = new();
    }
}
