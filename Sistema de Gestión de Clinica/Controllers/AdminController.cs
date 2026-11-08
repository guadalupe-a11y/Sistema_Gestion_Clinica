using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            {
                TempData["Error"] = "Todos los campos son obligatorios.";
                return RedirectToAction(nameof(Medicos));
            }

            var rolMedico = await _context.Rol.FirstOrDefaultAsync(r => r.NombreRol == "Medico");
            var usuario = new Usuario
            {
                Correo = $"{nombre.ToLower().Replace(" ", "")}@clinica.com",
                PasswordHash = "123456",
                IdRol = rolMedico?.IdRol ?? 3,
                Activo = true
            };

            var hasher = new PasswordHasher<Usuario>();
            usuario.PasswordHash = hasher.HashPassword(usuario, "123456");

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

        // POST: Admin/AsignarHorario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarHorario(int idMedico, int diaSemana, TimeSpan horaInicio, TimeSpan horaFin)
        {
            var horario = new HorarioMedico
            {
                IdMedico = idMedico,
                DiaSemana = diaSemana,
                HoraInicio = horaInicio,
                HoraFin = horaFin
            };
            _context.HorarioMedico.Add(horario);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Horario asignado exitosamente.";
            return RedirectToAction(nameof(Horarios));
        }

        // GET: Admin/Usuarios
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _context.Usuario
                .Include(u => u.Rol)
                .ToListAsync();

            var roles = await _context.Rol.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "IdRol", "NombreRol");

            return View(usuarios);
        }

        // POST: Admin/CrearUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(string correo, string contrasena, int idRol)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena) || idRol <= 0)
            {
                TempData["Error"] = "Todos los campos son obligatorios.";
                return RedirectToAction(nameof(Usuarios));
            }

            bool existe = await _context.Usuario.AnyAsync(u => u.Correo == correo);
            if (existe)
            {
                TempData["Error"] = "El correo ya se encuentra registrado en el sistema.";
                return RedirectToAction(nameof(Usuarios));
            }

            var nuevoUsuario = new Usuario
            {
                Correo = correo,
                IdRol = idRol,
                Activo = true
            };

            var hasher = new PasswordHasher<Usuario>();
            nuevoUsuario.PasswordHash = hasher.HashPassword(nuevoUsuario, contrasena);

            _context.Usuario.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "Usuario creado exitosamente.";
            return RedirectToAction(nameof(Usuarios));
        }

        // POST: Admin/CambiarEstadoUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoUsuario(int id)
        {
            var usuario = await _context.Usuario.FindAsync(id);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Usuarios));
            }

            usuario.Activo = !usuario.Activo;
            _context.Usuario.Update(usuario);
            await _context.SaveChangesAsync();

            string estadoTexto = usuario.Activo ? "activado" : "desactivado";
            TempData["Exito"] = $"El usuario {usuario.Correo} ha sido {estadoTexto} correctamente.";

            return RedirectToAction(nameof(Usuarios));
        }
    }
}