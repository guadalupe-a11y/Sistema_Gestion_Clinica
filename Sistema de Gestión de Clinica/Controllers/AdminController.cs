using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;

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

        // GET: Admin/Usuarios
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _context.Usuario
                .Include(u => u.Rol)
                .ToListAsync();
            return View(usuarios);
        }

        // GET: Admin/Medicos
        public async Task<IActionResult> Medicos()
        {
            var medicos = await _context.Medico
                .Include(m => m.Especialidad)
                .Include(m => m.Consultorio)
                .Include(m => m.Usuario)
                .ToListAsync();
            return View(medicos);
        }

        // GET: Admin/Horarios
        public async Task<IActionResult> Horarios()
        {
            var horarios = await _context.HorarioMedico
                .Include(h => h.Medico)
                .ToListAsync();
            return View(horarios);
        }
    }
}