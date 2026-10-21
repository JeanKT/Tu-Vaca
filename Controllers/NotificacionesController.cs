using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuVaca.Seguridad;
using TuVaca.Servicios;

namespace TuVaca.Controllers
{
    /// <summary>Notificaciones dentro de la plataforma: la página "Mis notificaciones" y las acciones de la campanita.</summary>
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly NotificacionService _notificaciones;
        private readonly IWebHostEnvironment _entorno;

        public NotificacionesController(NotificacionService notificaciones, IWebHostEnvironment entorno)
        {
            _notificaciones = notificaciones;
            _entorno = entorno;
        }

        // GET /Notificaciones: todas las notificaciones del usuario
        public async Task<IActionResult> Index()
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            ViewData["EsDesarrollo"] = _entorno.IsDevelopment();
            return View(await _notificaciones.ListarAsync(usuarioId));
        }

        // GET /Notificaciones/Abrir/5: la marca como leída y lleva a la página relacionada
        public async Task<IActionResult> Abrir(int id)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            var notificacion = await _notificaciones.MarcarLeidaAsync(id, usuarioId);
            if (notificacion == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(notificacion.Enlace) && Url.IsLocalUrl(notificacion.Enlace))
            {
                return LocalRedirect(notificacion.Enlace);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarTodas(string? returnUrl = null)
        {
            await _notificaciones.MarcarTodasLeidasAsync(User.ObtenerUsuarioId()!.Value);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Solo en desarrollo: crea una notificación de ejemplo para probar la campanita
        /// mientras los demás casos de uso todavía no generan notificaciones.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prueba()
        {
            if (!_entorno.IsDevelopment())
            {
                return NotFound();
            }

            await _notificaciones.CrearAsync(
                User.ObtenerUsuarioId()!.Value,
                TiposNotificacionId.PublicacionAprobada,
                "Notificación de prueba: tu publicación fue aprobada por el administrador.",
                Url.Action(nameof(Index)));

            TempData["Exito"] = "Se creó una notificación de prueba. Mira la campanita.";
            return RedirectToAction(nameof(Index));
        }
    }
}
