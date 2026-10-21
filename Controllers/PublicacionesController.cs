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
    /// <summary>CU-03: Publicar vaca en venta, el listado "Mis publicaciones" y pausar (FA-2).</summary>
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

        // ============================== APOYO ==============================

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
