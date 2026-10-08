using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class Estados
{
    public int EstadoId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Ambito { get; set; } = null!;

    public virtual ICollection<PublicacionesLotePasto> PublicacionesLotePasto { get; set; } = new List<PublicacionesLotePasto>();

    public virtual ICollection<PublicacionesVaca> PublicacionesVaca { get; set; } = new List<PublicacionesVaca>();

    public virtual ICollection<RegistrosSanitarios> RegistrosSanitarios { get; set; } = new List<RegistrosSanitarios>();

    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();

    public virtual ICollection<Vacas> Vacas { get; set; } = new List<Vacas>();
}
