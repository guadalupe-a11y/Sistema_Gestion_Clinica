using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;

namespace Sistema_Gestión_Clinica.Controllers
{
    public class AdminController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public AdminController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // GET: Admin/Index
        public async Task<IActionResult> Index()
        {
            var citasHoy = await _context.Cita
                .Include(c => c.EstadoCita)
                .Where(c => c.FechaHora.Date == DateTime.Today)
                .ToListAsync();

            ViewBag.TotalCitasHoy = citasHoy.Count;
            ViewBag.CitasAtendidas = citasHoy.Count(c => c.EstadoCita?.NombreEstado == "Finalizada");
            ViewBag.CitasEnEspera = citasHoy.Count(c => c.EstadoCita?.NombreEstado == "En Espera");
            ViewBag.CitasCanceladas = citasHoy.Count(c => c.EstadoCita?.NombreEstado == "Cancelada");

            ViewBag.TotalMedicos = await _context.Medico.CountAsync();
            ViewBag.TotalPacientes = await _context.Paciente.CountAsync();

            return View(citasHoy);
        }
    }
}