using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class Departamentos
{
    public int DepartamentoId { get; set; }

    public string CodigoDane { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Municipios> Municipios { get; set; } = new List<Municipios>();
}
