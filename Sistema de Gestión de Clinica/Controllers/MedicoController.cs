using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;

namespace Sistema_Gestión_Clinica.Controllers
{
    [Authorize] // <--- ESTO BLOQUEA EL ACCESO A USUARIOS NO AUTENTICADOS
    public class MedicoController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public MedicoController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // GET: Medico/Index/1 (1 = IdMedico de prueba)
        public async Task<IActionResult> Index(int idMedico = 1)
        {
            var citasHoy = await _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.EstadoCita)
                .Where(c => c.IdMedico == idMedico && c.FechaHora.Date == DateTime.Today)
                .OrderBy(c => c.FechaHora)
                .ToListAsync();

            ViewBag.IdMedico = idMedico;
            return View(citasHoy);
        }

        // POST: Medico/LlamarSiguiente
        [HttpPost]
        public async Task<IActionResult> LlamarSiguiente(int idCita)
        {
            var cita = await _context.Cita.FindAsync(idCita);
            if (cita != null)
            {
                var estadoEnConsulta = await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == "En Consulta");
                if (estadoEnConsulta != null)
                {
                    cita.IdEstadoCita = estadoEnConsulta.IdEstadoCita;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Atender", new { idCita });
        }

        // GET: Medico/Atender/5
        public async Task<IActionResult> Atender(int idCita)
        {
            var cita = await _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.EstadoCita)
                .FirstOrDefaultAsync(c => c.IdCita == idCita);

            if (cita == null) return NotFound();

            var expediente = await _context.Expediente
                .FirstOrDefaultAsync(e => e.IdPaciente == cita.IdPaciente);

            ViewBag.Expediente = expediente;
            return View(cita);
        }

        // POST: Medico/FinalizarConsulta
        [HttpPost]
        public async Task<IActionResult> FinalizarConsulta(int idCita, string notas, string diagnostico, string indicaciones)
        {
            var cita = await _context.Cita.FindAsync(idCita);
            if (cita == null) return NotFound();

            var estadoFinalizada = await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == "Finalizada");
            if (estadoFinalizada != null)
            {
                cita.IdEstadoCita = estadoFinalizada.IdEstadoCita;
            }

            var registro = new RegistroAtencion
            {
                IdCita = idCita,
                NotasConsulta = notas,
                Diagnostico = diagnostico,
                Indicaciones = indicaciones
            };

            _context.RegistroAtencion.Add(registro);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Consulta finalizada exitosamente.";
            return RedirectToAction("Index", new { idMedico = cita.IdMedico });
        }
    }
}
