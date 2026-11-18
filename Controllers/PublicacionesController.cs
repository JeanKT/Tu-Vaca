using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Entidades;
using TuVaca.Models.Publicaciones;
using TuVaca.Seguridad;

namespace TuVaca.Controllers
{
    /// <summary>
    /// CU-03: Publicar vaca en venta, "Mis publicaciones", pausar (FA-2) y reanudar.
    /// CU-16: Editar publicación. CU-17: Eliminar publicación.
    /// </summary>
    [Authorize(Roles = Catalogos.RolProductor)]
    public class PublicacionesController : Controller
    {
        private const string MensajeYaPublicada = "Esta vaca ya cuenta con una publicación activa."; // FE-3

        private readonly TuVacaDbContext _db;
        private readonly IWebHostEnvironment _entorno;
        private readonly ILogger<PublicacionesController> _logger;

        public PublicacionesController(TuVacaDbContext db, IWebHostEnvironment entorno, ILogger<PublicacionesController> logger)
        {
            _db = db;
            _entorno = entorno;
            _logger = logger;
        }

        // GET /Publicaciones: "Mis publicaciones" (paso 7)
        public async Task<IActionResult> Index()
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;

            var publicaciones = await (
                from p in _db.PublicacionesVaca
                join v in _db.Vacas on p.VacaId equals v.VacaId
                join e in _db.Estados on p.EstadoId equals e.EstadoId
                join m in _db.Municipios on p.MunicipioId equals m.MunicipioId
                join d in _db.Departamentos on m.DepartamentoId equals d.DepartamentoId
                where v.UsuarioId == usuarioId
                orderby p.FechaPublicacion descending
                select new PublicacionListadoItem
                {
                    PublicacionVacaId = p.PublicacionVacaId,
                    VacaId = v.VacaId,
                    Identificacion = v.Identificacion,
                    Raza = v.Raza,
                    Precio = p.Precio,
                    Descripcion = p.Descripcion,
                    Ubicacion = m.Nombre + ", " + d.Nombre,
                    FotoUrl = p.FotoUrl,
                    EstadoId = p.EstadoId,
                    Estado = e.Nombre,
                    MotivoRechazo = p.MotivoRechazo,
                    FechaPublicacion = p.FechaPublicacion
                }).ToListAsync();

            return View(publicaciones);
        }

        // GET /Publicaciones/Crear?vacaId=5: formulario con los datos de la vaca precargados (pasos 1 a 3)
        [HttpGet]
        public async Task<IActionResult> Crear(int vacaId)
        {
            var vaca = await BuscarVacaPropiaAsync(vacaId);
            if (vaca == null)
            {
                return NotFound(); // RN-1: solo vacas propias
            }

            var problema = await ProblemaParaPublicarAsync(vaca);
            if (problema != null)
            {
                TempData["Error"] = problema;
                return RedirectToAction("Index", "Vacas");
            }

            var modelo = new PublicarVacaViewModel
            {
                VacaId = vaca.VacaId,
                UsarFotoDeLaVaca = vaca.FotoUrl != null
            };

            // Si la vaca tiene municipio de origen, se propone como ubicación
            if (vaca.MunicipioOrigenId != null)
            {
                var municipioOrigen = vaca.MunicipioOrigenId.Value;
                modelo.MunicipioId = municipioOrigen;
                modelo.DepartamentoId = await _db.Municipios
                    .Where(m => m.MunicipioId == municipioOrigen)
                    .Select(m => (int?)m.DepartamentoId)
                    .FirstOrDefaultAsync();
            }

            CompletarDatosVaca(modelo, vaca);
            await CargarDepartamentosAsync();
            return View(modelo);
        }

        // POST /Publicaciones/Crear: pasos 4 a 7
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(PublicarVacaViewModel modelo)
        {
            var vaca = await BuscarVacaPropiaAsync(modelo.VacaId);
            if (vaca == null)
            {
                return NotFound(); // RN-1
            }
            CompletarDatosVaca(modelo, vaca);

            // RN-3 / FE-3: una vaca no puede tener dos publicaciones activas
            var problema = await ProblemaParaPublicarAsync(vaca);
            if (problema != null)
            {
                ModelState.AddModelError(string.Empty, problema);
            }

            // RN-4: al menos una fotografía, la de la vaca o una nueva
            var usarFotoDeLaVaca = modelo.UsarFotoDeLaVaca && vaca.FotoUrl != null;
            if (!usarFotoDeLaVaca)
            {
                if (modelo.FotoNueva == null || modelo.FotoNueva.Length == 0)
                {
                    ModelState.AddModelError(nameof(modelo.FotoNueva), "La publicación necesita al menos una fotografía.");
                }
                else
                {
                    var errorFoto = Fotos.Validar(modelo.FotoNueva);
                    if (errorFoto != null)
                    {
                        ModelState.AddModelError(nameof(modelo.FotoNueva), errorFoto);
                    }
                }
            }

            if (modelo.MunicipioId != null
                && !await _db.Municipios.AnyAsync(m => m.MunicipioId == modelo.MunicipioId))
            {
                ModelState.AddModelError(nameof(modelo.MunicipioId), "Selecciona un municipio válido.");
            }

            if (!ModelState.IsValid)
            {
                await CargarDepartamentosAsync();
                return View(modelo); // FE-2
            }

            try
            {
                var fotoUrl = usarFotoDeLaVaca
                    ? vaca.FotoUrl!
                    : await Fotos.GuardarAsync(modelo.FotoNueva!, _entorno.WebRootPath, "publicaciones");

                // Paso 6: se crea la publicación como "Activa"
                var publicacion = new PublicacionesVaca
                {
                    VacaId = vaca.VacaId,
                    Precio = modelo.Precio!.Value,
                    Descripcion = modelo.Descripcion.Trim(),
                    MunicipioId = modelo.MunicipioId!.Value,
                    FotoUrl = fotoUrl,
                    EstadoId = Catalogos.PublicacionActiva,
                    FechaPublicacion = DateTime.Now
                };
                _db.PublicacionesVaca.Add(publicacion);
                await _db.SaveChangesAsync();

                // Paso 7: confirmación y la publicación aparece en "Mis publicaciones"
                TempData["Exito"] = $"La vaca {vaca.Identificacion} quedó publicada en venta.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-03: error al publicar la vaca {VacaId}", vaca.VacaId);

                // Si otra pestaña la publicó al mismo tiempo, la base lo impide (índice de una activa por vaca)
                var mensaje = await TienePublicacionActivaAsync(vaca.VacaId)
                    ? MensajeYaPublicada                                     // FE-3
                    : "No fue posible publicar la vaca. Intenta nuevamente."; // FE-1
                ModelState.AddModelError(string.Empty, mensaje);
                await CargarDepartamentosAsync();
                return View(modelo);
            }
        }

        // POST /Publicaciones/Pausar/5 (FA-2): deja de mostrarse a los compradores
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pausar(int id)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            var publicacion = await (
                from p in _db.PublicacionesVaca
                join v in _db.Vacas on p.VacaId equals v.VacaId
                where p.PublicacionVacaId == id && v.UsuarioId == usuarioId
                select p).FirstOrDefaultAsync();

            if (publicacion == null)
            {
                return NotFound();
            }

            if (publicacion.EstadoId != Catalogos.PublicacionActiva)
            {
                TempData["Error"] = "Solo se pueden pausar publicaciones activas.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                publicacion.EstadoId = Catalogos.PublicacionPausada;
                publicacion.FechaActualizacion = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["Exito"] = "La publicación quedó pausada: los compradores ya no la ven.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-03: error al pausar la publicación {Id}", id);
                TempData["Error"] = "No fue posible pausar la publicación. Intenta nuevamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST /Publicaciones/Reanudar/5: vuelve a mostrar una publicación pausada (RN-5-CU03)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reanudar(int id)
        {
            var publicacion = await BuscarPublicacionPropiaAsync(id);
            if (publicacion == null)
            {
                return NotFound();
            }
            if (publicacion.EstadoId != Catalogos.PublicacionPausada)
            {
                TempData["Error"] = "Solo se pueden reanudar publicaciones pausadas.";
                return RedirectToAction(nameof(Index));
            }
            if (await TienePublicacionActivaAsync(publicacion.VacaId))
            {
                TempData["Error"] = MensajeYaPublicada;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                publicacion.EstadoId = Catalogos.PublicacionActiva;
                publicacion.FechaActualizacion = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["Exito"] = "La publicación está activa otra vez.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-03: error al reanudar la publicación {Id}", id);
                TempData["Error"] = "No fue posible reanudar la publicación. Intenta nuevamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================== CU-16 ==============================

        // GET /Publicaciones/Editar/5  (pasos 1 a 3)
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var publicacion = await BuscarPublicacionPropiaAsync(id);
            if (publicacion == null)
            {
                return NotFound(); // RN-1-CU16
            }
            if (!SePuedeModificar(publicacion.EstadoId))
            {
                TempData["Error"] = "Solo se pueden editar publicaciones activas o pausadas."; // FE-3 / RN-4
                return RedirectToAction(nameof(Index));
            }

            var vaca = await _db.Vacas.FirstAsync(v => v.VacaId == publicacion.VacaId);
            var modelo = new EditarPublicacionViewModel
            {
                PublicacionVacaId = publicacion.PublicacionVacaId,
                VacaResumen = $"Vaca {vaca.Identificacion} · {vaca.Raza} · {vaca.Sexo}",
                FotoActualUrl = publicacion.FotoUrl,
                Precio = publicacion.Precio,
                Descripcion = publicacion.Descripcion,
                MunicipioId = publicacion.MunicipioId,
                DepartamentoId = await _db.Municipios
                    .Where(m => m.MunicipioId == publicacion.MunicipioId)
                    .Select(m => (int?)m.DepartamentoId)
                    .FirstOrDefaultAsync()
            };

            await CargarDepartamentosAsync();
            return View(modelo);
        }

        // POST /Publicaciones/Editar  (pasos 4 a 7)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarPublicacionViewModel modelo)
        {
            var publicacion = await BuscarPublicacionPropiaAsync(modelo.PublicacionVacaId);
            if (publicacion == null)
            {
                return NotFound();
            }
            modelo.FotoActualUrl = publicacion.FotoUrl;
            var vaca = await _db.Vacas.FirstAsync(v => v.VacaId == publicacion.VacaId);
            modelo.VacaResumen = $"Vaca {vaca.Identificacion} · {vaca.Raza} · {vaca.Sexo}";

            if (!SePuedeModificar(publicacion.EstadoId))
            {
                ModelState.AddModelError(string.Empty, "Solo se pueden editar publicaciones activas o pausadas.");
            }
            if (modelo.MunicipioId != null && !await _db.Municipios.AnyAsync(m => m.MunicipioId == modelo.MunicipioId))
            {
                ModelState.AddModelError(nameof(modelo.MunicipioId), "Selecciona un municipio válido.");
            }
            var errorFoto = Fotos.Validar(modelo.FotoNueva);
            if (errorFoto != null)
            {
                ModelState.AddModelError(nameof(modelo.FotoNueva), errorFoto);
            }

            if (!ModelState.IsValid)
            {
                await CargarDepartamentosAsync();
                return View(modelo); // FE-2
            }

            try
            {
                if (modelo.FotoNueva != null && modelo.FotoNueva.Length > 0)
                {
                    publicacion.FotoUrl = await Fotos.GuardarAsync(modelo.FotoNueva, _entorno.WebRootPath, "publicaciones");
                }
                // Paso 6 (RN-2: la vaca no cambia)
                publicacion.Precio = modelo.Precio!.Value;
                publicacion.Descripcion = modelo.Descripcion.Trim();
                publicacion.MunicipioId = modelo.MunicipioId!.Value;
                publicacion.FechaActualizacion = DateTime.Now; // RN-5
                await _db.SaveChangesAsync();

                TempData["Exito"] = "La publicación quedó actualizada.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-16: error al editar la publicación {Id}", modelo.PublicacionVacaId);
                ModelState.AddModelError(string.Empty, "No fue posible actualizar la publicación. Intenta nuevamente."); // FE-1
                await CargarDepartamentosAsync();
                return View(modelo);
            }
        }

        // ============================== CU-17 ==============================

        // POST /Publicaciones/Eliminar/5  (pasos 2 a 6; la confirmación del paso 3 la pide la vista)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var publicacion = await BuscarPublicacionPropiaAsync(id);
            if (publicacion == null)
            {
                return NotFound(); // RN-1-CU17
            }
            // RN-2 / FE-2: una publicación vendida se conserva como historial
            if (!SePuedeModificar(publicacion.EstadoId))
            {
                TempData["Error"] = "Esta publicación no se puede eliminar.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Se retira de las búsquedas cambiando su estado a "Cancelada":
                // los mensajes de compradores que la referencian se conservan y queda la fecha (RN-4).
                // La vaca NO se elimina y se puede volver a publicar (RN-3).
                publicacion.EstadoId = Catalogos.PublicacionCancelada;
                publicacion.FechaActualizacion = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["Exito"] = "La publicación fue eliminada. La vaca sigue en \"Mis vacas\" y puedes volver a publicarla.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-17: error al eliminar la publicación {Id}", id);
                TempData["Error"] = "No fue posible eliminar la publicación. Intenta nuevamente."; // FE-1
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================== APOYO ==============================

        private static bool SePuedeModificar(int estadoId) =>
            estadoId == Catalogos.PublicacionActiva || estadoId == Catalogos.PublicacionPausada;

        private Task<PublicacionesVaca?> BuscarPublicacionPropiaAsync(int id)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            return (from p in _db.PublicacionesVaca
                    join v in _db.Vacas on p.VacaId equals v.VacaId
                    where p.PublicacionVacaId == id && v.UsuarioId == usuarioId
                    select p).FirstOrDefaultAsync();
        }

        private Task<Vacas?> BuscarVacaPropiaAsync(int vacaId)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            return _db.Vacas.FirstOrDefaultAsync(v => v.VacaId == vacaId && v.UsuarioId == usuarioId);
        }

        private Task<bool> TienePublicacionActivaAsync(int vacaId) =>
            _db.PublicacionesVaca.AnyAsync(p => p.VacaId == vacaId && p.EstadoId == Catalogos.PublicacionActiva);

        /// <summary>Devuelve por qué no se puede publicar la vaca, o null si sí se puede.</summary>
        private async Task<string?> ProblemaParaPublicarAsync(Vacas vaca)
        {
            if (vaca.EstadoId != Catalogos.VacaViva)
            {
                return "Solo se pueden publicar vacas en estado Viva.";
            }
            if (await TienePublicacionActivaAsync(vaca.VacaId))
            {
                return MensajeYaPublicada; // FE-3
            }
            return null;
        }

        private static void CompletarDatosVaca(PublicarVacaViewModel modelo, Vacas vaca)
        {
            modelo.Identificacion = vaca.Identificacion;
            modelo.Raza = vaca.Raza;
            modelo.Sexo = vaca.Sexo;
            modelo.Edad = Formatos.Edad(vaca.FechaNacimiento);
            modelo.Peso = vaca.Peso;
            modelo.FotoVacaUrl = vaca.FotoUrl;
        }

        private async Task CargarDepartamentosAsync()
        {
            ViewBag.Departamentos = await _db.Departamentos
                .OrderBy(d => d.Nombre)
                .Select(d => new SelectListItem { Value = d.DepartamentoId.ToString(), Text = d.Nombre })
                .ToListAsync();
        }
    }
}
