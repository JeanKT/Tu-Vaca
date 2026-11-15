using Microsoft.EntityFrameworkCore;
using TuVaca.Models.Entidades;

namespace TuVaca.Data;

/// <summary>
/// Le avisa a Entity Framework qué tablas tienen triggers
/// (BaseDeDatos/TuVacaDB_03_procedimientos_triggers.sql).
/// Sin esto, SQL Server rechaza los INSERT/UPDATE de EF con el error
/// "The target table ... cannot have any enabled triggers if the statement contains an OUTPUT clause".
/// Va en un archivo aparte para que no se pierda si se vuelve a hacer scaffold del DbContext.
/// </summary>
public partial class TuVacaDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vacas>().ToTable(tb =>
        {
            tb.HasTrigger("trg_Vacas_Auditoria");
            tb.HasTrigger("trg_Vacas_CancelarPublicaciones");
        });

        modelBuilder.Entity<RegistrosSanitarios>().ToTable(tb =>
            tb.HasTrigger("trg_RegistrosSanitarios_NoEliminar"));
    }
}
