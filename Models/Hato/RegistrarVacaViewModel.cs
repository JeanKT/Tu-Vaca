using System.ComponentModel.DataAnnotations;

namespace TuVaca.Models.Hato
{
    /// <summary>
    /// Formulario del CU-02 (paso 2): identificación, raza, sexo, fecha de nacimiento
    /// (o edad aproximada), peso, origen y fotografía.
    /// Las reglas que dependen de varios campos se revisan en VacasController.
    /// </summary>
    public class RegistrarVacaViewModel
    {
        // RN-1-CU02: única en el sistema (se revisa en el controlador)
        [Required(ErrorMessage = "Escribe la identificación (arete, chip o registro).")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        [Display(Name = "Identificación (arete, chip o registro)")]
        public string Identificacion { get; set; } = string.Empty;

        // RN-2-CU02: raza, sexo, fecha de nacimiento (o edad aproximada) y peso son obligatorios
        [Required(ErrorMessage = "Escribe la raza.")]
        [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
        [Display(Name = "Raza")]
        public string Raza { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona el sexo.")]
        [RegularExpression("^(Hembra|Macho)$", ErrorMessage = "Selecciona Hembra o Macho.")]
        [Display(Name = "Sexo")]
        public string Sexo { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [Range(1, 360, ErrorMessage = "La edad debe estar entre 1 y 360 meses.")]
        [Display(Name = "o edad aproximada (meses)")]
        public int? EdadMeses { get; set; }

        [Required(ErrorMessage = "Escribe el peso.")]
        [Range(0.1, 3000, ErrorMessage = "El peso debe ser mayor a 0 kg (máximo 3000).")]
        [Display(Name = "Peso (kg)")]
        public decimal? Peso { get; set; }

        // Origen de la vaca (opcional): solo se guarda el ID del municipio (catálogo DANE)
        [Display(Name = "Departamento de origen")]
        public int? DepartamentoId { get; set; }

        [Display(Name = "Municipio de origen")]
        public int? MunicipioOrigenId { get; set; }

        // RN-4-CU02: se puede adjuntar una foto, pero no es obligatoria
        [Display(Name = "Fotografía")]
        public IFormFile? Foto { get; set; }
    }
}
