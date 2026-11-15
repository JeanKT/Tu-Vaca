using System.ComponentModel.DataAnnotations;

namespace TuVaca.Models.Produccion
{
    /// <summary>CU-11 paso 3: fecha y cantidad en litros.</summary>
    public class RegistrarProduccionViewModel
    {
        public int VacaId { get; set; }
        public string VacaIdentificacion { get; set; } = string.Empty;

        // RN-2 / RN-4: fecha obligatoria y no futura
        [Required(ErrorMessage = "Escribe la fecha.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha")]
        public DateTime? Fecha { get; set; }

        // RN-2 / RN-3: cantidad obligatoria y mayor a cero
        [Required(ErrorMessage = "Escribe la cantidad de leche.")]
        [Range(0.1, 200, ErrorMessage = "La cantidad debe ser mayor a 0 litros (máximo 200).")]
        [Display(Name = "Cantidad (litros)")]
        public decimal? CantidadLitros { get; set; }

        // FA-2: el usuario confirmó que quiere actualizar el registro de ese día
        public bool ConfirmarActualizacion { get; set; }
        public bool YaExiste { get; set; }
        public decimal? CantidadAnterior { get; set; }
    }

    public class ProduccionItem
    {
        public int ProduccionId { get; set; }
        public DateOnly Fecha { get; set; }
        public decimal CantidadLitros { get; set; }
    }

    /// <summary>CU-11 consulta: historial ordenado por fecha con total y promedio (RN-6).</summary>
    public class HistorialProduccionViewModel
    {
        public int VacaId { get; set; }
        public string VacaIdentificacion { get; set; } = string.Empty;
        public string VacaRaza { get; set; } = string.Empty;
        public bool VacaViva { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public List<ProduccionItem> Registros { get; set; } = new();
        public decimal Total => Registros.Sum(r => r.CantidadLitros);
        public decimal Promedio => Registros.Count == 0 ? 0 : Math.Round(Total / Registros.Count, 2);
    }

    /// <summary>Resumen del hato: producción de cada vaca en el periodo.</summary>
    public class ResumenProduccionItem
    {
        public int VacaId { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public int Dias { get; set; }
        public decimal Total { get; set; }
        public decimal Promedio => Dias == 0 ? 0 : Math.Round(Total / Dias, 2);
    }

    public class ResumenProduccionViewModel
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public List<ResumenProduccionItem> Vacas { get; set; } = new();
        public decimal Total => Vacas.Sum(v => v.Total);
    }
}
