using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class Municipios
{
    public int MunicipioId { get; set; }

    public string CodigoDane { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int DepartamentoId { get; set; }

    public virtual Departamentos Departamento { get; set; } = null!;

    public virtual ICollection<PublicacionesLotePasto> PublicacionesLotePasto { get; set; } = new List<PublicacionesLotePasto>();

    public virtual ICollection<PublicacionesVaca> PublicacionesVaca { get; set; } = new List<PublicacionesVaca>();

    public virtual ICollection<Vacas> Vacas { get; set; } = new List<Vacas>();

    public virtual ICollection<VentasExternas> VentasExternas { get; set; } = new List<VentasExternas>();
}
