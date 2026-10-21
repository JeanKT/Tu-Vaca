using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuVaca.Data;

namespace TuVaca.Controllers
{
    /// <summary>
    /// Catálogos de departamentos y municipios (DANE) para las listas en cascada.
    /// Lo usan el registro de la vaca (origen) y la publicación (ubicación).
    /// </summary>
    public class UbicacionesController : Controller
    {
        private readonly TuVacaDbContext _db;

        public UbicacionesController(TuVacaDbContext db)
        {
            _db = db;
        }

        // GET /Ubicaciones/Municipios?departamentoId=5
        [HttpGet]
        public async Task<IActionResult> Municipios(int departamentoId)
        {
            var municipios = await _db.Municipios
                .Where(m => m.DepartamentoId == departamentoId)
                .OrderBy(m => m.Nombre)
                .Select(m => new { id = m.MunicipioId, nombre = m.Nombre })
                .ToListAsync();

            return Json(municipios);
        }
    }
}
