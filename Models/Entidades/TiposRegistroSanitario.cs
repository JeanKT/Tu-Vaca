using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class TiposRegistroSanitario
{
    public int TipoRegistroId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<RegistrosSanitarios> RegistrosSanitarios { get; set; } = new List<RegistrosSanitarios>();
}
