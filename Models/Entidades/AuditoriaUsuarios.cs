using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class AuditoriaUsuarios
{
    public int AuditoriaId { get; set; }

    public int AdministradorId { get; set; }

    public int UsuarioAfectadoId { get; set; }

    public string Accion { get; set; } = null!;

    public string? Detalle { get; set; }

    public DateTime FechaAccion { get; set; }

    public virtual Usuarios Administrador { get; set; } = null!;

    public virtual Usuarios UsuarioAfectado { get; set; } = null!;
}
