using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Entidades;

namespace TuVaca.Servicios
{
    /// <summary>IDs fijos de la tabla TiposNotificacion (BaseDeDatos/TuVacaDB_02_notificaciones.sql).</summary>
    public static class TiposNotificacionId
    {
        public const int MensajeDeComprador = 1;    // CU-06 paso 6
        public const int RecordatorioDosis = 2;     // CU-07 FA-2
        public const int PublicacionAprobada = 3;   // CU-09 RN-4
        public const int PublicacionRechazada = 4;  // CU-09 RN-4 (con el motivo, RN-2)
        public const int PublicacionEliminada = 5;  // CU-09 RN-4 (con el motivo, RN-2)
        public const int PublicacionCancelada = 6;  // CU-13 FA-2 y CU-14 FA-2
        public const int VentaConfirmada = 7;       // CU-15 RN-6
    }

    /// <summary>Una notificación lista para mostrar.</summary>
    public record NotificacionVista(int Id, string Tipo, string Mensaje, string? Enlace, DateTime Fecha, bool Leida);

    /// <summary>
    /// Notificaciones dentro de la plataforma (la campanita).
    /// Los demás casos de uso solo tienen que llamar a CrearAsync.
    /// </summary>
    public class NotificacionService
    {
        private readonly TuVacaDbContext _db;

        public NotificacionService(TuVacaDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Crea una notificación para un usuario.
        /// enlace: página que se abre al hacer clic (por ejemplo "/Publicaciones").
        /// fechaProgramada: desde cuándo se muestra. Para el recordatorio de la próxima dosis
        /// (CU-07 FA-2) se manda la fecha de la dosis; si es null, se muestra de una vez.
        /// </summary>
        public async Task CrearAsync(int usuarioId, int tipoNotificacionId, string mensaje,
                                     string? enlace = null, DateTime? fechaProgramada = null)
        {
            var ahora = DateTime.Now;
            _db.Notificaciones.Add(new Notificaciones
            {
                UsuarioId = usuarioId,
                TipoNotificacionId = tipoNotificacionId,
                Mensaje = mensaje,
                Enlace = enlace,
                FechaProgramada = fechaProgramada ?? ahora,
                EstadoId = Catalogos.NotificacionNoLeida,
                FechaCreacion = ahora
            });
            await _db.SaveChangesAsync();
        }

        public Task<int> ContarNoLeidasAsync(int usuarioId) =>
            Visibles(usuarioId).CountAsync(n => n.EstadoId == Catalogos.NotificacionNoLeida);

        /// <summary>Notificaciones del usuario, de la más reciente a la más antigua.</summary>
        public async Task<List<NotificacionVista>> ListarAsync(int usuarioId, int? cantidad = null)
        {
            var consulta =
                from n in Visibles(usuarioId)
                join t in _db.TiposNotificacion on n.TipoNotificacionId equals t.TipoNotificacionId
                orderby n.FechaProgramada descending, n.NotificacionId descending
                select new NotificacionVista(
                    n.NotificacionId,
                    t.Nombre,
                    n.Mensaje,
                    n.Enlace,
                    n.FechaProgramada,
                    n.EstadoId == Catalogos.NotificacionLeida);

            if (cantidad.HasValue)
            {
                consulta = consulta.Take(cantidad.Value);
            }
            return await consulta.ToListAsync();
        }

        /// <summary>Marca como leída una notificación del usuario. Devuelve null si no existe o no es suya.</summary>
        public async Task<Notificaciones?> MarcarLeidaAsync(int notificacionId, int usuarioId)
        {
            var notificacion = await _db.Notificaciones
                .FirstOrDefaultAsync(n => n.NotificacionId == notificacionId && n.UsuarioId == usuarioId);

            if (notificacion != null && notificacion.EstadoId != Catalogos.NotificacionLeida)
            {
                notificacion.EstadoId = Catalogos.NotificacionLeida;
                await _db.SaveChangesAsync();
            }
            return notificacion;
        }

        public async Task MarcarTodasLeidasAsync(int usuarioId)
        {
            var pendientes = await Visibles(usuarioId)
                .Where(n => n.EstadoId == Catalogos.NotificacionNoLeida)
                .ToListAsync();

            foreach (var notificacion in pendientes)
            {
                notificacion.EstadoId = Catalogos.NotificacionLeida;
            }
            await _db.SaveChangesAsync();
        }

        /// <summary>Las del usuario que ya llegaron a su fecha (los recordatorios futuros todavía no se ven).</summary>
        private IQueryable<Notificaciones> Visibles(int usuarioId)
        {
            var ahora = DateTime.Now;
            return _db.Notificaciones.Where(n => n.UsuarioId == usuarioId && n.FechaProgramada <= ahora);
        }
    }
}
