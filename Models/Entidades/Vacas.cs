using System;
using System.Collections.Generic;

namespace TuVaca.Models.Entidades;

public partial class Vacas
{
    public int VacaId { get; set; }

    public int UsuarioId { get; set; }

    public string Identificacion { get; set; } = null!;

    public string Raza { get; set; } = null!;

    public string Sexo { get; set; } = null!;

    public DateOnly? FechaNacimiento { get; set; }

    public decimal Peso { get; set; }

    public string? FotoUrl { get; set; }

    public int? MunicipioOrigenId { get; set; }

    public int EstadoId { get; set; }

    public string? AmbitoEstado { get; set; }

    public DateOnly? FechaMuerte { get; set; }

    public int? CausaMuerteId { get; set; }

    public string? ComentarioMuerte { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual CausasMuerte? CausaMuerte { get; set; }

    public virtual Estados? Estados { get; set; }

    public virtual Municipios? MunicipioOrigen { get; set; }

    public virtual ICollection<ProduccionLeche> ProduccionLeche { get; set; } = new List<ProduccionLeche>();

    public virtual PublicacionesVaca? PublicacionesVaca { get; set; }

    public virtual ICollection<RegistrosSanitarios> RegistrosSanitarios { get; set; } = new List<RegistrosSanitarios>();

    public virtual Usuarios Usuario { get; set; } = null!;

    public virtual VentasExternas? VentasExternas { get; set; }

    public virtual ICollection<VentasPlataforma> VentasPlataforma { get; set; } = new List<VentasPlataforma>();
}
