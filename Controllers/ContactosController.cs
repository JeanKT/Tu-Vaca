using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuVaca.Comun;
using TuVaca.Data;
using TuVaca.Models.Contactos;
using TuVaca.Models.Entidades;
using TuVaca.Seguridad;
using TuVaca.Servicios;

namespace TuVaca.Controllers
{
    /// <summary>CU-06: Contactar vendedor, y la bandeja "Mensajes recibidos" del vendedor (RN-4).</summary>
    [Authorize] // precondición: el comprador debe tener sesión activa
    public class ContactosController : Controller
    {
        private const string MensajeNoDisponible = "Esta publicación ya no está disponible."; // FE-3

        private readonly TuVacaDbContext _db;
        private readonly NotificacionService _notificaciones;
        private readonly CorreoService _correo;
        private readonly ILogger<ContactosController> _logger;

        public ContactosController(TuVacaDbContext db, NotificacionService notificaciones,
                                   CorreoService correo, ILogger<ContactosController> logger)
        {
            _db = db;
            _notificaciones = notificaciones;
            _correo = correo;
            _logger = logger;
        }

        // GET /Contactos/Crear?publicacionId=5  (pasos 2 y 3)
        [HttpGet]
        public async Task<IActionResult> Crear(int publicacionId)
        {
            var modelo = await CargarPublicacionAsync(publicacionId);
            if (modelo == null)
            {
                TempData["Error"] = MensajeNoDisponible; // FE-3
                return RedirectToAction("Index", "Marketplace");
            }
            return View(modelo);
        }

        // POST /Contactos/Crear  (pasos 4 a 7)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ContactarViewModel modelo)
        {
            var publicacion = await (
                from p in _db.PublicacionesVaca
                join v in _db.Vacas on p.VacaId equals v.VacaId
                where p.PublicacionVacaId == modelo.PublicacionVacaId
                select new { p.PublicacionVacaId, p.EstadoId, v.UsuarioId, v.Identificacion, v.Raza })
                .FirstOrDefaultAsync();

            // RN-1 / FE-3: solo publicaciones activas
            if (publicacion == null || publicacion.EstadoId != Catalogos.PublicacionActiva)
            {
                TempData["Error"] = MensajeNoDisponible;
                return RedirectToAction("Index", "Marketplace");
            }

            var compradorId = User.ObtenerUsuarioId()!.Value;
            if (publicacion.UsuarioId == compradorId)
            {
                TempData["Error"] = "No puedes contactarte con tu propia publicación.";
                return RedirectToAction("Detalle", "Marketplace", new { id = publicacion.PublicacionVacaId });
            }

            // Paso 5 / FE-2: mensaje vacío
            if (string.IsNullOrWhiteSpace(modelo.Mensaje))
            {
                ModelState.AddModelError(nameof(modelo.Mensaje), "Debes escribir un mensaje antes de enviarlo.");
            }
            if (!ModelState.IsValid)
            {
                var recarga = await CargarPublicacionAsync(modelo.PublicacionVacaId);
                if (recarga != null)
                {
                    recarga.Mensaje = modelo.Mensaje;
                    return View(recarga);
                }
                return RedirectToAction("Index", "Marketplace");
            }

            try
            {
                // Paso 6 / RN-3: se registra con fecha y hora, asociado a comprador y publicación (y por ella al vendedor)
                _db.Contactos.Add(new Contactos
                {
                    PublicacionVacaId = publicacion.PublicacionVacaId,
                    CompradorId = compradorId,
                    Mensaje = modelo.Mensaje.Trim(),
                    FechaContacto = DateTime.Now
                });
                await _db.SaveChangesAsync();

                // Notificación interna al vendedor (campanita)
                // (el mensaje ya quedó guardado: si la notificación falla, solo se anota en el log)
                try
                {
                    await _notificaciones.CrearAsync(
                        publicacion.UsuarioId,
                        TiposNotificacionId.MensajeDeComprador,
                        $"{User.ObtenerNombre()} te escribió por la vaca {publicacion.Identificacion} ({publicacion.Raza}).",
                        "/Contactos/Recibidos");
                }
                catch (Exception exNotificacion)
                {
                    _logger.LogWarning(exNotificacion, "CU-06: no se pudo notificar al vendedor {VendedorId}", publicacion.UsuarioId);
                }

                // Además, aviso por correo (si falla, el mensaje igual queda registrado)
                await EnviarCorreoAlVendedorAsync(publicacion.UsuarioId, publicacion.Identificacion, modelo.Mensaje.Trim());

                // Paso 7
                TempData["Exito"] = "Tu mensaje fue enviado al vendedor.";
                return RedirectToAction("Detalle", "Marketplace", new { id = publicacion.PublicacionVacaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CU-06: error al registrar el contacto de la publicación {Id}", modelo.PublicacionVacaId);
                ModelState.AddModelError(string.Empty, "No fue posible enviar tu mensaje. Intenta nuevamente."); // FE-1
                var recarga = await CargarPublicacionAsync(modelo.PublicacionVacaId);
                if (recarga == null)
                {
                    return RedirectToAction("Index", "Marketplace");
                }
                recarga.Mensaje = modelo.Mensaje;
                return View(recarga);
            }
        }

        // GET /Contactos/Recibidos: mensajes que le llegaron al vendedor (RN-4)
        [HttpGet]
        [Authorize(Roles = Catalogos.RolProductor)]
        public async Task<IActionResult> Recibidos()
        {
            var vendedorId = User.ObtenerUsuarioId()!.Value;

            var mensajes = await (
                from c in _db.Contactos
                join p in _db.PublicacionesVaca on c.PublicacionVacaId equals p.PublicacionVacaId
                join v in _db.Vacas on p.VacaId equals v.VacaId
                join u in _db.Usuarios on c.CompradorId equals u.UsuarioId
                where v.UsuarioId == vendedorId
                orderby c.FechaContacto descending
                select new ContactoRecibidoItem
                {
                    ContactoId = c.ContactoId,
                    PublicacionVacaId = p.PublicacionVacaId,
                    Publicacion = "Vaca " + v.Identificacion + " · " + v.Raza,
                    CompradorNombre = u.NombreCompleto,
                    CompradorCorreo = u.Correo,
                    CompradorTelefono = u.Telefono,
                    Mensaje = c.Mensaje,
                    FechaContacto = c.FechaContacto
                }).ToListAsync();

            return View(mensajes);
        }

        // ============================== APOYO ==============================

        private Task<ContactarViewModel?> CargarPublicacionAsync(int publicacionId) =>
            (from p in _db.PublicacionesVaca
             join v in _db.Vacas on p.VacaId equals v.VacaId
             join u in _db.Usuarios on v.UsuarioId equals u.UsuarioId
             where p.PublicacionVacaId == publicacionId && p.EstadoId == Catalogos.PublicacionActiva
             select new ContactarViewModel
             {
                 PublicacionVacaId = p.PublicacionVacaId,
                 Resumen = "Vaca " + v.Identificacion + " · " + v.Raza + " · " + v.Sexo,
                 FotoUrl = p.FotoUrl,
                 Precio = p.Precio,
                 VendedorNombre = u.NombreCompleto
             }).FirstOrDefaultAsync();

        private async Task EnviarCorreoAlVendedorAsync(int vendedorId, string identificacion, string mensaje)
        {
            try
            {
                var vendedor = await _db.Usuarios.FindAsync(vendedorId);
                var comprador = await _db.Usuarios.FindAsync(User.ObtenerUsuarioId()!.Value);
                if (vendedor == null || comprador == null)
                {
                    return;
                }

                var cuerpo =
                    $"<p>Hola {System.Net.WebUtility.HtmlEncode(vendedor.NombreCompleto)},</p>" +
                    $"<p><b>{System.Net.WebUtility.HtmlEncode(comprador.NombreCompleto)}</b> está interesado en tu vaca <b>{System.Net.WebUtility.HtmlEncode(identificacion)}</b> y te escribió:</p>" +
                    $"<blockquote>{System.Net.WebUtility.HtmlEncode(mensaje)}</blockquote>" +
                    $"<p>Datos de contacto: {System.Net.WebUtility.HtmlEncode(comprador.Correo)}" +
                    (string.IsNullOrWhiteSpace(comprador.Telefono) ? "" : $" · {System.Net.WebUtility.HtmlEncode(comprador.Telefono)}") +
                    "</p><p>— Tu Vaca</p>";

                await _correo.EnviarAsync(vendedor.Correo, "Tienes un nuevo mensaje en Tu Vaca", cuerpo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CU-06: no se pudo enviar el correo al vendedor {VendedorId}", vendedorId);
            }
        }
    }
}
