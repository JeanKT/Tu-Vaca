using System.Security.Claims;

namespace TuVaca.Seguridad
{
    /// <summary>
    /// Datos del usuario que inició sesión. Se usan en los controladores y en las vistas:
    /// User.ObtenerUsuarioId(), User.ObtenerNombre(), User.ObtenerRol().
    /// </summary>
    public static class UsuarioActual
    {
        /// <summary>Claim con el token de la fila de la tabla Sesiones (RN-6-CU01).</summary>
        public const string ClaimSesion = "TuVaca:Sesion";

        public static int? ObtenerUsuarioId(this ClaimsPrincipal usuario)
        {
            var valor = usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(valor, out var id) ? id : null;
        }

        public static string ObtenerNombre(this ClaimsPrincipal usuario) =>
            usuario.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

        public static string ObtenerRol(this ClaimsPrincipal usuario) =>
            usuario.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        public static string? ObtenerTokenSesion(this ClaimsPrincipal usuario) =>
            usuario.FindFirst(ClaimSesion)?.Value;

        public static bool EstaAutenticado(this ClaimsPrincipal usuario) =>
            usuario.Identity?.IsAuthenticated == true;
    }
}
