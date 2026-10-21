using System.ComponentModel.DataAnnotations;

namespace TuVaca.Models.Publicaciones
{
    /// <summary>
    /// Formulario del CU-03 (paso 3): los datos de la vaca vienen precargados
    /// y se completan precio, descripción, ubicación y fotografía.
    /// </summary>
    public class PublicarVacaViewModel
    {
        public int VacaId { get; set; }

        // Datos de la vaca (solo se muestran; el controlador los vuelve a leer de la base)
        public string Identificacion { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public string Edad { get; set; } = string.Empty;
        public decimal Peso { get; set; }
        public string? FotoVacaUrl { get; set; }

        // RN-2-CU03: obligatorio y mayor a cero
        [Required(ErrorMessage = "Escribe el precio.")]
        [Range(1, 9999999999, ErrorMessage = "El precio debe ser mayor a cero.")]
        [Display(Name = "Precio de venta (pesos)")]
        public decimal? Precio { get; set; }

        [Required(ErrorMessage = "Escribe una descripción.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres.")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        // Ubicación: solo se guarda el ID del municipio (catálogo DANE)
        [Required(ErrorMessage = "Selecciona el departamento.")]
        [Display(Name = "Departamento")]
        public int? DepartamentoId { get; set; }

        [Required(ErrorMessage = "Selecciona el municipio.")]
        [Display(Name = "Municipio")]
        public int? MunicipioId { get; set; }

        // RN-4-CU03: al menos una foto (la de la vaca o una nueva)
        [Display(Name = "Usar la foto que ya tiene la vaca")]
        public bool UsarFotoDeLaVaca { get; set; }

        [Display(Name = "Foto nueva")]
        public IFormFile? FotoNueva { get; set; }
    }
}
