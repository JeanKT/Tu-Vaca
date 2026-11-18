namespace TuVaca.Models.Hato
{
    /// <summary>Una vaca en el listado "Mis vacas".</summary>
    public class VacaListadoItem
    {
        public int VacaId { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public DateOnly? FechaNacimiento { get; set; }
        public decimal Peso { get; set; }
        public string? FotoUrl { get; set; }
        public int EstadoId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool TienePublicacionActiva { get; set; }
    }
}
