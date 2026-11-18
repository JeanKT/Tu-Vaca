using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Entidades;
using TuVaca.Models.Sanidad;
using TuVaca.Seguridad;
using TuVaca.Servicios;

namespace TuVaca.Controllers
{
    /// <summary>
    /// CU-07: Registrar vacuna o tratamiento.
    /// CU-08: Consultar historial de vacunas.
    /// </summary>
    [Authorize(Roles = Catalogos.RolProductor)]
    public class SanidadController : Controller
    {
        private readonly TuVacaDbContext _db;
        private readonly NotificacionService _notificaciones;
        private readonly ILogger<SanidadController> _logger;

        public SanidadController(TuVacaDbContext db, NotificacionService notificaciones, ILogger<SanidadController> logger)
        {
            _db = db;
            _notificaciones = notificaciones;
            _logger = logger;
        }

        // ============================ CU-08 ============================

        // GET /Sanidad/Historial/5?TipoRegistroId=1&Desde=...&Hasta=...
        [HttpGet]
        public async Task<IActionResult> Historial(int id, int? tipoRegistroId, DateTime? desde, DateTime? hasta)
        {
            var vaca = await BuscarVacaPropiaAsync(id);
            if (vaca == null)
            {
                return NotFound(); // RN-1-CU08: solo el dueño
            }

            var modelo = new HistorialSanitarioViewModel
            {
                VacaId = vaca.VacaId,
                VacaIdentificacion = vaca.Identificacion,
                VacaRaza = vaca.Raza,
                VacaViva = vaca.EstadoId == Catalogos.VacaViva,
                TipoRegistroId = tipoRegistroId,
                Desde = desde,
                Hasta = hasta
            };

            try
            {
                // Paso 3: todos los registros de la vaca (también los anulados, RN-3)
                var consulta = _db.RegistrosSanitarios.Where(r => r.VacaId == vaca.VacaId);

                if (tipoRegistroId != null)
                {
                    consulta = consulta.Where(r => r.TipoRegistroId == tipoRegistroId);
                }
                if (desde != null)
                {
                    var d = DateOnly.FromDateTime(desde.Value);
                    consulta = consulta.Where(r => r.FechaAplicacion >= d);
                }
                if (hasta != null)
                {
                    var h = DateOnly.FromDateTime(hasta.Value);
                    consulta = consulta.Where(r => r.FechaAplicacion <= h);
                }

                // RN-2: del más reciente al más antiguo
                modelo.Registros = await consulta
                    .OrderByDescending(r => r.FechaAplicacion)
                    .ThenByDescending(r => r.RegistroId)
                    .Select(r => new RegistroSanitarioItem
                    {
                        RegistroId = r.RegistroId,
                        Tipo = r.TipoRegistro.Nombre,
                        Nombre = r.Nombre,
                        FechaAplicacion = r.FechaAplicacion,
                        Responsable = r.Responsable,
                        Observaciones = r.Observaciones,
                        ProximaDosis = r.ProximaDosis,
                        Anulado = r.EstadoId == Catalogos.RegistroSanitarioAnulado
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-08: error al consultar el historial de la vaca {VacaId}", id);
                TempData["Error"] = "No fue posible cargar el historial. Intenta nuevamente."; // FE-2
            }

            await CargarTiposAsync();
            return View(modelo);
        }

        // ============================ CU-07 ============================

        // GET /Sanidad/Registrar?vacaId=5  (pasos 1 a 3)
        [HttpGet]
        public async Task<IActionResult> Registrar(int vacaId)
        {
            var vaca = await BuscarVacaPropiaAsync(vacaId);
            if (vaca == null)
            {
                return NotFound(); // RN-1-CU07
            }
            if (vaca.EstadoId != Catalogos.VacaViva)
            {
                TempData["Error"] = "Solo se pueden registrar vacunas a vacas en estado Viva.";
                return RedirectToAction(nameof(Historial), new { id = vacaId });
            }

            await CargarTiposAsync();
            return View(new RegistrarSanitarioViewModel
            {
                VacaId = vaca.VacaId,
                VacaIdentificacion = vaca.Identificacion,
                FechaAplicacion = DateTime.Today
            });
        }

        // POST /Sanidad/Registrar  (pasos 4 a 7)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarSanitarioViewModel modelo)
        {
            var vaca = await BuscarVacaPropiaAsync(modelo.VacaId);
            if (vaca == null)
            {
                return NotFound();
            }
            modelo.VacaIdentificacion = vaca.Identificacion;

            if (vaca.EstadoId != Catalogos.VacaViva)
            {
                ModelState.AddModelError(string.Empty, "Solo se pueden registrar vacunas a vacas en estado Viva.");
            }

            // RN-3 / FE-2: la fecha de aplicación no puede ser futura
            if (modelo.FechaAplicacion != null && modelo.FechaAplicacion.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(modelo.FechaAplicacion), "La fecha de aplicación no puede ser futura.");
            }
            // La próxima dosis debe ser posterior a la aplicación
            if (modelo.ProximaDosis != null && modelo.FechaAplicacion != null
                && modelo.ProximaDosis.Value.Date <= modelo.FechaAplicacion.Value.Date)
            {
                ModelState.AddModelError(nameof(modelo.ProximaDosis), "La próxima dosis debe ser posterior a la fecha de aplicación.");
            }
            if (modelo.TipoRegistroId != null
                && !await _db.TiposRegistroSanitario.AnyAsync(t => t.TipoRegistroId == modelo.TipoRegistroId))
            {
                ModelState.AddModelError(nameof(modelo.TipoRegistroId), "Selecciona un tipo válido.");
            }

            if (!ModelState.IsValid)
            {
                await CargarTiposAsync();
                return View(modelo);
            }

            try
            {
                // Paso 6: se guarda asociado al historial de la vaca
                var registro = new RegistrosSanitarios
                {
                    VacaId = vaca.VacaId,
                    TipoRegistroId = modelo.TipoRegistroId!.Value,
                    Nombre = modelo.Nombre.Trim(),
                    FechaAplicacion = DateOnly.FromDateTime(modelo.FechaAplicacion!.Value),
                    Responsable = modelo.Responsable.Trim(),
                    Observaciones = string.IsNullOrWhiteSpace(modelo.Observaciones) ? null : modelo.Observaciones.Trim(),
                    ProximaDosis = modelo.ProximaDosis == null ? null : DateOnly.FromDateTime(modelo.ProximaDosis.Value),
                    EstadoId = Catalogos.RegistroSanitarioActivo,
                    FechaRegistro = DateTime.Now
                };
                _db.RegistrosSanitarios.Add(registro);
                await _db.SaveChangesAsync();

                // FA-2: recordatorio en la campanita el día de la próxima dosis
                if (modelo.ProximaDosis != null)
                {
                    await CrearRecordatorioAsync(vaca, registro.Nombre, modelo.ProximaDosis.Value);
                }

                // Paso 7
                TempData["Exito"] = modelo.ProximaDosis != null
                    ? $"Registro guardado. Te recordaremos la próxima dosis el {Formatos.Fecha(modelo.ProximaDosis.Value)}."
                    : "Registro guardado en el historial sanitario.";
                return RedirectToAction(nameof(Historial), new { id = vaca.VacaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-07: error al registrar vacuna/tratamiento de la vaca {VacaId}", vaca.VacaId);
                ModelState.AddModelError(string.Empty, "No fue posible registrar la vacuna/tratamiento. Intenta nuevamente."); // FE-1
                await CargarTiposAsync();
                return View(modelo);
            }
        }

        // ============================== APOYO ==============================

        // El registro ya quedó guardado: si el recordatorio falla, solo se anota en el log
        private async Task CrearRecordatorioAsync(Vacas vaca, string nombre, DateTime proximaDosis)
        {
            try
            {
                await _notificaciones.CrearAsync(
                    vaca.UsuarioId,
                    TiposNotificacionId.RecordatorioDosis,
                    $"Hoy toca la próxima dosis de {nombre} para la vaca {vaca.Identificacion}.",
                    $"/Sanidad/Historial/{vaca.VacaId}",
                    proximaDosis.Date.AddHours(6));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CU-07: no se pudo crear el recordatorio de la vaca {VacaId}", vaca.VacaId);
            }
        }

        private Task<Vacas?> BuscarVacaPropiaAsync(int vacaId)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            return _db.Vacas.FirstOrDefaultAsync(v => v.VacaId == vacaId && v.UsuarioId == usuarioId);
        }

        private async Task CargarTiposAsync()
        {
            ViewBag.Tipos = await _db.TiposRegistroSanitario
                .OrderBy(t => t.Nombre)
                .Select(t => new SelectListItem { Value = t.TipoRegistroId.ToString(), Text = t.Nombre })
                .ToListAsync();
        }
    }
}
