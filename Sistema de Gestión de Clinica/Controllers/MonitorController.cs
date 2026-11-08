
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    public class MonitorController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public MonitorController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // GET: Monitor/Index
        [HttpGet]
        public IActionResult Index()
        {
            return View("Sala");
        }

        // GET: Monitor/ObtenerTurnos
        [HttpGet]
        public async Task<IActionResult> ObtenerTurnos()
        {
            // Obtener citas activas en estado Agendada, En Espera o En Consulta
            var turnos = await _context.Cita
                .AsNoTracking()
                .Include(c => c.Medico)
                    .ThenInclude(m => m.Consultorio)
                .Include(c => c.EstadoCita)
                .Where(c => c.EstadoCita != null &&
                    (c.EstadoCita.NombreEstado == "Agendada" ||
                     c.EstadoCita.NombreEstado == "En Espera" ||
                     c.EstadoCita.NombreEstado == "En Consulta"))
                .OrderBy(c => c.FechaHora)
                .Take(10)
                .Select(c => new
                {
                    codigoTurno = c.CodigoTurno ?? $"T-{c.IdCita}",
                    consultorio = c.Medico != null && c.Medico.Consultorio != null
                        ? $"Consultorio {c.Medico.Consultorio.NumeroConsultorio}"
                        : "Sin consultorio",
                    hora = c.FechaHora.ToString("HH:mm"),
                    estado = c.EstadoCita.NombreEstado
                })
                .ToListAsync();

            return Json(turnos);
        }

    }
}
