using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;

namespace Sistema_Gestión_Clinica.Controllers
{
    public class RecepcionController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public RecepcionController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // GET: Recepcion/Index
        public async Task<IActionResult> Index()
        {
            List<Models.Cita> citas1 = await _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                .Include(c => c.EstadoCita)
                .Where(c => c.FechaHora.Date == DateTime.Today)
                .OrderBy(c => c.FechaHora)
                .ToListAsync();
            var citas = citas1;

            return View(citas);
        }

        // POST: Recepcion/MarcarLlegada
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarLlegada(int idCita)
        {
            var cita = await _context.Cita
                .Include(c => c.EstadoCita)
                .FirstOrDefaultAsync(c => c.IdCita == idCita);

            if (cita == null)
            {
                TempData["Mensaje"] = "No se encontró la cita.";
                return RedirectToAction(nameof(Index));
            }

            // Validar que la cita corresponda al día actual.
            if (cita.FechaHora.Date != DateTime.Today)
            {
                TempData["Mensaje"] =
                    "Solo se puede registrar la llegada de citas del día actual.";
                return RedirectToAction(nameof(Index));
            }

            // Evitar registrar dos veces la llegada.
            if (cita.HoraCheckIn != null)
            {
                TempData["Mensaje"] = "La llegada de este paciente ya fue registrada.";
                return RedirectToAction(nameof(Index));
            }

            // Validar que la cita esté Agendada.
            if (cita.EstadoCita?.NombreEstado != "Agendada")
            {
                TempData["Mensaje"] =
                    "La cita no está en un estado válido para registrar la llegada.";
                return RedirectToAction(nameof(Index));
            }

            var estadoEnEspera = await _context.EstadoCita
                .FirstOrDefaultAsync(e => e.NombreEstado == "En Espera");

            if (estadoEnEspera == null)
            {
                TempData["Mensaje"] = "No se encontró el estado En Espera.";
                return RedirectToAction(nameof(Index));
            }

            cita.IdEstadoCita = estadoEnEspera.IdEstadoCita;
            cita.HoraCheckIn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Llegada registrada correctamente.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> WalkIn()
        {
            ViewBag.Medicos = await _context.Medico
                .Include(m => m.Especialidad)
                .OrderBy(m => m.Apellido)
                .ThenBy(m => m.Nombre)
                .ToListAsync();

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WalkIn(
            string DUI,
            string Nombre,
            string Apellido,
            string Telefono,
            string? Correo,
            string? Contrasena,
            int IdMedico,
            string? MotivoConsulta)
        {
            // Recargar los médicos si hay que mostrar un error.
            async Task CargarMedicos()
            {
                ViewBag.Medicos = await _context.Medico
                    .Include(m => m.Especialidad)
                    .OrderBy(m => m.Apellido)
                    .ThenBy(m => m.Nombre)
                    .ToListAsync();
            }

            DUI = (DUI ?? "").Trim();
            Nombre = (Nombre ?? "").Trim();
            Apellido = (Apellido ?? "").Trim();
            Telefono = (Telefono ?? "").Trim();
            Correo = (Correo ?? "").Trim();
            MotivoConsulta = MotivoConsulta?.Trim();

            if (string.IsNullOrWhiteSpace(DUI) ||
                string.IsNullOrWhiteSpace(Nombre) ||
                string.IsNullOrWhiteSpace(Apellido) ||
                string.IsNullOrWhiteSpace(Telefono))
            {
                TempData["Mensaje"] = "Complete los datos del paciente.";
                await CargarMedicos();
                return View();
            }

            var medico = await _context.Medico
                .Include(m => m.Especialidad)
                .FirstOrDefaultAsync(m => m.IdMedico == IdMedico);

            if (medico == null)
            {
                TempData["Mensaje"] = "Seleccione un médico válido.";
                await CargarMedicos();
                return View();
            }

            // Buscar al paciente por DUI.
            var paciente = await _context.Paciente
                .FirstOrDefaultAsync(p => p.DUI == DUI);

            // Si es nuevo, crear Usuario, Paciente y Expediente.
            if (paciente == null)
            {
                if (string.IsNullOrWhiteSpace(Correo) ||
                    string.IsNullOrWhiteSpace(Contrasena))
                {
                    TempData["Mensaje"] =
                        "El paciente es nuevo. Ingrese un correo y una contraseña para crear su usuario.";

                    await CargarMedicos();
                    return View();
                }

                bool correoExiste = await _context.Usuario
                    .AnyAsync(u => u.Correo == Correo);

                if (correoExiste)
                {
                    TempData["Mensaje"] =
                        "Ese correo ya está registrado. Verifique los datos o busque al paciente por su DUI.";

                    await CargarMedicos();
                    return View();
                }

                var rolPaciente = await _context.Rol
                    .FirstOrDefaultAsync(r => r.NombreRol == "Paciente");

                if (rolPaciente == null)
                {
                    TempData["Mensaje"] =
                        "No se encontró el rol Paciente en la base de datos.";

                    await CargarMedicos();
                    return View();
                }

                var usuario = new Usuario
                {
                    IdRol = rolPaciente.IdRol,
                    Correo = Correo,
                    Contrasena = Contrasena,
                    Activo = true
                };

                _context.Usuario.Add(usuario);
                await _context.SaveChangesAsync();

                paciente = new Paciente
                {
                    IdUsuario = usuario.IdUsuario,
                    DUI = DUI,
                    Nombre = Nombre,
                    Apellido = Apellido,
                    Telefono = Telefono
                };

                _context.Paciente.Add(paciente);
                await _context.SaveChangesAsync();

                var expediente = new Expediente
                {
                    IdPaciente = paciente.IdPaciente,
                    FechaApertura = DateTime.Now
                };

                _context.Expediente.Add(expediente);
                await _context.SaveChangesAsync();
            }

            // Comprobar si el médico atiende hoy y a esta hora.
            var ahora = DateTime.Now;

            int diaSemana = ahora.DayOfWeek == DayOfWeek.Sunday
                ? 7
                : (int)ahora.DayOfWeek;

            var tieneHorario = await _context.HorarioMedico
                .AnyAsync(h =>
                    h.IdMedico == IdMedico &&
                    h.DiaSemana == diaSemana &&
                    h.HoraInicio <= ahora.TimeOfDay &&
                    h.HoraFin > ahora.TimeOfDay);

            // Verificar si una cita programada o en consulta
            // ocupa el intervalo de la próxima consulta.
            int duracion = medico.DuracionConsultaMin > 0
                ? medico.DuracionConsultaMin
                : 20;

            var citaOcupada = await _context.Cita
                .Include(c => c.EstadoCita)
                .AnyAsync(c =>
                    c.IdMedico == IdMedico &&
                    c.FechaHora.Date == ahora.Date &&
                    c.FechaHora < ahora.AddMinutes(duracion) &&
                    c.FechaHora.AddMinutes(duracion) > ahora &&
                    c.EstadoCita != null &&
                    (c.EstadoCita.NombreEstado == "Agendada" ||
                     c.EstadoCita.NombreEstado == "En Consulta"));

            var estadoEnEspera = await _context.EstadoCita
                .FirstOrDefaultAsync(e => e.NombreEstado == "En Espera");

            if (estadoEnEspera == null)
            {
                TempData["Mensaje"] =
                    "No se encontró el estado En Espera en la base de datos.";

                await CargarMedicos();
                return View();
            }

            if (tieneHorario && !citaOcupada)
            {
                // Crear turno para el paciente que puede ingresar a la fila.
                string codigoTurno;

                do
                {
                    codigoTurno =
                        $"W{ahora:HHmmss}{Random.Shared.Next(10, 99)}";
                }
                while (await _context.Cita.AnyAsync(
                    c => c.CodigoTurno == codigoTurno));

                var cita = new Cita
                {
                    CodigoTurno = codigoTurno,
                    IdPaciente = paciente.IdPaciente,
                    IdMedico = IdMedico,
                    IdEstadoCita = estadoEnEspera.IdEstadoCita,
                    FechaHora = ahora,
                    HoraCheckIn = ahora,
                    MotivoConsulta = MotivoConsulta
                };

                _context.Cita.Add(cita);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] =
                    $"Llegada registrada. Turno asignado: {codigoTurno}. El paciente fue agregado a la fila de espera.";
            }
            else
            {
                // Sin disponibilidad inmediata: registrar solicitud pendiente.
                var solicitud = new ListaEspera
                {
                    IdPaciente = paciente.IdPaciente,
                    IdMedico = medico.IdMedico,
                    IdEspecialidad = medico.IdEspecialidad,
                    FechaDeseada = ahora.Date,
                    FechaRegistro = ahora,
                    Prioridad = 2,
                    Atendido = false
                };

                _context.ListaEspera.Add(solicitud);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] =
                    "El paciente quedó registrado en la lista de espera porque no hay disponibilidad inmediata.";
            }

            return RedirectToAction(nameof(WalkIn));
        }


    }
}
