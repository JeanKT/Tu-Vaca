using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class ReportesPublicacion
{
    public int ReporteId { get; set; }

    public int? PublicacionVacaId { get; set; }

    public int? PublicacionLoteId { get; set; }

    public int UsuarioReportaId { get; set; }

    public string? Motivo { get; set; }

    public DateTime FechaReporte { get; set; }

    public virtual PublicacionesLotePasto? PublicacionLote { get; set; }

    public virtual PublicacionesVaca? PublicacionVaca { get; set; }

    public virtual Usuarios UsuarioReporta { get; set; } = null!;
}
