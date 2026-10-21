using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class Contactos
{
    public int ContactoId { get; set; }

    public int? PublicacionVacaId { get; set; }

    public int? PublicacionLoteId { get; set; }

    public int CompradorId { get; set; }

    public string Mensaje { get; set; } = null!;

    public DateTime FechaContacto { get; set; }

    public virtual Usuarios Comprador { get; set; } = null!;

    public virtual PublicacionesLotePasto? PublicacionLote { get; set; }

    public virtual PublicacionesVaca? PublicacionVaca { get; set; }
}
