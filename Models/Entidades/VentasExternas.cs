using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class VentasExternas
{
    public int VentaExternaId { get; set; }

    public int VacaId { get; set; }

    public DateOnly FechaVenta { get; set; }

    public string? CompradorNombre { get; set; }

    public decimal? ValorVenta { get; set; }

    public int? MunicipioVentaId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Municipios? MunicipioVenta { get; set; }

    public virtual Vacas Vaca { get; set; } = null!;
}
