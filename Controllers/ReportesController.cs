using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Reportes;
using TuVaca.Seguridad;
using TuVaca.Servicios;

namespace TuVaca.Controllers
{
    /// <summary>
    /// Reportes en PDF y Excel.
    /// - Producción de leche del hato (PDF y Excel): usa dbo.sp_ReporteProduccion.
    /// - Historial sanitario de una vaca (PDF): usa dbo.sp_HistorialSanitario.
    /// - Usuarios de la plataforma (Excel, solo Administrador).
    /// </summary>
    [Authorize]
    public class ReportesController : Controller
    {
        private const string TipoExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly TuVacaDbContext _db;
        private readonly ReporteService _reportes;
        private readonly ILogger<ReportesController> _logger;

        public ReportesController(TuVacaDbContext db, ReporteService reportes, ILogger<ReportesController> logger)
        {
            _db = db;
            _reportes = reportes;
            _logger = logger;
        }

        // GET /Reportes/ProduccionExcel?desde=2026-10-01&hasta=2026-11-15
        [Authorize(Roles = Catalogos.RolProductor)]
        public async Task<IActionResult> ProduccionExcel(DateTime? desde, DateTime? hasta)
        {
            var (d, h) = Rango(desde, hasta);
            try
            {
                var filas = await ProduccionAsync(d, h);
                var archivo = _reportes.ProduccionExcel(filas, d, h, User.ObtenerNombre());
                return File(archivo, TipoExcel, $"produccion_{d:yyyyMMdd}_{h:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                return Fallo(ex, "Produccion");
            }
        }

        // GET /Reportes/ProduccionPdf?desde=...&hasta=...
        [Authorize(Roles = Catalogos.RolProductor)]
        public async Task<IActionResult> ProduccionPdf(DateTime? desde, DateTime? hasta)
        {
            var (d, h) = Rango(desde, hasta);
            try
            {
                var filas = await ProduccionAsync(d, h);
                var archivo = _reportes.ProduccionPdf(filas, d, h, User.ObtenerNombre());
                return File(archivo, "application/pdf", $"produccion_{d:yyyyMMdd}_{h:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return Fallo(ex, "Produccion");
            }
        }

        // GET /Reportes/SanidadPdf/5
        [Authorize(Roles = Catalogos.RolProductor)]
        public async Task<IActionResult> SanidadPdf(int id)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            var vaca = await _db.Vacas.FirstOrDefaultAsync(v => v.VacaId == id && v.UsuarioId == usuarioId);
            if (vaca == null)
            {
                return NotFound();
            }

            try
            {
                var filas = await _db.Database
                    .SqlQuery<HistorialSanitarioFila>($"EXEC dbo.sp_HistorialSanitario @VacaID = {id}, @UsuarioID = {usuarioId}")
                    .ToListAsync();
                var archivo = _reportes.SanidadPdf(filas, vaca.Identificacion, User.ObtenerNombre());
                return File(archivo, "application/pdf", $"historial_sanitario_{vaca.Identificacion}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reporte: error al generar el historial sanitario de la vaca {Id}", id);
                TempData["Error"] = "No fue posible generar el reporte. Intenta nuevamente.";
                return RedirectToAction("Historial", "Sanidad", new { id });
            }
        }

        // GET /Reportes/UsuariosExcel
        [Authorize(Roles = Catalogos.RolAdministrador)]
        public async Task<IActionResult> UsuariosExcel()
        {
            try
            {
                var filas = await _db.Usuarios
                    .OrderBy(u => u.NombreCompleto)
                    .Select(u => new UsuarioReporteFila
                    {
                        NombreCompleto = u.NombreCompleto,
                        NumeroDocumento = u.NumeroDocumento,
                        Correo = u.Correo,
                        Telefono = u.Telefono,
                        Rol = u.Rol.NombreRol,
                        Estado = u.EstadoId == Catalogos.UsuarioActivo ? "Activo" : "Inactivo",
                        FechaRegistro = u.FechaRegistro
                    })
                    .ToListAsync();
                return File(_reportes.UsuariosExcel(filas), TipoExcel, $"usuarios_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                return Fallo(ex, "Usuarios");
            }
        }

        // ============================== APOYO ==============================

        /// <summary>Llama al procedimiento almacenado dbo.sp_ReporteProduccion.</summary>
        private Task<List<ReporteProduccionFila>> ProduccionAsync(DateTime desde, DateTime hasta)
        {
            var usuarioId = User.ObtenerUsuarioId()!.Value;
            var d = DateOnly.FromDateTime(desde);
            var h = DateOnly.FromDateTime(hasta);
            return _db.Database
                .SqlQuery<ReporteProduccionFila>($"EXEC dbo.sp_ReporteProduccion @UsuarioID = {usuarioId}, @Desde = {d}, @Hasta = {h}")
                .ToListAsync();
        }

        private IActionResult Fallo(Exception ex, string controladorDeRegreso)
        {
            _logger.LogError(ex, "Reporte: error al generar el archivo");
            TempData["Error"] = "No fue posible generar el reporte. Intenta nuevamente.";
            return RedirectToAction("Index", controladorDeRegreso);
        }

        private static (DateTime desde, DateTime hasta) Rango(DateTime? desde, DateTime? hasta)
        {
            var h = (hasta ?? DateTime.Today).Date;
            var d = (desde ?? h.AddDays(-29)).Date;
            return d <= h ? (d, h) : (h, d);
        }
    }
}
