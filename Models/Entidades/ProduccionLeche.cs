using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class ProduccionLeche
{
    public int ProduccionId { get; set; }

    public int VacaId { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal CantidadLitros { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual Vacas Vaca { get; set; } = null!;
}
