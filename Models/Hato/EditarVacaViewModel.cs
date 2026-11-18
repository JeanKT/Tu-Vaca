using System.ComponentModel.DataAnnotations;

namespace TuVaca.Models.Hato
{
    /// <summary>
    /// CU-12 paso 3: formulario precargado. La identificación NO se edita (RN-2),
    /// solo se muestra. Raza, sexo, fecha de nacimiento, peso y foto sí (RN-3).
    /// </summary>
    public class EditarVacaViewModel
    {
        public int VacaId { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string? FotoActualUrl { get; set; }

        [Required(ErrorMessage = "Escribe la raza.")]
        [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
        public string Raza { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona el sexo.")]
        [RegularExpression("^(Hembra|Macho)$", ErrorMessage = "Selecciona Hembra o Macho.")]
        public string Sexo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe la fecha de nacimiento.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [Required(ErrorMessage = "Escribe el peso.")]
        [Range(0.1, 3000, ErrorMessage = "El peso debe ser mayor a 0 kg (máximo 3000).")]
        [Display(Name = "Peso (kg)")]
        public decimal? Peso { get; set; }

        public int? DepartamentoId { get; set; }
        public int? MunicipioOrigenId { get; set; }

        [Display(Name = "Cambiar fotografía")]
        public IFormFile? Foto { get; set; }
    }
}
