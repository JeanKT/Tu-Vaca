using System.ComponentModel.DataAnnotations;

namespace TuVaca.Models.Publicaciones
{
    /// <summary>
    /// CU-16 paso 3: precio, descripción, ubicación y fotografía se pueden editar;
    /// la vaca asociada no se cambia (RN-2).
    /// </summary>
    public class EditarPublicacionViewModel
    {
        public int PublicacionVacaId { get; set; }
        public string VacaResumen { get; set; } = string.Empty;
        public string FotoActualUrl { get; set; } = string.Empty;

        // RN-3-CU16: sigue siendo mayor a cero
        [Required(ErrorMessage = "Escribe el precio.")]
        [Range(1, 9999999999, ErrorMessage = "El precio debe ser mayor a cero.")]
        [Display(Name = "Precio de venta (pesos)")]
        public decimal? Precio { get; set; }

        [Required(ErrorMessage = "Escribe una descripción.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres.")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona el departamento.")]
        public int? DepartamentoId { get; set; }

        [Required(ErrorMessage = "Selecciona el municipio.")]
        public int? MunicipioId { get; set; }

        [Display(Name = "Cambiar fotografía")]
        public IFormFile? FotoNueva { get; set; }
    }
}
