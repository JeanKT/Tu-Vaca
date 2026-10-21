using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class Usuarios
{
    public int UsuarioId { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string NumeroDocumento { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string ContrasenaHash { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public string? FotoPerfilUrl { get; set; }

    public int RolId { get; set; }

    public int EstadoId { get; set; }

    public string? AmbitoEstado { get; set; }

    public int IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual ICollection<ArriendosLote> ArriendosLoteComprador { get; set; } = new List<ArriendosLote>();

    public virtual ICollection<ArriendosLote> ArriendosLoteVendedor { get; set; } = new List<ArriendosLote>();

    public virtual ICollection<AuditoriaUsuarios> AuditoriaUsuariosAdministrador { get; set; } = new List<AuditoriaUsuarios>();

    public virtual ICollection<AuditoriaUsuarios> AuditoriaUsuariosUsuarioAfectado { get; set; } = new List<AuditoriaUsuarios>();

    public virtual ICollection<Contactos> Contactos { get; set; } = new List<Contactos>();

    public virtual Estados? Estados { get; set; }

    public virtual ICollection<ModeracionPublicaciones> ModeracionPublicaciones { get; set; } = new List<ModeracionPublicaciones>();

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();

    public virtual ICollection<PublicacionesLotePasto> PublicacionesLotePasto { get; set; } = new List<PublicacionesLotePasto>();

    public virtual ICollection<ReportesPublicacion> ReportesPublicacion { get; set; } = new List<ReportesPublicacion>();

    public virtual Roles Rol { get; set; } = null!;

    public virtual ICollection<Sesiones> Sesiones { get; set; } = new List<Sesiones>();

    public virtual ICollection<TokensRecuperacion> TokensRecuperacion { get; set; } = new List<TokensRecuperacion>();

    public virtual ICollection<Vacas> Vacas { get; set; } = new List<Vacas>();

    public virtual ICollection<VentasPlataforma> VentasPlataformaComprador { get; set; } = new List<VentasPlataforma>();

    public virtual ICollection<VentasPlataforma> VentasPlataformaVendedor { get; set; } = new List<VentasPlataforma>();
}
