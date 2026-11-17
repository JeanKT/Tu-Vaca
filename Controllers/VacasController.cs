using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Entidades;
using TuVaca.Models.Hato;
using TuVaca.Seguridad;

namespace TuVaca.Controllers
{
    /// <summary>CU-02: Registrar vaca propia, el listado "Mis vacas" y CU-12: Editar información de vaca.</summary>
    [Authorize(Roles = Catalogos.RolProductor)]
    public class VacasController : Controller
    {
        private readonly TuVacaDbContext _db;
        private readonly IWebHostEnvironment _entorno;
        private readonly ILogger<VacasController> _logger;

        public VacasController(TuVacaDbContext db, IWebHostEnvironment entorno, ILogger<VacasController> logger)
        {
            _db = db;
            _entorno = entorno;
            _logger = logger;
        }

        // GET /Vacas: "Mis vacas" (pasos 1 y 6)
        public async Task<IActionResult> Index()
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;

            var vacas = await (
                from v in _db.Vacas
                join e in _db.Estados on v.EstadoId equals e.EstadoId
                where v.UsuarioId == usuarioId // RN-3: solo las vacas de quien inició sesión
                orderby v.FechaRegistro descending
                select new VacaListadoItem
                {
                    VacaId = v.VacaId,
                    Identificacion = v.Identificacion,
                    Raza = v.Raza,
                    Sexo = v.Sexo,
                    FechaNacimiento = v.FechaNacimiento,
                    Peso = v.Peso,
                    FotoUrl = v.FotoUrl,
                    EstadoId = v.EstadoId,
                    Estado = e.Nombre,
                    TienePublicacionActiva = _db.PublicacionesVaca.Any(p =>
                        p.VacaId == v.VacaId && p.EstadoId == Catalogos.PublicacionActiva)
                }).ToListAsync();

            return View(vacas);
        }

        // GET /Vacas/Registrar: formulario (paso 2)
        [HttpGet]
        public async Task<IActionResult> Registrar()
        {
            await CargarDepartamentosAsync();
            return View(new RegistrarVacaViewModel());
        }

        // POST /Vacas/Registrar: pasos 3 a 6
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarVacaViewModel modelo)
        {
            // RN-2: fecha de nacimiento o, si no se sabe, edad aproximada
            if (modelo.FechaNacimiento == null && modelo.EdadMeses == null)
            {
                ModelState.AddModelError(nameof(modelo.FechaNacimiento),
                    "Escribe la fecha de nacimiento o la edad aproximada.");
            }

            // FE-2: la fecha no puede ser futura
            if (modelo.FechaNacimiento != null && modelo.FechaNacimiento.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(modelo.FechaNacimiento), "La fecha de nacimiento no puede ser futura.");
            }

            // Origen (opcional): si se escogió, el municipio debe existir en el catálogo
            if (modelo.MunicipioOrigenId != null
                && !await _db.Municipios.AnyAsync(m => m.MunicipioId == modelo.MunicipioOrigenId))
            {
                ModelState.AddModelError(nameof(modelo.MunicipioOrigenId), "Selecciona un municipio válido.");
            }

            // RN-4: la foto es opcional, pero si se sube debe ser una imagen de máximo 2 MB
            var errorFoto = Fotos.Validar(modelo.Foto);
            if (errorFoto != null)
            {
                ModelState.AddModelError(nameof(modelo.Foto), errorFoto);
            }

            if (!ModelState.IsValid)
            {
                await CargarDepartamentosAsync();
                return View(modelo); // FE-2: mensajes por campo
            }

            var identificacion = modelo.Identificacion.Trim();

            try
            {
                // Paso 4 / FE-3: la identificación no puede repetirse (RN-1)
                if (await _db.Vacas.AnyAsync(v => v.Identificacion == identificacion))
                {
                    ModelState.AddModelError(nameof(modelo.Identificacion), "Esta identificación ya está registrada.");
                    await CargarDepartamentosAsync();
                    return View(modelo);
                }

                // Si solo se conoce la edad, se guarda una fecha de nacimiento aproximada
                var hoy = DateOnly.FromDateTime(DateTime.Today);
                var nacimiento = modelo.FechaNacimiento != null
                    ? DateOnly.FromDateTime(modelo.FechaNacimiento.Value)
                    : hoy.AddMonths(-modelo.EdadMeses!.Value);

                // FA-2: sin foto se guarda null y el listado muestra una imagen genérica
                string? fotoUrl = null;
                if (modelo.Foto != null && modelo.Foto.Length > 0)
                {
                    fotoUrl = await Fotos.GuardarAsync(modelo.Foto, _entorno.WebRootPath, "vacas");
                }

                // Paso 5: se registra y queda asociada al Productor/Vendedor (RN-3)
                var vaca = new Vacas
                {
                    UsuarioId = User.ObtenerUsuarioId()!.Value,
                    Identificacion = identificacion,
                    Raza = modelo.Raza.Trim(),
                    Sexo = modelo.Sexo,
                    FechaNacimiento = nacimiento,
                    Peso = modelo.Peso!.Value,
                    FotoUrl = fotoUrl,
                    MunicipioOrigenId = modelo.MunicipioOrigenId,
                    EstadoId = Catalogos.VacaViva,
                    FechaRegistro = DateTime.Now
                };
                _db.Vacas.Add(vaca);
                await _db.SaveChangesAsync();

                // Paso 6: confirmación y la vaca aparece en "Mis vacas"
                TempData["Exito"] = $"La vaca {vaca.Identificacion} quedó registrada.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-02: error al registrar la vaca");
                ModelState.AddModelError(string.Empty, "No fue posible registrar la vaca. Intenta nuevamente."); // FE-1
                await CargarDepartamentosAsync();
                return View(modelo);
            }
        }

        // ============================== CU-12 ==============================

        // GET /Vacas/Editar/5  (pasos 1 a 3: formulario precargado)
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var vaca = await BuscarVacaPropiaAsync(id);
            if (vaca == null)
            {
                return NotFound(); // RN-1-CU12: solo el dueño
            }
            if (vaca.EstadoId != Catalogos.VacaViva)
            {
                TempData["Error"] = "Solo se pueden editar vacas en estado Viva.";
                return RedirectToAction(nameof(Index));
            }

            var modelo = new EditarVacaViewModel
            {
                VacaId = vaca.VacaId,
                Identificacion = vaca.Identificacion,
                FotoActualUrl = vaca.FotoUrl,
                Raza = vaca.Raza,
                Sexo = vaca.Sexo,
                FechaNacimiento = vaca.FechaNacimiento?.ToDateTime(TimeOnly.MinValue),
                Peso = vaca.Peso,
                MunicipioOrigenId = vaca.MunicipioOrigenId
            };
            if (vaca.MunicipioOrigenId != null)
            {
                modelo.DepartamentoId = await _db.Municipios
                    .Where(m => m.MunicipioId == vaca.MunicipioOrigenId)
                    .Select(m => (int?)m.DepartamentoId)
                    .FirstOrDefaultAsync();
            }

            await CargarDepartamentosAsync();
            return View(modelo);
        }

        // POST /Vacas/Editar  (pasos 4 a 7)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarVacaViewModel modelo)
        {
            var vaca = await BuscarVacaPropiaAsync(modelo.VacaId);
            if (vaca == null)
            {
                return NotFound();
            }
            modelo.Identificacion = vaca.Identificacion; // RN-2: no se edita
            modelo.FotoActualUrl = vaca.FotoUrl;

            if (vaca.EstadoId != Catalogos.VacaViva)
            {
                ModelState.AddModelError(string.Empty, "Solo se pueden editar vacas en estado Viva.");
            }
            if (modelo.FechaNacimiento != null && modelo.FechaNacimiento.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(modelo.FechaNacimiento), "La fecha de nacimiento no puede ser futura.");
            }
            if (modelo.MunicipioOrigenId != null
                && !await _db.Municipios.AnyAsync(m => m.MunicipioId == modelo.MunicipioOrigenId))
            {
                ModelState.AddModelError(nameof(modelo.MunicipioOrigenId), "Selecciona un municipio válido.");
            }
            var errorFoto = Fotos.Validar(modelo.Foto);
            if (errorFoto != null)
            {
                ModelState.AddModelError(nameof(modelo.Foto), errorFoto);
            }

            if (!ModelState.IsValid)
            {
                await CargarDepartamentosAsync();
                return View(modelo); // FE-2
            }

            try
            {
                var fotoAnterior = vaca.FotoUrl;
                if (modelo.Foto != null && modelo.Foto.Length > 0)
                {
                    vaca.FotoUrl = await Fotos.GuardarAsync(modelo.Foto, _entorno.WebRootPath, "vacas");
                }

                // Paso 6
                vaca.Raza = modelo.Raza.Trim();
                vaca.Sexo = modelo.Sexo;
                vaca.FechaNacimiento = DateOnly.FromDateTime(modelo.FechaNacimiento!.Value);
                vaca.Peso = modelo.Peso!.Value;
                vaca.MunicipioOrigenId = modelo.MunicipioOrigenId;
                vaca.FechaActualizacion = DateTime.Now; // RN-5: trazabilidad

                // RN-4 / FA-2: si la publicación activa usaba la foto de la vaca, se actualiza también.
                // (Raza, peso y edad de la publicación se leen siempre de la vaca, así que ya quedan al día.)
                if (vaca.FotoUrl != fotoAnterior && fotoAnterior != null)
                {
                    var publicaciones = await _db.PublicacionesVaca
                        .Where(p => p.VacaId == vaca.VacaId && p.EstadoId == Catalogos.PublicacionActiva && p.FotoUrl == fotoAnterior)
                        .ToListAsync();
                    foreach (var p in publicaciones)
                    {
                        p.FotoUrl = vaca.FotoUrl!;
                        p.FechaActualizacion = DateTime.Now;
                    }
                }

                await _db.SaveChangesAsync();

                // Paso 7
                TempData["Exito"] = $"La información de la vaca {vaca.Identificacion} quedó actualizada.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-12: error al editar la vaca {VacaId}", vaca.VacaId);
                ModelState.AddModelError(string.Empty, "No fue posible actualizar la información. Intenta nuevamente."); // FE-1
                await CargarDepartamentosAsync();
                return View(modelo);
            }
        }

        private Task<Vacas?> BuscarVacaPropiaAsync(int vacaId)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            return _db.Vacas.FirstOrDefaultAsync(v => v.VacaId == vacaId && v.UsuarioId == usuarioId);
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
