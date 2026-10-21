using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class PublicacionesVaca
{
    public int PublicacionVacaId { get; set; }

    public int VacaId { get; set; }

    public decimal Precio { get; set; }

    public string Descripcion { get; set; } = null!;

    public int MunicipioId { get; set; }

    public string FotoUrl { get; set; } = null!;

    public int EstadoId { get; set; }

    public string? AmbitoEstado { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime FechaPublicacion { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual ICollection<Contactos> Contactos { get; set; } = new List<Contactos>();

    public virtual Estados? Estados { get; set; }

    public virtual ICollection<ModeracionPublicaciones> ModeracionPublicaciones { get; set; } = new List<ModeracionPublicaciones>();

    public virtual Municipios Municipio { get; set; } = null!;

    public virtual ICollection<ReportesPublicacion> ReportesPublicacion { get; set; } = new List<ReportesPublicacion>();

    public virtual Vacas Vaca { get; set; } = null!;

    public virtual VentasPlataforma? VentasPlataforma { get; set; }
}
