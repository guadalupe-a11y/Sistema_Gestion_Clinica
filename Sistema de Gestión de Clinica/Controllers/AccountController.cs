using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
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
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public AccountController(SistemaDeGestionDB context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Buscar usuario por correo
            var usuario = await _context.Usuario
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Correo == model.Correo);

            if (usuario == null || !usuario.Activo)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas o la cuenta se encuentra inactiva.");
                return View(model);
            }

            // 2. Validación de contraseña con doble soporte (Texto plano vs PasswordHasher)
            bool esPasswordValida = false;

            // A. Verificación directa si está en texto plano en SQL ('123456')
            if (usuario.PasswordHash == model.Contrasena)
            {
                esPasswordValida = true;
            }
            else
            {
                // B. Verificación con PasswordHasher previniendo excepciones de formato Base64
                try
                {
                    var result = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.Contrasena);
                    if (result != PasswordVerificationResult.Failed)
                    {
                        esPasswordValida = true;
                    }
                }
                catch (FormatException)
                {
                    esPasswordValida = false;
                }
                catch (Exception)
                {
                    esPasswordValida = false;
                }
            }

            if (!esPasswordValida)
            {
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                return View(model);
            }

            // 3. Crear Claims de Autenticación (Permite reemplazar idPaciente=1 e idMedico=1 dinámicamente)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol?.NombreRol ?? "Paciente")
            };

            if (usuario.Rol?.NombreRol == "Paciente")
            {
                var paciente = await _context.Paciente.FirstOrDefaultAsync(p => p.IdUsuario == usuario.IdUsuario);
                if (paciente != null) claims.Add(new Claim("IdPaciente", paciente.IdPaciente.ToString()));
            }
            else if (usuario.Rol?.NombreRol == "Medico")
            {
                var medico = await _context.Medico.FirstOrDefaultAsync(m => m.IdUsuario == usuario.IdUsuario);
                if (medico != null) claims.Add(new Claim("IdMedico", medico.IdMedico.ToString()));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 4. Redirección por Rol
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
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Validación de correo único
            if (await _context.Usuario.AnyAsync(u => u.Correo == model.Correo))
            {
                ModelState.AddModelError("Correo", "El correo ya está registrado.");
                return View(model);
            }

            // Validación de DUI único
            if (await _context.Paciente.AnyAsync(p => p.DUI == model.DUI))
            {
                ModelState.AddModelError("DUI", "El número de DUI ya está registrado.");
                return View(model);
            }

            var rolPaciente = await _context.Rol.FirstOrDefaultAsync(r => r.NombreRol == "Paciente");

            var nuevoUsuario = new Usuario
            {
                IdRol = rolPaciente?.IdRol ?? 4,
                Correo = model.Correo,
                Activo = true
            };

            // Encriptación segura con PasswordHasher
            nuevoUsuario.PasswordHash = _passwordHasher.HashPassword(nuevoUsuario, model.Password);

            _context.Usuario.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            var nuevoPaciente = new Paciente
            {
                IdUsuario = nuevoUsuario.IdUsuario,
                DUI = model.DUI,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Telefono = model.Telefono
            };

            _context.Paciente.Add(nuevoPaciente);
            await _context.SaveChangesAsync();

            var expediente = new Expediente
            {
                IdPaciente = nuevoPaciente.IdPaciente,
                FechaApertura = DateTime.Now
            };

            _context.Expediente.Add(expediente);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}