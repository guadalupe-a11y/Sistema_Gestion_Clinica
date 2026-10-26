using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    public class AccountController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public AccountController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Usa u.Contrasena en lugar de u.PasswordHash
            var usuario = await _context.Usuario
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Correo == model.Correo && u.Contrasena == model.Contrasena);
            if (usuario == null)
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                return View(model);
            }

            if (!usuario.Activo)
            {
                ModelState.AddModelError("", "La cuenta está inactiva. Contacte al administrador.");
                return View(model);
            }

            // Inicio de sesión con cookie: IdUsuario y rol viajan como claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol?.NombreRol ?? string.Empty)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // Redirección por Rol
            return usuario.Rol?.NombreRol switch
            {
                "Administrador" => RedirectToAction("Index", "Admin"),
                "Recepcionista" => RedirectToAction("Index", "Recepcion"),
                "Medico" => RedirectToAction("Index", "Medico"),
                "Paciente" => RedirectToAction("Index", "Paciente"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string dui, string nombre, string apellido, string telefono, string correo, string password)
        {
            if (await _context.Usuario.AnyAsync(u => u.Correo == correo))
            {
                ViewBag.Error = "El correo ya está registrado.";
                return View();
            }

            var rolPaciente = await _context.Rol.FirstOrDefaultAsync(r => r.NombreRol == "Paciente");

            // 1. Crear Usuario
            var nuevoUsuario = new Usuario
            {
                IdRol = rolPaciente?.IdRol ?? 4,
                Correo = correo,
                PasswordHash = password,
                Activo = true
            };
            _context.Usuario.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            // 2. Crear Paciente
            var nuevoPaciente = new Paciente
            {
                IdUsuario = nuevoUsuario.IdUsuario,
                DUI = dui,
                Nombre = nombre,
                Apellido = apellido,
                Telefono = telefono
            };
            _context.Paciente.Add(nuevoPaciente);
            await _context.SaveChangesAsync();

            // 3. Crear Expediente
            var expediente = new Expediente
            {
                IdPaciente = nuevoPaciente.IdPaciente,
                FechaApertura = DateTime.Now
            };
            _context.Expediente.Add(expediente);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }
    }
}
