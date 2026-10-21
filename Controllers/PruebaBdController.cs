using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuVaca.Data;

namespace TuVaca.Controllers
{
    public class PruebaBdController : Controller
    {
        private readonly TuVacaDbContext _db;

        public PruebaBdController(TuVacaDbContext db)
        {
            _db = db;
        }

        // Abre /PruebaBd en el navegador para comprobar la conexión
        public async Task<IActionResult> Index()
        {
            return Json(new
            {
                Departamentos = await _db.Departamentos.CountAsync(),
                Municipios = await _db.Municipios.CountAsync(),
                Estados = await _db.Estados.CountAsync()
            });
        }
    }
}

