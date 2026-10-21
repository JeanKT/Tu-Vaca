using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class TiposPublicacion
{
    public int TipoPublicacionId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<PublicacionesLotePasto> PublicacionesLotePasto { get; set; } = new List<PublicacionesLotePasto>();
}
