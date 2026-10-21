using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class RegistrosSanitarios
{
    public int RegistroId { get; set; }

    public int VacaId { get; set; }

    public int TipoRegistroId { get; set; }

    public string Nombre { get; set; } = null!;

    public DateOnly FechaAplicacion { get; set; }

    public string Responsable { get; set; } = null!;

    public string? Observaciones { get; set; }

    public DateOnly? ProximaDosis { get; set; }

    public int EstadoId { get; set; }

    public string? AmbitoEstado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual Estados? Estados { get; set; }

    public virtual TiposRegistroSanitario TipoRegistro { get; set; } = null!;

    public virtual Vacas Vaca { get; set; } = null!;
}
