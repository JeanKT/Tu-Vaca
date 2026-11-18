using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class TokensRecuperacion
{
    public int TokenId { get; set; }

    public int UsuarioId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime FechaExpiracion { get; set; }

    public bool Usado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}
