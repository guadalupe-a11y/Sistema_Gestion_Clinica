using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;

namespace Sistema_Gestión_Clinica.Controllers
{
    [Authorize]
    public class ExpedienteController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public ExpedienteController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // GET: Expediente/Ver/5
        public async Task<IActionResult> Ver(int idPaciente)
        {
            if (idPaciente <= 0) return NotFound();

            // 1. Buscar al paciente
            var paciente = await _context.Paciente
                .FirstOrDefaultAsync(p => p.IdPaciente == idPaciente);

            if (paciente == null)
            {
                TempData["Error"] = "No se encontró el paciente seleccionado.";
                return RedirectToAction("Index", "Recepcion");
            }

            // 2. Buscar expediente o crearlo si aún no existe (Autocreado)
            var expediente = await _context.Expediente
                .Include(e => e.Paciente)
                .FirstOrDefaultAsync(e => e.IdPaciente == idPaciente);

            if (expediente == null)
            {
                expediente = new Expediente
                {
                    IdPaciente = idPaciente,
                    FechaApertura = DateTime.Now
                };
                _context.Expediente.Add(expediente);
                await _context.SaveChangesAsync();

                // Recargar con la propiedad de navegación
                expediente.Paciente = paciente;
            }

            // 3. Cargar historial completo de atenciones (sin importar la fecha)
            ViewBag.HistorialAtenciones = await _context.RegistroAtencion
                .Include(r => r.Cita)
                    .ThenInclude(c => c.Medico)
                .Where(r => r.Cita.IdPaciente == idPaciente)
                .OrderByDescending(r => r.FechaAtencion)
                .ToListAsync();

            return View(expediente);
        }
    }
}