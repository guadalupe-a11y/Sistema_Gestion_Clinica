using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;
using Sistema_Gestión_Clinica.Services;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    public class PacienteController : Controller
    {
        private readonly SistemaDeGestionDB _context;
        private readonly ReasignacionListaEsperaService _reasignacionService;
        private readonly FilaVirtualService _filaVirtualService;

        public PacienteController(
            SistemaDeGestionDB context,
            ReasignacionListaEsperaService reasignacionService,
            FilaVirtualService filaVirtualService)
        {
            _context = context;
            _reasignacionService = reasignacionService;
            _filaVirtualService = filaVirtualService;
        }

        // GET: Paciente/Index/1 (1 = IdPaciente de prueba)
        public async Task<IActionResult> Index(int idPaciente = 1)
        {
            var citas = await _context.Cita
                .Include(c => c.Medico)
                .Include(c => c.EstadoCita)
                .Where(c => c.IdPaciente == idPaciente)
                .OrderByDescending(c => c.FechaHora)
                .ToListAsync();

            ViewBag.IdPaciente = idPaciente;
            return View(citas);
        }

        // GET: Paciente/Agendar
        [HttpGet]
        public async Task<IActionResult> Agendar()
        {
            ViewBag.Medicos = await _context.Medico
                .Include(m => m.Especialidad)
                .ToListAsync();

            return View();
        }

        // POST: Paciente/Agendar
        [HttpPost]
        public async Task<IActionResult> Agendar(AgendarCitaViewModel model, int idPaciente = 1)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Medicos = await _context.Medico.Include(m => m.Especialidad).ToListAsync();
                return View(model);
            }

            var estadoAgendada = await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == "Agendada");

            string codigo = $"T-{new Random().Next(100, 999)}";

            var nuevaCita = new Cita
            {
                CodigoTurno = codigo,
                IdPaciente = idPaciente,
                IdMedico = model.IdMedico,
                IdEstadoCita = estadoAgendada?.IdEstadoCita ?? 1,
                FechaHora = model.FechaHora,
                MotivoConsulta = model.MotivoConsulta
            };

            _context.Cita.Add(nuevaCita);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Cita agendada con éxito. Su código de turno es: {codigo}";
            return RedirectToAction("Index");
        }

        // POST: Paciente/CancelarCita
        [HttpPost]
        public async Task<IActionResult> CancelarCita(int idCita)
        {
            var cita = await _context.Cita.FindAsync(idCita);
            if (cita != null)
            {
                var estadoCancelada = await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == "Cancelada");
                if (estadoCancelada != null)
                {
                    cita.IdEstadoCita = estadoCancelada.IdEstadoCita;
                    await _context.SaveChangesAsync();

                    // Reasignación automática si hay alguien en lista de espera
                    await _reasignacionService.ReasignarCupoLiberadoAsync(cita.IdMedico, cita.FechaHora);
                }
            }

            TempData["Mensaje"] = "Cita cancelada correctamente.";
            return RedirectToAction("Index");
        }

        // GET: Paciente/FilaVirtual/5
        public async Task<IActionResult> FilaVirtual(int id)
        {
            var model = await _filaVirtualService.ObtenerEstadoFilaAsync(id);
            if (model == null) return NotFound();

            return View(model);
        }


        // GET: Paciente/ObtenerFilaVirtual/5
        [HttpGet]
        public async Task<IActionResult> ObtenerFilaVirtual(int id)
        {
            var modelo =
                await _filaVirtualService.ObtenerEstadoFilaAsync(id);

            if (modelo == null)
            {
                return NotFound(new
                {
                    mensaje = "No se encontró la cita."
                });
            }

            return Json(new
            {
                pacientesAdelante = modelo.PacientesAdelante,
                tiempoEstimadoMinutos =
                    modelo.TiempoEstimadoMinutos,
                estadoActual = modelo.EstadoActual
            });
        }

    }
}