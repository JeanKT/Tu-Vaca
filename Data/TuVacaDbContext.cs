using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TuVaca.Models.Entidades;

namespace TuVaca.Data;

public partial class TuVacaDbContext : DbContext
{
    public TuVacaDbContext(DbContextOptions<TuVacaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ArriendosLote> ArriendosLote { get; set; }

    public virtual DbSet<AuditoriaUsuarios> AuditoriaUsuarios { get; set; }

    public virtual DbSet<CausasMuerte> CausasMuerte { get; set; }

    public virtual DbSet<Contactos> Contactos { get; set; }

    public virtual DbSet<Departamentos> Departamentos { get; set; }

    public virtual DbSet<Estados> Estados { get; set; }

    public virtual DbSet<ModeracionPublicaciones> ModeracionPublicaciones { get; set; }

    public virtual DbSet<Municipios> Municipios { get; set; }

    public virtual DbSet<ProduccionLeche> ProduccionLeche { get; set; }

    public virtual DbSet<PublicacionesLotePasto> PublicacionesLotePasto { get; set; }

    public virtual DbSet<PublicacionesVaca> PublicacionesVaca { get; set; }

    public virtual DbSet<RegistrosSanitarios> RegistrosSanitarios { get; set; }

    public virtual DbSet<ReportesPublicacion> ReportesPublicacion { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<Sesiones> Sesiones { get; set; }

    public virtual DbSet<TiposPublicacion> TiposPublicacion { get; set; }

    public virtual DbSet<TiposRegistroSanitario> TiposRegistroSanitario { get; set; }

    public virtual DbSet<TokensRecuperacion> TokensRecuperacion { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    public virtual DbSet<Vacas> Vacas { get; set; }

    public virtual DbSet<VentasExternas> VentasExternas { get; set; }

    public virtual DbSet<VentasPlataforma> VentasPlataforma { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArriendosLote>(entity =>
        {
            entity.HasKey(e => e.ArriendoId).HasName("PK__Arriendo__12D411D6E265CC41");

            entity.HasIndex(e => e.PublicacionLoteId, "UQ__Arriendo__EFBB7ADD968B5BDC").IsUnique();

            entity.Property(e => e.ArriendoId).HasColumnName("ArriendoID");
            entity.Property(e => e.CompradorId).HasColumnName("CompradorID");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PublicacionLoteId).HasColumnName("PublicacionLoteID");
            entity.Property(e => e.Valor).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.VendedorId).HasColumnName("VendedorID");

            entity.HasOne(d => d.Comprador).WithMany(p => p.ArriendosLoteComprador)
                .HasForeignKey(d => d.CompradorId)
                .HasConstraintName("FK_ArriendosLote_Comprador");

            entity.HasOne(d => d.PublicacionLote).WithOne(p => p.ArriendosLote)
                .HasForeignKey<ArriendosLote>(d => d.PublicacionLoteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArriendosLote_Publicacion");

            entity.HasOne(d => d.Vendedor).WithMany(p => p.ArriendosLoteVendedor)
                .HasForeignKey(d => d.VendedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArriendosLote_Vendedor");
        });

        modelBuilder.Entity<AuditoriaUsuarios>(entity =>
        {
            entity.HasKey(e => e.AuditoriaId).HasName("PK__Auditori__095694E37E61C263");

            entity.Property(e => e.AuditoriaId).HasColumnName("AuditoriaID");
            entity.Property(e => e.Accion).HasMaxLength(30);
            entity.Property(e => e.AdministradorId).HasColumnName("AdministradorID");
            entity.Property(e => e.Detalle).HasMaxLength(300);
            entity.Property(e => e.FechaAccion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UsuarioAfectadoId).HasColumnName("UsuarioAfectadoID");

            entity.HasOne(d => d.Administrador).WithMany(p => p.AuditoriaUsuariosAdministrador)
                .HasForeignKey(d => d.AdministradorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditoriaUsuarios_Admin");

            entity.HasOne(d => d.UsuarioAfectado).WithMany(p => p.AuditoriaUsuariosUsuarioAfectado)
                .HasForeignKey(d => d.UsuarioAfectadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditoriaUsuarios_Afectado");
        });

        modelBuilder.Entity<CausasMuerte>(entity =>
        {
            entity.HasKey(e => e.CausaMuerteId).HasName("PK__CausasMu__9522D7D23DB5E9E0");

            entity.HasIndex(e => e.Nombre, "UQ__CausasMu__75E3EFCF1E2B0963").IsUnique();

            entity.Property(e => e.CausaMuerteId).HasColumnName("CausaMuerteID");
            entity.Property(e => e.Nombre).HasMaxLength(60);
        });

        modelBuilder.Entity<Contactos>(entity =>
        {
            entity.HasKey(e => e.ContactoId).HasName("PK__Contacto__8E0F85C8815055C6");

            entity.HasIndex(e => e.CompradorId, "IX_Contactos_CompradorID");

            entity.Property(e => e.ContactoId).HasColumnName("ContactoID");
            entity.Property(e => e.CompradorId).HasColumnName("CompradorID");
            entity.Property(e => e.FechaContacto).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PublicacionLoteId).HasColumnName("PublicacionLoteID");
            entity.Property(e => e.PublicacionVacaId).HasColumnName("PublicacionVacaID");

            entity.HasOne(d => d.Comprador).WithMany(p => p.Contactos)
                .HasForeignKey(d => d.CompradorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contactos_Comprador");

            entity.HasOne(d => d.PublicacionLote).WithMany(p => p.Contactos)
                .HasForeignKey(d => d.PublicacionLoteId)
                .HasConstraintName("FK_Contactos_PublicacionLote");

            entity.HasOne(d => d.PublicacionVaca).WithMany(p => p.Contactos)
                .HasForeignKey(d => d.PublicacionVacaId)
                .HasConstraintName("FK_Contactos_PublicacionVaca");
        });

        modelBuilder.Entity<Departamentos>(entity =>
        {
            entity.HasKey(e => e.DepartamentoId).HasName("PK__Departam__66BB0E1EE03A993C");

            entity.HasIndex(e => e.Nombre, "UQ__Departam__75E3EFCF430DC8CD").IsUnique();

            entity.HasIndex(e => e.CodigoDane, "UQ__Departam__AFCA081ACBBC2189").IsUnique();

            entity.Property(e => e.DepartamentoId).HasColumnName("DepartamentoID");
            entity.Property(e => e.CodigoDane)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Nombre).HasMaxLength(80);
        });

        modelBuilder.Entity<Estados>(entity =>
        {
            entity.HasKey(e => e.EstadoId).HasName("PK__Estados__FEF86B6003188EBD");

            entity.HasIndex(e => new { e.EstadoId, e.Ambito }, "UQ_Estados_EstadoAmbito").IsUnique();

            entity.HasIndex(e => new { e.Nombre, e.Ambito }, "UQ_Estados_NombreAmbito").IsUnique();

            entity.Property(e => e.EstadoId)
                .ValueGeneratedNever()
                .HasColumnName("EstadoID");
            entity.Property(e => e.Ambito)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre).HasMaxLength(30);
        });

        modelBuilder.Entity<ModeracionPublicaciones>(entity =>
        {
            entity.HasKey(e => e.ModeracionId).HasName("PK__Moderaci__FCB0D9FB36AF4874");

            entity.Property(e => e.ModeracionId).HasColumnName("ModeracionID");
            entity.Property(e => e.Accion).HasMaxLength(15);
            entity.Property(e => e.AdministradorId).HasColumnName("AdministradorID");
            entity.Property(e => e.FechaAccion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Motivo).HasMaxLength(300);
            entity.Property(e => e.PublicacionLoteId).HasColumnName("PublicacionLoteID");
            entity.Property(e => e.PublicacionVacaId).HasColumnName("PublicacionVacaID");

            entity.HasOne(d => d.Administrador).WithMany(p => p.ModeracionPublicaciones)
                .HasForeignKey(d => d.AdministradorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Moderacion_Admin");

            entity.HasOne(d => d.PublicacionLote).WithMany(p => p.ModeracionPublicaciones)
                .HasForeignKey(d => d.PublicacionLoteId)
                .HasConstraintName("FK_Moderacion_PublicacionLote");

            entity.HasOne(d => d.PublicacionVaca).WithMany(p => p.ModeracionPublicaciones)
                .HasForeignKey(d => d.PublicacionVacaId)
                .HasConstraintName("FK_Moderacion_PublicacionVaca");
        });

        modelBuilder.Entity<Municipios>(entity =>
        {
            entity.HasKey(e => e.MunicipioId).HasName("PK__Municipi__1EEFE56E9894028F");

            entity.HasIndex(e => e.DepartamentoId, "IX_Municipios_DepartamentoID");

            entity.HasIndex(e => new { e.Nombre, e.DepartamentoId }, "UQ_Municipios_NombreDepartamento").IsUnique();

            entity.HasIndex(e => e.CodigoDane, "UQ__Municipi__AFCA081A7B23E96C").IsUnique();

            entity.Property(e => e.MunicipioId).HasColumnName("MunicipioID");
            entity.Property(e => e.CodigoDane)
                .HasMaxLength(5)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.DepartamentoId).HasColumnName("DepartamentoID");
            entity.Property(e => e.Nombre).HasMaxLength(80);

            entity.HasOne(d => d.Departamento).WithMany(p => p.Municipios)
                .HasForeignKey(d => d.DepartamentoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Municipios_Departamentos");
        });

        modelBuilder.Entity<ProduccionLeche>(entity =>
        {
            entity.HasKey(e => e.ProduccionId).HasName("PK__Producci__6632E91D08BC7A63");

            entity.HasIndex(e => e.VacaId, "IX_ProduccionLeche_VacaID");

            entity.HasIndex(e => new { e.VacaId, e.Fecha }, "UQ_ProduccionLeche_VacaFecha").IsUnique();

            entity.Property(e => e.ProduccionId).HasColumnName("ProduccionID");
            entity.Property(e => e.CantidadLitros).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.VacaId).HasColumnName("VacaID");

            entity.HasOne(d => d.Vaca).WithMany(p => p.ProduccionLeche)
                .HasForeignKey(d => d.VacaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduccionLeche_Vacas");
        });

        modelBuilder.Entity<PublicacionesLotePasto>(entity =>
        {
            entity.HasKey(e => e.PublicacionLoteId).HasName("PK__Publicac__EFBB7ADC62C0ECC5");

            entity.HasIndex(e => e.UsuarioId, "IX_PublicacionesLote_UsuarioID");

            entity.Property(e => e.PublicacionLoteId).HasColumnName("PublicacionLoteID");
            entity.Property(e => e.AmbitoEstado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](20),'PUBLICACION'))", true);
            entity.Property(e => e.Area).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.EstadoId)
                .HasDefaultValue(20)
                .HasColumnName("EstadoID");
            entity.Property(e => e.FechaPublicacion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FotoUrl).HasMaxLength(300);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(300);
            entity.Property(e => e.MunicipioId).HasColumnName("MunicipioID");
            entity.Property(e => e.Precio).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TipoPasto).HasMaxLength(80);
            entity.Property(e => e.TipoPublicacionId).HasColumnName("TipoPublicacionID");
            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioID");

            entity.HasOne(d => d.Municipio).WithMany(p => p.PublicacionesLotePasto)
                .HasForeignKey(d => d.MunicipioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PublicacionesLote_Municipios");

            entity.HasOne(d => d.TipoPublicacion).WithMany(p => p.PublicacionesLotePasto)
                .HasForeignKey(d => d.TipoPublicacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PublicacionesLote_TiposPublicacion");

            entity.HasOne(d => d.Usuario).WithMany(p => p.PublicacionesLotePasto)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PublicacionesLote_Usuarios");

            entity.HasOne(d => d.Estados).WithMany(p => p.PublicacionesLotePasto)
                .HasPrincipalKey(p => new { p.EstadoId, p.Ambito })
                .HasForeignKey(d => new { d.EstadoId, d.AmbitoEstado })
                .HasConstraintName("FK_PublicacionesLote_Estados");
        });

        modelBuilder.Entity<PublicacionesVaca>(entity =>
        {
            entity.HasKey(e => e.PublicacionVacaId).HasName("PK__Publicac__C7DFB69E791DED9D");

            entity.HasIndex(e => e.VacaId, "IX_PublicacionesVaca_VacaID");

            entity.HasIndex(e => e.VacaId, "UX_PublicacionesVaca_UnaActivaPorVaca")
                .IsUnique()
                .HasFilter("([EstadoID]=(20))");

            entity.Property(e => e.PublicacionVacaId).HasColumnName("PublicacionVacaID");
            entity.Property(e => e.AmbitoEstado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](20),'PUBLICACION'))", true);
            entity.Property(e => e.EstadoId)
                .HasDefaultValue(20)
                .HasColumnName("EstadoID");
            entity.Property(e => e.FechaPublicacion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FotoUrl).HasMaxLength(300);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(300);
            entity.Property(e => e.MunicipioId).HasColumnName("MunicipioID");
            entity.Property(e => e.Precio).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.VacaId).HasColumnName("VacaID");

            entity.HasOne(d => d.Municipio).WithMany(p => p.PublicacionesVaca)
                .HasForeignKey(d => d.MunicipioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PublicacionesVaca_Municipios");

            entity.HasOne(d => d.Vaca).WithOne(p => p.PublicacionesVaca)
                .HasForeignKey<PublicacionesVaca>(d => d.VacaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PublicacionesVaca_Vacas");

            entity.HasOne(d => d.Estados).WithMany(p => p.PublicacionesVaca)
                .HasPrincipalKey(p => new { p.EstadoId, p.Ambito })
                .HasForeignKey(d => new { d.EstadoId, d.AmbitoEstado })
                .HasConstraintName("FK_PublicacionesVaca_Estados");
        });

        modelBuilder.Entity<RegistrosSanitarios>(entity =>
        {
            entity.HasKey(e => e.RegistroId).HasName("PK__Registro__B897313E652C7284");

            entity.HasIndex(e => e.VacaId, "IX_RegistrosSanitarios_VacaID");

            entity.Property(e => e.RegistroId).HasColumnName("RegistroID");
            entity.Property(e => e.AmbitoEstado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](20),'REGISTRO_SANITARIO'))", true);
            entity.Property(e => e.EstadoId)
                .HasDefaultValue(30)
                .HasColumnName("EstadoID");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.Responsable).HasMaxLength(150);
            entity.Property(e => e.TipoRegistroId).HasColumnName("TipoRegistroID");
            entity.Property(e => e.VacaId).HasColumnName("VacaID");

            entity.HasOne(d => d.TipoRegistro).WithMany(p => p.RegistrosSanitarios)
                .HasForeignKey(d => d.TipoRegistroId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RegistrosSanitarios_Tipos");

            entity.HasOne(d => d.Vaca).WithMany(p => p.RegistrosSanitarios)
                .HasForeignKey(d => d.VacaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RegistrosSanitarios_Vacas");

            entity.HasOne(d => d.Estados).WithMany(p => p.RegistrosSanitarios)
                .HasPrincipalKey(p => new { p.EstadoId, p.Ambito })
                .HasForeignKey(d => new { d.EstadoId, d.AmbitoEstado })
                .HasConstraintName("FK_RegistrosSanitarios_Estados");
        });

        modelBuilder.Entity<ReportesPublicacion>(entity =>
        {
            entity.HasKey(e => e.ReporteId).HasName("PK__Reportes__0B29EA4ED5A1FF18");

            entity.Property(e => e.ReporteId).HasColumnName("ReporteID");
            entity.Property(e => e.FechaReporte).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Motivo).HasMaxLength(300);
            entity.Property(e => e.PublicacionLoteId).HasColumnName("PublicacionLoteID");
            entity.Property(e => e.PublicacionVacaId).HasColumnName("PublicacionVacaID");
            entity.Property(e => e.UsuarioReportaId).HasColumnName("UsuarioReportaID");

            entity.HasOne(d => d.PublicacionLote).WithMany(p => p.ReportesPublicacion)
                .HasForeignKey(d => d.PublicacionLoteId)
                .HasConstraintName("FK_Reportes_PublicacionLote");

            entity.HasOne(d => d.PublicacionVaca).WithMany(p => p.ReportesPublicacion)
                .HasForeignKey(d => d.PublicacionVacaId)
                .HasConstraintName("FK_Reportes_PublicacionVaca");

            entity.HasOne(d => d.UsuarioReporta).WithMany(p => p.ReportesPublicacion)
                .HasForeignKey(d => d.UsuarioReportaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reportes_Usuario");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("PK__Roles__F92302D119FD32D7");

            entity.HasIndex(e => e.NombreRol, "UQ__Roles__4F0B537FE1352534").IsUnique();

            entity.Property(e => e.RolId).HasColumnName("RolID");
            entity.Property(e => e.NombreRol).HasMaxLength(30);
        });

        modelBuilder.Entity<Sesiones>(entity =>
        {
            entity.HasKey(e => e.SesionId).HasName("PK__Sesiones__52FD7C06F15316F0");

            entity.HasIndex(e => e.UsuarioId, "IX_Sesiones_UsuarioID");

            entity.HasIndex(e => e.Token, "UQ__Sesiones__1EB4F8175DB52F3C").IsUnique();

            entity.Property(e => e.SesionId).HasColumnName("SesionID");
            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.FechaInicio).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Token).HasMaxLength(300);
            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioID");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Sesiones)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sesiones_Usuarios");
        });

        modelBuilder.Entity<TiposPublicacion>(entity =>
        {
            entity.HasKey(e => e.TipoPublicacionId).HasName("PK__TiposPub__9015A7EAF352EA30");

            entity.HasIndex(e => e.Nombre, "UQ__TiposPub__75E3EFCF6C4359EF").IsUnique();

            entity.Property(e => e.TipoPublicacionId).HasColumnName("TipoPublicacionID");
            entity.Property(e => e.Nombre).HasMaxLength(30);
        });

        modelBuilder.Entity<TiposRegistroSanitario>(entity =>
        {
            entity.HasKey(e => e.TipoRegistroId).HasName("PK__TiposReg__16A7E9364F6F2056");

            entity.HasIndex(e => e.Nombre, "UQ__TiposReg__75E3EFCF41F38F59").IsUnique();

            entity.Property(e => e.TipoRegistroId).HasColumnName("TipoRegistroID");
            entity.Property(e => e.Nombre).HasMaxLength(30);
        });

        modelBuilder.Entity<TokensRecuperacion>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("PK__TokensRe__658FEE8AEAF3E0B6");

            entity.HasIndex(e => e.Token, "UQ__TokensRe__1EB4F817632D5CB6").IsUnique();

            entity.Property(e => e.TokenId).HasColumnName("TokenID");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Token).HasMaxLength(200);
            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioID");

            entity.HasOne(d => d.Usuario).WithMany(p => p.TokensRecuperacion)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_TokensRecuperacion_Usuarios");
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("PK__Usuarios__2B3DE7982218F792");

            entity.HasIndex(e => e.RolId, "IX_Usuarios_RolID");

            entity.HasIndex(e => e.Correo, "UQ__Usuarios__60695A19898C77B5").IsUnique();

            entity.HasIndex(e => e.NumeroDocumento, "UQ__Usuarios__A420258823803471").IsUnique();

            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioID");
            entity.Property(e => e.AmbitoEstado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](20),'USUARIO'))", true);
            entity.Property(e => e.ContrasenaHash).HasMaxLength(256);
            entity.Property(e => e.Correo).HasMaxLength(150);
            entity.Property(e => e.Direccion).HasMaxLength(200);
            entity.Property(e => e.EstadoId)
                .HasDefaultValue(1)
                .HasColumnName("EstadoID");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FotoPerfilUrl).HasMaxLength(300);
            entity.Property(e => e.NombreCompleto).HasMaxLength(150);
            entity.Property(e => e.NumeroDocumento).HasMaxLength(30);
            entity.Property(e => e.RolId).HasColumnName("RolID");
            entity.Property(e => e.Telefono).HasMaxLength(30);

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Roles");

            entity.HasOne(d => d.Estados).WithMany(p => p.Usuarios)
                .HasPrincipalKey(p => new { p.EstadoId, p.Ambito })
                .HasForeignKey(d => new { d.EstadoId, d.AmbitoEstado })
                .HasConstraintName("FK_Usuarios_Estados");
        });

        modelBuilder.Entity<Vacas>(entity =>
        {
            entity.HasKey(e => e.VacaId).HasName("PK__Vacas__1A38182249266BC0");

            entity.HasIndex(e => e.UsuarioId, "IX_Vacas_UsuarioID");

            entity.HasIndex(e => e.Identificacion, "UQ__Vacas__D6F931E57A3F427C").IsUnique();

            entity.Property(e => e.VacaId).HasColumnName("VacaID");
            entity.Property(e => e.AmbitoEstado)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](20),'VACA'))", true);
            entity.Property(e => e.CausaMuerteId).HasColumnName("CausaMuerteID");
            entity.Property(e => e.ComentarioMuerte).HasMaxLength(300);
            entity.Property(e => e.EstadoId)
                .HasDefaultValue(10)
                .HasColumnName("EstadoID");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.FotoUrl).HasMaxLength(300);
            entity.Property(e => e.Identificacion).HasMaxLength(50);
            entity.Property(e => e.MunicipioOrigenId).HasColumnName("MunicipioOrigenID");
            entity.Property(e => e.Peso).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Raza).HasMaxLength(80);
            entity.Property(e => e.Sexo).HasMaxLength(10);
            entity.Property(e => e.UsuarioId).HasColumnName("UsuarioID");

            entity.HasOne(d => d.CausaMuerte).WithMany(p => p.Vacas)
                .HasForeignKey(d => d.CausaMuerteId)
                .HasConstraintName("FK_Vacas_CausasMuerte");

            entity.HasOne(d => d.MunicipioOrigen).WithMany(p => p.Vacas)
                .HasForeignKey(d => d.MunicipioOrigenId)
                .HasConstraintName("FK_Vacas_Municipios");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Vacas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vacas_Usuarios");

            entity.HasOne(d => d.Estados).WithMany(p => p.Vacas)
                .HasPrincipalKey(p => new { p.EstadoId, p.Ambito })
                .HasForeignKey(d => new { d.EstadoId, d.AmbitoEstado })
                .HasConstraintName("FK_Vacas_Estados");
        });

        modelBuilder.Entity<VentasExternas>(entity =>
        {
            entity.HasKey(e => e.VentaExternaId).HasName("PK__VentasEx__FBCD1E0803A2FF2C");

            entity.HasIndex(e => e.VacaId, "UQ__VentasEx__1A381823B1C65781").IsUnique();

            entity.Property(e => e.VentaExternaId).HasColumnName("VentaExternaID");
            entity.Property(e => e.CompradorNombre).HasMaxLength(150);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.MunicipioVentaId).HasColumnName("MunicipioVentaID");
            entity.Property(e => e.VacaId).HasColumnName("VacaID");
            entity.Property(e => e.ValorVenta).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.MunicipioVenta).WithMany(p => p.VentasExternas)
                .HasForeignKey(d => d.MunicipioVentaId)
                .HasConstraintName("FK_VentasExternas_Municipios");

            entity.HasOne(d => d.Vaca).WithOne(p => p.VentasExternas)
                .HasForeignKey<VentasExternas>(d => d.VacaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentasExternas_Vacas");
        });

        modelBuilder.Entity<VentasPlataforma>(entity =>
        {
            entity.HasKey(e => e.VentaPlataformaId).HasName("PK__VentasPl__07D39219C724DD55");

            entity.HasIndex(e => e.PublicacionVacaId, "UQ__VentasPl__C7DFB69F6E2BB8BE").IsUnique();

            entity.Property(e => e.VentaPlataformaId).HasColumnName("VentaPlataformaID");
            entity.Property(e => e.CompradorId).HasColumnName("CompradorID");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PublicacionVacaId).HasColumnName("PublicacionVacaID");
            entity.Property(e => e.VacaId).HasColumnName("VacaID");
            entity.Property(e => e.VendedorId).HasColumnName("VendedorID");

            entity.HasOne(d => d.Comprador).WithMany(p => p.VentasPlataformaComprador)
                .HasForeignKey(d => d.CompradorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentasPlataforma_Comprador");

            entity.HasOne(d => d.PublicacionVaca).WithOne(p => p.VentasPlataforma)
                .HasForeignKey<VentasPlataforma>(d => d.PublicacionVacaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentasPlataforma_Publicacion");

            entity.HasOne(d => d.Vaca).WithMany(p => p.VentasPlataforma)
                .HasForeignKey(d => d.VacaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentasPlataforma_Vaca");

            entity.HasOne(d => d.Vendedor).WithMany(p => p.VentasPlataformaVendedor)
                .HasForeignKey(d => d.VendedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentasPlataforma_Vendedor");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
