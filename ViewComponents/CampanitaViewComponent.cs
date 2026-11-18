using Microsoft.AspNetCore.Mvc;
using TuVaca.Seguridad;
using TuVaca.Servicios;

namespace TuVaca.ViewComponents
{
    /// <summary>Lo que muestra la campanita: cuántas faltan por leer y las 5 más recientes.</summary>
    public record CampanitaModelo(int NoLeidas, List<NotificacionVista> Ultimas);

    /// <summary>
    /// La campanita de la barra de navegación. _Navbar.cshtml la muestra sola en cuanto existe esta clase.
    /// Su vista está en Views/Shared/Components/Campanita/Default.cshtml.
    /// </summary>
    public class CampanitaViewComponent : ViewComponent
    {
        private readonly NotificacionService _notificaciones;
        private readonly ILogger<CampanitaViewComponent> _logger;

        public CampanitaViewComponent(NotificacionService notificaciones, ILogger<CampanitaViewComponent> logger)
        {
            _notificaciones = notificaciones;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var usuarioId = UserClaimsPrincipal.ObtenerUsuarioId();
            if (usuarioId == null)
            {
                return Content(string.Empty);
            }

            try
            {
                var modelo = new CampanitaModelo(
                    await _notificaciones.ContarNoLeidasAsync(usuarioId.Value),
                    await _notificaciones.ListarAsync(usuarioId.Value, 5));
                return View(modelo);
            }
            catch (Exception ex)
            {
                // Si falla (por ejemplo, falta ejecutar el script 02), la página sigue funcionando sin campanita
                _logger.LogError(ex, "No se pudieron cargar las notificaciones de la campanita");
                return Content(string.Empty);
            }
        }
    }
}
