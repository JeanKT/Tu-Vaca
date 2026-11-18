using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class Sesiones
{
    public int SesionId { get; set; }

    public int UsuarioId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaExpiracion { get; set; }

    public bool Activa { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}
