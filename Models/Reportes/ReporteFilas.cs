namespace TuVaca.Models.Reportes
{
    /// <summary>Una fila de dbo.sp_ReporteProduccion (los nombres deben ser iguales a las columnas del SP).</summary>
    public class ReporteProduccionFila
    {
        public int VacaID { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string Raza { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int DiasRegistrados { get; set; }
        public decimal TotalLitros { get; set; }
        public decimal PromedioLitros { get; set; }
        public decimal MinimoLitros { get; set; }
        public decimal MaximoLitros { get; set; }
    }

    /// <summary>Una fila de dbo.sp_HistorialSanitario.</summary>
    public class HistorialSanitarioFila
    {
        public int RegistroID { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public DateOnly FechaAplicacion { get; set; }
        public string Responsable { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public DateOnly? ProximaDosis { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string EstadoProximaDosis { get; set; } = string.Empty;
    }

    public class UsuarioReporteFila
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }
}
