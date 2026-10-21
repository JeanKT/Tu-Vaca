using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class ArriendosLote
{
    public int ArriendoId { get; set; }

    public int PublicacionLoteId { get; set; }

    public int VendedorId { get; set; }

    public int? CompradorId { get; set; }

    public DateOnly FechaArriendoVenta { get; set; }

    public decimal? Valor { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Usuarios? Comprador { get; set; }

    public virtual PublicacionesLotePasto PublicacionLote { get; set; } = null!;

    public virtual Usuarios Vendedor { get; set; } = null!;
}
