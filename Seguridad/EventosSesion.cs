using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;

namespace TuVaca.Seguridad
{
    /// <summary>
    /// En cada solicitud revisa que la sesión siga activa en la tabla Sesiones y que el usuario siga activo.
    /// Así, cerrar sesión (RN-6-CU01), restablecer la contraseña (RN-4-CU23) o desactivar
    /// al usuario (CU-10) sacan de verdad a la persona, aunque todavía tenga la cookie.
    /// </summary>
    public class EventosSesion : CookieAuthenticationEvents
    {
        private readonly TuVacaDbContext _db;

        public EventosSesion(TuVacaDbContext db)
        {
            _db = db;
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var token = context.Principal?.ObtenerTokenSesion();
            var ahora = DateTime.Now;

            var sesion = token == null
                ? null
                : await _db.Sesiones.AsNoTracking().FirstOrDefaultAsync(s => s.Token == token);

            var valida = false;
            if (sesion != null
                && sesion.Activa
                && (sesion.FechaExpiracion == null || sesion.FechaExpiracion > ahora))
            {
                var usuarioId = sesion.UsuarioId;
                valida = await _db.Usuarios.AnyAsync(u => u.UsuarioId == usuarioId
                                                       && u.EstadoId == Catalogos.UsuarioActivo);
            }

            if (!valida)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}
