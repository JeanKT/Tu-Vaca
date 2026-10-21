using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class TiposNotificacion
{
    public int TipoNotificacionId { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
}
