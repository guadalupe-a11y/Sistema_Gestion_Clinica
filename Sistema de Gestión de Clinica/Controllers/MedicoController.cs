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
    [Authorize(Roles = "Medico,Administrador")] // Permite el acceso únicamente a Médicos y Administradores
    public class MedicoController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public MedicoController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // Método auxiliar para obtener el IdMedico del usuario autenticado a partir de sus Claims
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

            // Si es un médico logueado, filtramos por su ID
            if (idMedico.HasValue)
            {
                query = query.Where(c => c.IdMedico == idMedico.Value);
            }

            // 1. Obtener las citas programadas para hoy
            var citas = await query
                .Where(c => c.FechaHora.Date == DateTime.Today)
                .OrderBy(c => c.FechaHora)
                .ToListAsync();

            // 2. Si hay menos de 5 citas hoy, traer las citas más recientes para visualización
            if (citas.Count < 5)
            {
                citas = await query
                    .OrderByDescending(c => c.FechaHora)
                    .Take(15)
                    .ToListAsync();

                // Reordenar por hora ascendente para mantener orden cronológico en la vista
                citas = citas.OrderBy(c => c.FechaHora).ToList();
            }

            ViewBag.IdMedico = idMedico ?? 0;
            return View(citas);
        }

        // POST: Medico/LlamarSiguiente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LlamarSiguiente(int idCita)
        {
            var cita = await _context.Cita.FindAsync(idCita);
            if (cita != null)
            {
                var estadoEnConsulta = await _context.EstadoCita
                    .FirstOrDefaultAsync(e => e.NombreEstado == "En Consulta");

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

            // Obtener el expediente del paciente vinculado
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

            // 1. Cambiar el estado de la cita a "Finalizada"
            var estadoFinalizada = await _context.EstadoCita
                .FirstOrDefaultAsync(e => e.NombreEstado == "Finalizada");

            if (estadoFinalizada != null)
            {
                cita.IdEstadoCita = estadoFinalizada.IdEstadoCita;
            }


            // 2. Buscar expediente del paciente para asociar la atención
            var expediente = await _context.Expediente
                .FirstOrDefaultAsync(e => e.IdPaciente == cita.IdPaciente);

            // Verificar que el paciente tenga un expediente válido
            if (expediente == null)
            {
                TempData["Error"] =
                    "No se puede finalizar la consulta porque el paciente no tiene un expediente registrado.";

                return RedirectToAction(nameof(Atender), new { idCita });
            }

            // 3. Crear el registro de la consulta médica
            var registro = new RegistroAtencion
            {
                IdCita = idCita,
                IdExpediente = expediente.IdExpediente,
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