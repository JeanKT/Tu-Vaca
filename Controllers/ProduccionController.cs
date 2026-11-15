using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Entidades;
using TuVaca.Models.Produccion;
using TuVaca.Seguridad;

namespace TuVaca.Controllers
{
    /// <summary>
    /// CU-11: Registrar y consultar producción de leche.
    /// Incluye eliminar un registro (CU-22, flujo eliminar).
    /// </summary>
    [Authorize(Roles = Catalogos.RolProductor)]
    public class ProduccionController : Controller
    {
        private readonly TuVacaDbContext _db;
        private readonly ILogger<ProduccionController> _logger;

        public ProduccionController(TuVacaDbContext db, ILogger<ProduccionController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /Produccion?desde=...&hasta=...  Resumen de todo el hato (menú "Producción")
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            var (d, h) = Rango(desde, hasta);
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            var dOnly = DateOnly.FromDateTime(d);
            var hOnly = DateOnly.FromDateTime(h);

            var vacas = await _db.Vacas
                .Where(v => v.UsuarioId == usuarioId && v.Sexo == "Hembra")
                .OrderBy(v => v.Identificacion)
                .Select(v => new ResumenProduccionItem
                {
                    VacaId = v.VacaId,
                    Identificacion = v.Identificacion,
                    Raza = v.Raza,
                    Dias = v.ProduccionLeche.Count(p => p.Fecha >= dOnly && p.Fecha <= hOnly),
                    Total = v.ProduccionLeche.Where(p => p.Fecha >= dOnly && p.Fecha <= hOnly)
                                             .Sum(p => (decimal?)p.CantidadLitros) ?? 0
                })
                .ToListAsync();

            return View(new ResumenProduccionViewModel { Desde = d, Hasta = h, Vacas = vacas });
        }

        // GET /Produccion/Historial/5?desde=...&hasta=...  (flujo consultar, FA-3 rango de fechas)
        [HttpGet]
        public async Task<IActionResult> Historial(int id, DateTime? desde, DateTime? hasta)
        {
            var vaca = await BuscarVacaPropiaAsync(id);
            if (vaca == null)
            {
                return NotFound(); // RN-1
            }

            var (d, h) = Rango(desde, hasta);
            var modelo = new HistorialProduccionViewModel
            {
                VacaId = vaca.VacaId,
                VacaIdentificacion = vaca.Identificacion,
                VacaRaza = vaca.Raza,
                VacaViva = vaca.EstadoId == Catalogos.VacaViva,
                Desde = d,
                Hasta = h
            };

            try
            {
                var dOnly = DateOnly.FromDateTime(d);
                var hOnly = DateOnly.FromDateTime(h);
                modelo.Registros = await _db.ProduccionLeche
                    .Where(p => p.VacaId == vaca.VacaId && p.Fecha >= dOnly && p.Fecha <= hOnly)
                    .OrderByDescending(p => p.Fecha)
                    .Select(p => new ProduccionItem { ProduccionId = p.ProduccionId, Fecha = p.Fecha, CantidadLitros = p.CantidadLitros })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-11: error al consultar la producción de la vaca {VacaId}", id);
                TempData["Error"] = "No fue posible completar la operación. Intenta nuevamente."; // FE-1
            }

            return View(modelo);
        }

        // GET /Produccion/Registrar?vacaId=5  (pasos 1 a 3)
        [HttpGet]
        public async Task<IActionResult> Registrar(int vacaId)
        {
            var vaca = await BuscarVacaPropiaAsync(vacaId);
            if (vaca == null)
            {
                return NotFound();
            }
            if (vaca.EstadoId != Catalogos.VacaViva)
            {
                TempData["Error"] = "Solo se puede registrar producción de vacas en estado Viva.";
                return RedirectToAction(nameof(Historial), new { id = vacaId });
            }

            return View(new RegistrarProduccionViewModel
            {
                VacaId = vaca.VacaId,
                VacaIdentificacion = vaca.Identificacion,
                Fecha = DateTime.Today
            });
        }

        // POST /Produccion/Registrar  (pasos 4 a 7, FA-2)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarProduccionViewModel modelo)
        {
            var vaca = await BuscarVacaPropiaAsync(modelo.VacaId);
            if (vaca == null)
            {
                return NotFound();
            }
            modelo.VacaIdentificacion = vaca.Identificacion;

            if (vaca.EstadoId != Catalogos.VacaViva)
            {
                ModelState.AddModelError(string.Empty, "Solo se puede registrar producción de vacas en estado Viva.");
            }
            // RN-4 / FE-2
            if (modelo.Fecha != null && modelo.Fecha.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(modelo.Fecha), "La fecha no puede ser futura.");
            }
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var fecha = DateOnly.FromDateTime(modelo.Fecha!.Value);

            try
            {
                // Paso 5 / RN-5: un solo registro por vaca por día
                var existente = await _db.ProduccionLeche.FirstOrDefaultAsync(p => p.VacaId == vaca.VacaId && p.Fecha == fecha);

                if (existente != null && !modelo.ConfirmarActualizacion)
                {
                    // FA-2: se pregunta si desea actualizarlo
                    modelo.YaExiste = true;
                    modelo.CantidadAnterior = existente.CantidadLitros;
                    return View(modelo);
                }

                if (existente != null)
                {
                    existente.CantidadLitros = modelo.CantidadLitros!.Value;
                    existente.FechaActualizacion = DateTime.Now;
                }
                else
                {
                    // Paso 6
                    _db.ProduccionLeche.Add(new ProduccionLeche
                    {
                        VacaId = vaca.VacaId,
                        Fecha = fecha,
                        CantidadLitros = modelo.CantidadLitros!.Value,
                        FechaRegistro = DateTime.Now
                    });
                }
                await _db.SaveChangesAsync();

                // Paso 7
                TempData["Exito"] = existente != null
                    ? $"Producción del {Formatos.Fecha(fecha)} actualizada a {modelo.CantidadLitros:0.##} L."
                    : $"Producción registrada: {modelo.CantidadLitros:0.##} L el {Formatos.Fecha(fecha)}.";
                return RedirectToAction(nameof(Historial), new { id = vaca.VacaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-11: error al registrar producción de la vaca {VacaId}", vaca.VacaId);
                ModelState.AddModelError(string.Empty, "No fue posible completar la operación. Intenta nuevamente."); // FE-1
                return View(modelo);
            }
        }

        // POST /Produccion/Eliminar/7  (CU-22 flujo eliminar: RN-5 sí se puede borrar definitivamente)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            var registro = await _db.ProduccionLeche
                .Include(p => p.Vaca)
                .FirstOrDefaultAsync(p => p.ProduccionId == id && p.Vaca.UsuarioId == usuarioId);
            if (registro == null)
            {
                return NotFound();
            }

            try
            {
                _db.ProduccionLeche.Remove(registro);
                await _db.SaveChangesAsync();
                TempData["Exito"] = $"Se eliminó el registro del {Formatos.Fecha(registro.Fecha)}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-22: error al eliminar el registro de producción {Id}", id);
                TempData["Error"] = "No fue posible completar la operación. Intenta nuevamente.";
            }
            return RedirectToAction(nameof(Historial), new { id = registro.VacaId });
        }

        // ============================== APOYO ==============================

        private Task<Vacas?> BuscarVacaPropiaAsync(int vacaId)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            return _db.Vacas.FirstOrDefaultAsync(v => v.VacaId == vacaId && v.UsuarioId == usuarioId);
        }

        /// <summary>Por defecto, los últimos 30 días.</summary>
        private static (DateTime desde, DateTime hasta) Rango(DateTime? desde, DateTime? hasta)
        {
            var h = (hasta ?? DateTime.Today).Date;
            var d = (desde ?? h.AddDays(-29)).Date;
            return d <= h ? (d, h) : (h, d);
        }
    }
}
