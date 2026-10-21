using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class CausasMuerte
{
    public int CausaMuerteId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Vacas> Vacas { get; set; } = new List<Vacas>();
}
