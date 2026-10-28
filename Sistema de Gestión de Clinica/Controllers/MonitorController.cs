
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
            DateTime hoy = DateTime.Today;
            DateTime manana = hoy.AddDays(1);

            var turnos = await _context.Cita
                .AsNoTracking()
                .Where(c =>
                    c.FechaHora >= hoy &&
                    c.FechaHora < manana &&
                    c.EstadoCita != null &&
                    (
                        c.EstadoCita.NombreEstado == "Agendada" ||
                        c.EstadoCita.NombreEstado == "En Espera" ||
                        c.EstadoCita.NombreEstado == "En Consulta"
                    ))
                .OrderBy(c => c.FechaHora)
                .Select(c => new
                {
                    codigoTurno = c.CodigoTurno,

                    consultorio = c.Medico != null &&
                                  c.Medico.Consultorio != null
                        ? c.Medico.Consultorio.NombreConsultorio
                        : "Sin consultorio",

                    hora = c.FechaHora.ToString("HH:mm"),

                    estado = c.EstadoCita!.NombreEstado
                })
                .ToListAsync();

            return Json(turnos);
        }

    }
}
