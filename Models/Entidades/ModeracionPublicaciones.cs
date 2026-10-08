using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class ModeracionPublicaciones
{
    public int ModeracionId { get; set; }

    public int AdministradorId { get; set; }

    public int? PublicacionVacaId { get; set; }

    public int? PublicacionLoteId { get; set; }

    public string Accion { get; set; } = null!;

    public string? Motivo { get; set; }

    public DateTime FechaAccion { get; set; }

    public virtual Usuarios Administrador { get; set; } = null!;

    public virtual PublicacionesLotePasto? PublicacionLote { get; set; }

    public virtual PublicacionesVaca? PublicacionVaca { get; set; }
}
