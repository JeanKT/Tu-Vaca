namespace TuVaca.Models.Publicaciones
{
    /// <summary>Una publicación en el listado "Mis publicaciones".</summary>
    public class PublicacionListadoItem
    {
        public int PublicacionVacaId { get; set; }
        public int VacaId { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string FotoUrl { get; set; } = string.Empty;
        public int EstadoId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? MotivoRechazo { get; set; }
        public DateTime FechaPublicacion { get; set; }
    }
}
