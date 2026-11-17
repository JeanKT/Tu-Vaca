using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Administracion;
using TuVaca.Models.Entidades;
using TuVaca.Seguridad;

namespace TuVaca.Controllers
{
    /// <summary>
    /// CU-10: Gestionar usuarios (solo Administrador, RN-1).
    /// Acciones: editar datos, cambiar rol, activar y desactivar. Todo queda en AuditoriaUsuarios (RN-5).
    /// </summary>
    [Authorize(Roles = Catalogos.RolAdministrador)]
    public class UsuariosController : Controller
    {
        // Valores permitidos por el CHECK de AuditoriaUsuarios.Accion
        private const string AccionEditar = "EditarDatos";
        private const string AccionRol = "CambiarRol";
        private const string AccionActivar = "Activar";
        private const string AccionDesactivar = "Desactivar";

        private readonly TuVacaDbContext _db;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(TuVacaDbContext db, ILogger<UsuariosController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /Usuarios?Buscar=...&RolId=...&EstadoId=...  (pasos 1 a 3, FA-2)
        [HttpGet]
        public async Task<IActionResult> Index(string? buscar, int? rolId, int? estadoId)
        {
            var consulta = _db.Usuarios.AsQueryable();
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(u => u.NombreCompleto.Contains(texto) || u.Correo.Contains(texto) || u.NumeroDocumento.Contains(texto));
            }
            if (rolId != null)
            {
                consulta = consulta.Where(u => u.RolId == rolId);
            }
            if (estadoId != null)
            {
                consulta = consulta.Where(u => u.EstadoId == estadoId);
            }

            var modelo = new UsuariosIndexViewModel
            {
                Buscar = buscar,
                RolId = rolId,
                EstadoId = estadoId,
                Usuarios = await consulta
                    .OrderBy(u => u.NombreCompleto)
                    .Select(u => new UsuarioListadoItem
                    {
                        UsuarioId = u.UsuarioId,
                        NombreCompleto = u.NombreCompleto,
                        Correo = u.Correo,
                        NumeroDocumento = u.NumeroDocumento,
                        RolId = u.RolId,
                        Rol = u.Rol.NombreRol,
                        EstadoId = u.EstadoId,
                        Estado = u.EstadoId == Catalogos.UsuarioActivo ? "Activo" : "Inactivo",
                        FechaRegistro = u.FechaRegistro
                    })
                    .ToListAsync()
            };

            await CargarRolesAsync();
            return View(modelo);
        }

        // GET /Usuarios/Editar/5
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.Auditoria = await AuditoriaAsync(id);
            return View(new EditarUsuarioViewModel
            {
                UsuarioId = usuario.UsuarioId,
                Correo = usuario.Correo,
                NumeroDocumento = usuario.NumeroDocumento,
                NombreCompleto = usuario.NombreCompleto,
                Telefono = usuario.Telefono,
                Direccion = usuario.Direccion
            });
        }

        // POST /Usuarios/Editar  (acción "Editar datos", pasos 6 a 8)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EditarUsuarioViewModel modelo)
        {
            var usuario = await _db.Usuarios.FindAsync(modelo.UsuarioId);
            if (usuario == null)
            {
                return NotFound();
            }
            modelo.Correo = usuario.Correo;
            modelo.NumeroDocumento = usuario.NumeroDocumento;

            if (!ModelState.IsValid)
            {
                ViewBag.Auditoria = await AuditoriaAsync(usuario.UsuarioId);
                return View(modelo);
            }

            try
            {
                var cambios = new List<string>();
                if (usuario.NombreCompleto != modelo.NombreCompleto.Trim()) cambios.Add("nombre");
                if (usuario.Telefono != modelo.Telefono?.Trim()) cambios.Add("teléfono");
                if (usuario.Direccion != modelo.Direccion?.Trim()) cambios.Add("dirección");

                usuario.NombreCompleto = modelo.NombreCompleto.Trim();
                usuario.Telefono = string.IsNullOrWhiteSpace(modelo.Telefono) ? null : modelo.Telefono.Trim();
                usuario.Direccion = string.IsNullOrWhiteSpace(modelo.Direccion) ? null : modelo.Direccion.Trim();
                usuario.FechaActualizacion = DateTime.Now;
                Auditar(usuario.UsuarioId, AccionEditar,
                    cambios.Count == 0 ? "Sin cambios" : "Cambió: " + string.Join(", ", cambios));
                await _db.SaveChangesAsync();

                TempData["Exito"] = $"Se actualizaron los datos de {usuario.NombreCompleto}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-10: error al editar el usuario {Id}", modelo.UsuarioId);
                ModelState.AddModelError(string.Empty, "No fue posible completar la acción. Intenta nuevamente."); // FE-1
                ViewBag.Auditoria = await AuditoriaAsync(usuario.UsuarioId);
                return View(modelo);
            }
        }

        // POST /Usuarios/CambiarRol/5  (RN-3: solo un administrador asigna el rol Administrador)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(int id, int rolId)
        {
            var usuario = await _db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.UsuarioId == id);
            var rolNuevo = await _db.Roles.FindAsync(rolId);
            if (usuario == null || rolNuevo == null)
            {
                return NotFound();
            }
            if (usuario.UsuarioId == User.ObtenerUsuarioId())
            {
                TempData["Error"] = "No puedes cambiar tu propio rol.";
                return RedirectToAction(nameof(Index));
            }
            if (usuario.RolId == rolId)
            {
                TempData["Error"] = "El usuario ya tiene ese rol.";
                return RedirectToAction(nameof(Index));
            }

            return await GuardarAsync(() =>
            {
                Auditar(usuario.UsuarioId, AccionRol, $"{usuario.Rol.NombreRol} → {rolNuevo.NombreRol}");
                usuario.RolId = rolId;
                usuario.FechaActualizacion = DateTime.Now;
                CerrarSesiones(usuario.UsuarioId); // debe volver a entrar con el rol nuevo
                return Task.CompletedTask;
            }, $"{usuario.NombreCompleto} ahora es {rolNuevo.NombreRol}.");
        }

        // POST /Usuarios/Desactivar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            // RN-2 / FE-2: un administrador no se desactiva a sí mismo
            if (id == User.ObtenerUsuarioId())
            {
                TempData["Error"] = "No puedes desactivar tu propia cuenta de Administrador.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }
            if (usuario.EstadoId == Catalogos.UsuarioInactivo)
            {
                TempData["Error"] = "El usuario ya está inactivo.";
                return RedirectToAction(nameof(Index));
            }

            return await GuardarAsync(async () =>
            {
                usuario.EstadoId = Catalogos.UsuarioInactivo;
                usuario.FechaActualizacion = DateTime.Now;

                // RN-4: sus publicaciones activas pasan a "Pausada"
                var publicaciones = await (
                    from p in _db.PublicacionesVaca
                    join v in _db.Vacas on p.VacaId equals v.VacaId
                    where v.UsuarioId == id && p.EstadoId == Catalogos.PublicacionActiva
                    select p).ToListAsync();
                foreach (var p in publicaciones)
                {
                    p.EstadoId = Catalogos.PublicacionPausada;
                    p.FechaActualizacion = DateTime.Now;
                }

                CerrarSesiones(id); // se le cierra la sesión de inmediato
                Auditar(id, AccionDesactivar, publicaciones.Count > 0 ? $"{publicaciones.Count} publicación(es) pausada(s)" : null);
            }, $"{usuario.NombreCompleto} quedó inactivo.");
        }

        // POST /Usuarios/Activar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }
            if (usuario.EstadoId == Catalogos.UsuarioActivo)
            {
                TempData["Error"] = "El usuario ya está activo.";
                return RedirectToAction(nameof(Index));
            }

            return await GuardarAsync(() =>
            {
                usuario.EstadoId = Catalogos.UsuarioActivo;
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
                usuario.FechaActualizacion = DateTime.Now;
                Auditar(id, AccionActivar, null);
                return Task.CompletedTask;
            }, $"{usuario.NombreCompleto} quedó activo.");
        }

        // ============================== APOYO ==============================

        /// <summary>Pasos 7 y 8: aplica el cambio y muestra el mensaje (o FE-1 si falla).</summary>
        private async Task<IActionResult> GuardarAsync(Func<Task> cambio, string mensajeExito)
        {
            try
            {
                await cambio();
                await _db.SaveChangesAsync();
                TempData["Exito"] = mensajeExito;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-10: error al aplicar una acción sobre un usuario");
                TempData["Error"] = "No fue posible completar la acción. Intenta nuevamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>RN-5: fecha, hora y administrador responsable.</summary>
        private void Auditar(int usuarioAfectadoId, string accion, string? detalle)
        {
            _db.AuditoriaUsuarios.Add(new AuditoriaUsuarios
            {
                AdministradorId = User.ObtenerUsuarioId()!.Value,
                UsuarioAfectadoId = usuarioAfectadoId,
                Accion = accion,
                Detalle = detalle,
                FechaAccion = DateTime.Now
            });
        }

        private void CerrarSesiones(int usuarioId)
        {
            foreach (var sesion in _db.Sesiones.Where(s => s.UsuarioId == usuarioId && s.Activa))
            {
                sesion.Activa = false;
            }
        }

        private Task<List<AuditoriaItem>> AuditoriaAsync(int usuarioId) =>
            _db.AuditoriaUsuarios
                .Where(a => a.UsuarioAfectadoId == usuarioId)
                .OrderByDescending(a => a.FechaAccion)
                .Take(20)
                .Select(a => new AuditoriaItem
                {
                    Fecha = a.FechaAccion,
                    Administrador = a.Administrador.NombreCompleto,
                    Accion = a.Accion,
                    Detalle = a.Detalle
                })
                .ToListAsync();

        private async Task CargarRolesAsync()
        {
            ViewBag.Roles = await _db.Roles
                .OrderBy(r => r.RolId)
                .Select(r => new SelectListItem { Value = r.RolId.ToString(), Text = r.NombreRol })
                .ToListAsync();
        }
    }
}
