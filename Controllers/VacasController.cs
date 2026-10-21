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
    /// <summary>CU-02: Registrar vaca propia, y el listado "Mis vacas".</summary>
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

        private async Task CargarDepartamentosAsync()
        {
            ViewBag.Departamentos = await _db.Departamentos
                .OrderBy(d => d.Nombre)
                .Select(d => new SelectListItem { Value = d.DepartamentoId.ToString(), Text = d.Nombre })
                .ToListAsync();
        }
    }
}
