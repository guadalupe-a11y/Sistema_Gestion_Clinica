using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    [Authorize(Roles = "Administrador")]
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
            ViewBag.TotalMedicos = await _context.Medico.CountAsync();
            ViewBag.TotalPacientes = await _context.Paciente.CountAsync();
            ViewBag.CitasHoy = await _context.Cita.CountAsync(c => c.FechaHora.Date == DateTime.Today);

            // CORRECCIÓN CS0019 (Línea 28): Conteo global de la lista de espera
            ViewBag.EnListaEspera = await _context.ListaEspera.CountAsync();

            return View();
        }


// GET: Admin/Medicos
public async Task<IActionResult> Medicos()
    {
        var model = new MedicosViewModel();

        // 1. Cargar Médicos
        model.Medicos = await _context.Medico
            .Include(m => m.Especialidad)
            .Include(m => m.Consultorio)
            .ToListAsync() ?? new List<Medico>();

        // 2. Cargar Especialidades a List<SelectListItem>
        var listEspecialidades = await _context.Especialidad.ToListAsync();
        if (listEspecialidades != null && listEspecialidades.Any())
        {
            model.Especialidades = listEspecialidades.Select(e => new SelectListItem
            {
                Value = e.IdEspecialidad.ToString(),
                Text = e.NombreEspecialidad
            }).ToList();
        }

        // 3. Cargar Consultorios a List<SelectListItem>
        var listConsultorios = await _context.Consultorio.ToListAsync();
        if (listConsultorios != null && listConsultorios.Any())
        {
            model.Consultorios = listConsultorios.Select(c => new SelectListItem
            {
                Value = c.IdConsultorio.ToString(),
                Text = $"Consultorio {c.NumeroConsultorio}"
            }).ToList();
        }

        return View(model);
    }

    // POST: Admin/RegistrarMedico
    [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarMedico(string nombre, string apellido, int idEspecialidad, int idConsultorio, int duracionConsulta)
        {
            if (!ModelState.IsValid)
            {
                // En caso de error, recargar ViewBags antes de retornar la vista
                ViewBag.Especialidades = new SelectList(await _context.Especialidad.ToListAsync(), "IdEspecialidad", "NombreEspecialidad");
                ViewBag.Consultorios = new SelectList(await _context.Consultorio.ToListAsync(), "IdConsultorio", "NumeroConsultorio");

                var listaMedicos = await _context.Medico.Include(m => m.Especialidad).Include(m => m.Consultorio).ToListAsync();
                return View("Medicos", listaMedicos);
            }

            var rolMedico = await _context.Rol.FirstOrDefaultAsync(r => r.NombreRol == "Medico");
            var usuario = new Usuario
            {
                Correo = $"{nombre.ToLower().Replace(" ", "")}@clinica.com",
                PasswordHash = "123456",
                IdRol = rolMedico?.IdRol ?? 3,
                Activo = true
            };
            _context.Usuario.Add(usuario);
            await _context.SaveChangesAsync();

            var medico = new Medico
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = nombre,
                Apellido = apellido,
                IdEspecialidad = idEspecialidad,
                IdConsultorio = idConsultorio,
                DuracionConsultaMinutos = duracionConsulta
            };
            _context.Medico.Add(medico);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Médico registrado exitosamente.";
            return RedirectToAction(nameof(Medicos));
        }

        // GET: Admin/Horarios
        public async Task<IActionResult> Horarios()
        {
            var horarios = await _context.HorarioMedico
                .Include(h => h.Medico)
                .ToListAsync();

            var listaMedicos = await _context.Medico
                .Select(m => new { m.IdMedico, NombreCompleto = "Dr/a. " + m.Nombre + " " + m.Apellido })
                .ToListAsync();

            ViewBag.Medicos = new SelectList(listaMedicos, "IdMedico", "NombreCompleto");

            return View(horarios);
        }

        // GET: Admin/Usuarios
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _context.Usuario
                .Include(u => u.Rol)
                .ToListAsync();

            ViewBag.Roles = new SelectList(await _context.Rol.ToListAsync(), "IdRol", "NombreRol");

            return View(usuarios);
        }

        // POST: Admin/AsignarHorario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarHorario(int idMedico, int diaSemana, TimeSpan horaInicio, TimeSpan horaFin)
        {
            var horario = new HorarioMedico
            {
                IdMedico = idMedico,
                DiaSemana = diaSemana, // Ahora ambos son int
                HoraInicio = horaInicio,
                HoraFin = horaFin
            };
            _context.HorarioMedico.Add(horario);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Horario asignado exitosamente.";
            return RedirectToAction(nameof(Horarios));
        }

        // POST: Admin/CrearUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(string correo, string contrasena, int idRol)
        {
            if (await _context.Usuario.AnyAsync(u => u.Correo == correo))
            {
                TempData["Error"] = "El correo electrónico ya existe.";
                return RedirectToAction(nameof(Usuarios));
            }

            var usuario = new Usuario
            {
                Correo = correo,
                PasswordHash = contrasena,
                IdRol = idRol,
                Activo = true
            };
            _context.Usuario.Add(usuario);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Usuario creado exitosamente.";
            return RedirectToAction(nameof(Usuarios));
        }
    }
}