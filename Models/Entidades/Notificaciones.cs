using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class Notificaciones
{
    public int NotificacionId { get; set; }

    public int UsuarioId { get; set; }

    public int TipoNotificacionId { get; set; }

    public string Mensaje { get; set; } = null!;

    public string? Enlace { get; set; }

    public DateTime FechaProgramada { get; set; }

    public int EstadoId { get; set; }

    public string? AmbitoEstado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Estados? Estados { get; set; }

    public virtual TiposNotificacion TipoNotificacion { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
