using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;

namespace Sistema_Gestión_Clinica.Controllers
{
    public class RecepcionController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public RecepcionController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // GET: Recepcion/Index
        public async Task<IActionResult> Index()
        {
            var citas = await _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                .Include(c => c.EstadoCita)
                .Where(c => c.FechaHora.Date == DateTime.Today)
                .OrderBy(c => c.FechaHora)
                .ToListAsync();

            return View(citas);
        }

        // POST: Recepcion/MarcarLlegada
        [HttpPost]
        public async Task<IActionResult> MarcarLlegada(int idCita)
        {
            var cita = await _context.Cita.FindAsync(idCita);
            if (cita != null)
            {
                var estadoEnEspera = await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == "En Espera");
                if (estadoEnEspera != null)
                {
                    cita.IdEstadoCita = estadoEnEspera.IdEstadoCita;
                    cita.HoraCheckIn = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }
    }
}
