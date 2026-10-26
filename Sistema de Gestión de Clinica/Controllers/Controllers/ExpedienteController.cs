using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Helpers;
using Sistema_Gestión_Clinica.Services;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    /// <summary>
    /// Expediente clínico (acceso restringido, RN-15):
    ///  - Médico: consulta y actualiza el expediente de los pacientes que atiende.
    ///  - Paciente: consulta únicamente el suyo, en modo lectura.
    ///  - Recepción y Administración: sin acceso.
    /// </summary>
    [Authorize]
    public class ExpedienteController : Controller
    {
        private readonly SistemaDeGestionDB _context;
        private readonly ExpedienteService _expedienteService;

        public ExpedienteController(SistemaDeGestionDB context, ExpedienteService expedienteService)
        {
            _context = context;
            _expedienteService = expedienteService;
        }

        // GET: Expediente/Ver/5   (5 = IdPaciente)  -> solo Médico
        [Authorize(Roles = "Medico")]
        [HttpGet]
        public async Task<IActionResult> Ver(int id)
        {
            var idUsuario = User.ObtenerIdUsuario();
            var medico = await _context.Medico.AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdUsuario == idUsuario);
            if (medico == null)
                return StatusCode(StatusCodes.Status403Forbidden, "Su usuario no tiene un perfil de médico asociado.");

            if (!await _expedienteService.MedicoPuedeVerPacienteAsync(medico.IdMedico, id))
            {
                TempData["Error"] = "No tiene permiso para consultar el expediente de este paciente: no lo ha atendido ni lo atiende.";
                return RedirectToAction("Index", "Medico");
            }

            var vm = await _expedienteService.ObtenerAsync(id, incluirNotas: true, idMedicoActual: medico.IdMedico);
            if (vm == null) return NotFound();

            return View(vm);
        }

        // POST: Expediente/ActualizarDatos   -> solo Médico (antecedentes, alergias, grupo sanguíneo)
        [Authorize(Roles = "Medico")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarDatos(ExpedienteEdicionViewModel model)
        {
            var idUsuario = User.ObtenerIdUsuario();
            var medico = await _context.Medico.AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdUsuario == idUsuario);
            if (medico == null)
                return StatusCode(StatusCodes.Status403Forbidden, "Su usuario no tiene un perfil de médico asociado.");

            if (!await _expedienteService.MedicoPuedeVerPacienteAsync(medico.IdMedico, model.IdPaciente))
            {
                TempData["Error"] = "No tiene permiso para modificar el expediente de este paciente.";
                return RedirectToAction("Index", "Medico");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Datos no válidos.";
                return RedirectToAction(nameof(Ver), new { id = model.IdPaciente });
            }

            var (exito, mensaje) = await _expedienteService.ActualizarDatosClinicosAsync(model);
            TempData[exito ? "Mensaje" : "Error"] = mensaje;
            return RedirectToAction(nameof(Ver), new { id = model.IdPaciente });
        }

        // GET: Expediente/MiExpediente   -> solo Paciente, solo lectura, solo el suyo
        [Authorize(Roles = "Paciente")]
        [HttpGet]
        public async Task<IActionResult> MiExpediente()
        {
            var idUsuario = User.ObtenerIdUsuario();
            var paciente = await _context.Paciente.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdUsuario == idUsuario);
            if (paciente == null)
                return StatusCode(StatusCodes.Status403Forbidden, "Su usuario no tiene un perfil de paciente asociado.");

            // El paciente nunca recibe las notas internas del médico, solo fecha, médico, diagnóstico e indicaciones.
            var vm = await _expedienteService.ObtenerAsync(paciente.IdPaciente, incluirNotas: false);
            if (vm == null) return NotFound();

            return View(vm);
        }
    }
}
