using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class VentasPlataforma
{
    public int VentaPlataformaId { get; set; }

    public int PublicacionVacaId { get; set; }

    public int VacaId { get; set; }

    public int VendedorId { get; set; }

    public int CompradorId { get; set; }

    public DateOnly FechaVenta { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Usuarios Comprador { get; set; } = null!;

    public virtual PublicacionesVaca PublicacionVaca { get; set; } = null!;

    public virtual Vacas Vaca { get; set; } = null!;

    public virtual Usuarios Vendedor { get; set; } = null!;
}
