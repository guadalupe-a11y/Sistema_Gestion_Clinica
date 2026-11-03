using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;

namespace Sistema_Gestión_Clinica.Controllers
{
    [Authorize] // Permite el acceso a los usuarios autorizados
    public class MedicoController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public MedicoController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // Método auxiliar para obtener el IdMedico del usuario autenticado
        private int? ObtenerIdMedicoLogueado()
        {
            var claim = User.FindFirst("IdMedico")?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }

        // GET: Medico/Index
        public async Task<IActionResult> Index()
        {
            int? idMedico = ObtenerIdMedicoLogueado();

            var query = _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                .Include(c => c.EstadoCita)
                .AsQueryable();

            // Si existe la cookie/claim del médico, filtramos por él.
            if (idMedico.HasValue)
            {
                query = query.Where(c => c.IdMedico == idMedico.Value);
            }

            // Cargar las citas agendadas para el día de hoy
            var citasHoy = await query
                .Where(c => c.FechaHora.Date == DateTime.Today)
                .OrderBy(c => c.FechaHora)
                .ToListAsync();

            // Si para hoy no hay citas registradas, se obtienen las últimas citas registradas en el sistema para evitar una pantalla vacía en la exposición
            if (!citasHoy.Any())
            {
                citasHoy = await query
                    .OrderByDescending(c => c.FechaHora)
                    .Take(10)
                    .ToListAsync();
            }

            ViewBag.IdMedico = idMedico ?? 0;
            return View(citasHoy);
        }

        // POST: Medico/LlamarSiguiente
        [HttpPost]
        [ValidateAntiForgeryToken]
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
            return RedirectToAction(nameof(Atender), new { idCita });
        }

        // GET: Medico/Atender/5
        public async Task<IActionResult> Atender(int idCita)
        {
            var cita = await _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                .Include(c => c.EstadoCita)
                .FirstOrDefaultAsync(c => c.IdCita == idCita);

            if (cita == null) return NotFound();

            var expediente = await _context.Expediente
                .FirstOrDefaultAsync(e => e.IdPaciente == cita.IdPaciente);

            ViewBag.Expediente = expediente;
            return View(cita);
        }

        // POST: Medico/FinalizarConsulta (ACTUALIZADO CON LOS NUEVOS CAMPOS)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarConsulta(
            int idCita,
            string notas,
            string diagnostico,
            string indicaciones,
            string sintomas,
            string recetaMedica,
            string estudiosSolicitados,
            DateTime? proximaCitaRecomendada)
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
                Indicaciones = indicaciones,
                Sintomas = sintomas,
                RecetaMedica = recetaMedica,
                EstudiosSolicitados = estudiosSolicitados,
                ProximaCitaRecomendada = proximaCitaRecomendada,
                FechaRegistro = DateTime.Now
            };

            _context.RegistroAtencion.Add(registro);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Consulta finalizada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}