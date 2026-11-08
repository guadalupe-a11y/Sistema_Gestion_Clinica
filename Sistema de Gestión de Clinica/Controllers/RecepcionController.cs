using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;
using Sistema_Gestión_Clinica.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    [Authorize]
    public class RecepcionController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public RecepcionController(SistemaDeGestionDB context)
        {
            _context = context;
        }


        // GET: Recepcion/Medicos
        [Authorize(Roles = "Recepcionista,Administrador")]
        [HttpGet]
        public async Task<IActionResult> Medicos()
        {
            var model = new MedicosViewModel();

            model.Medicos = await _context.Medico
                .Include(m => m.Especialidad)
                .Include(m => m.Consultorio)
                .ToListAsync();

            model.Especialidades = await _context.Especialidad
                .Select(e => new SelectListItem
                {
                    Value = e.IdEspecialidad.ToString(),
                    Text = e.NombreEspecialidad
                })
                .ToListAsync();

            model.Consultorios = await _context.Consultorio
                .Select(c => new SelectListItem
                {
                    Value = c.IdConsultorio.ToString(),
                    Text = "Consultorio " + c.NumeroConsultorio
                })
                .ToListAsync();

            return View("~/Views/Admin/Medicos.cshtml", model);
        }

        // GET: Recepcion/Horarios
        [Authorize(Roles = "Recepcionista,Administrador")]
        [HttpGet]
        public async Task<IActionResult> Horarios()
        {
            var horarios = await _context.HorarioMedico
                .Include(h => h.Medico)
                .ToListAsync();

            var medicos = await _context.Medico
                .Select(m => new
                {
                    m.IdMedico,
                    NombreCompleto = "Dr/a. " + m.Nombre + " " + m.Apellido
                })
                .ToListAsync();

            ViewBag.Medicos = new SelectList(
                medicos,
                "IdMedico",
                "NombreCompleto"
            );

            return View("~/Views/Admin/Horarios.cshtml", horarios);
        }



        // GET: Recepcion/Index?estado=Todas&fecha=2026-11-02
        public async Task<IActionResult> Index(string? estado, DateTime? fecha)
        {
            // Por defecto se muestran las citas del día actual.
            DateTime fechaSeleccionada = fecha?.Date ?? DateTime.Today;
            DateTime fechaSiguiente = fechaSeleccionada.AddDays(1);

            var query = _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                    .ThenInclude(m => m.Especialidad)
                .Include(c => c.EstadoCita)
                .Where(c =>
                    c.FechaHora >= fechaSeleccionada &&
                    c.FechaHora < fechaSiguiente);

            // Filtrar por estado cuando se seleccione uno.
            if (!string.IsNullOrEmpty(estado) && estado != "Todas")
            {
                query = query.Where(c =>
                    c.EstadoCita != null &&
                    c.EstadoCita.NombreEstado == estado);
            }

            var citas = await query
                .OrderBy(c => c.FechaHora)
                .ToListAsync();

            ViewBag.EstadoFiltro = estado ?? "Todas";
            ViewBag.FechaFiltro = fechaSeleccionada.ToString("yyyy-MM-dd");

            return View(citas);
        }


        // GET: Recepcion/VerExpediente/5
        public async Task<IActionResult> VerExpediente(int idPaciente)
        {
            var expediente = await _context.Expediente
                .Include(e => e.Paciente)
                .FirstOrDefaultAsync(e => e.IdPaciente == idPaciente);

            if (expediente == null)
            {
                TempData["Mensaje"] = "El paciente aún no cuenta con un expediente abierto.";
                // Redirige directamente al ExpedienteController con el Id del paciente
                return RedirectToAction("Ver", "Expediente", new { idPaciente });
            }

            //// Cargar el historial de atenciones / consultas de forma segura
            var historialAtenciones = await _context.RegistroAtencion
                .Include(r => r.Cita)
                    .ThenInclude(c => c!.Medico)
                .Where(r => r.Cita != null && r.Cita.IdPaciente == idPaciente)
                .OrderByDescending(r => r.FechaRegistro)
                .ToListAsync();

            ViewBag.Historial = historialAtenciones;

            return View(expediente);
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

            if (cita.FechaHora.Date != DateTime.Today)
            {
                TempData["Mensaje"] = "Solo se puede registrar la llegada de citas del día actual.";
                return RedirectToAction(nameof(Index));
            }

            if (cita.HoraCheckIn != null)
            {
                TempData["Mensaje"] = "La llegada de este paciente ya fue registrada.";
                return RedirectToAction(nameof(Index));
            }

            if (cita.EstadoCita?.NombreEstado != "Agendada")
            {
                TempData["Mensaje"] = "La cita no está en un estado válido para registrar la llegada.";
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

            var paciente = await _context.Paciente
                .FirstOrDefaultAsync(p => p.DUI == DUI);

            if (paciente == null)
            {
                if (string.IsNullOrWhiteSpace(Correo) || string.IsNullOrWhiteSpace(Contrasena))
                {
                    TempData["Mensaje"] = "El paciente es nuevo. Ingrese un correo y una contraseña para crear su usuario.";
                    await CargarMedicos();
                    return View();
                }

                bool correoExiste = await _context.Usuario.AnyAsync(u => u.Correo == Correo);
                if (correoExiste)
                {
                    TempData["Mensaje"] = "Ese correo ya está registrado. Verifique los datos o busque al paciente por su DUI.";
                    await CargarMedicos();
                    return View();
                }

                var rolPaciente = await _context.Rol.FirstOrDefaultAsync(r => r.NombreRol == "Paciente");
                if (rolPaciente == null)
                {
                    TempData["Mensaje"] = "No se encontró el rol Paciente en la base de datos.";
                    await CargarMedicos();
                    return View();
                }

                var usuario = new Usuario
                {
                    IdRol = rolPaciente.IdRol,
                    Correo = Correo,
                    Activo = true
                };

                //Proteger la contraseña del paciente nuevo
                var passwordHasher = new PasswordHasher<Usuario>();

                usuario.PasswordHash = passwordHasher.HashPassword(
                    usuario,
                    Contrasena!
                );

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

            var ahora = DateTime.Now;
            int diaSemana = ahora.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)ahora.DayOfWeek;

            var tieneHorario = await _context.HorarioMedico
                .AnyAsync(h =>
                    h.IdMedico == IdMedico &&
                    h.DiaSemana == diaSemana &&
                    h.HoraInicio <= ahora.TimeOfDay &&
                    h.HoraFin > ahora.TimeOfDay);

            int duracion = medico.DuracionConsultaMin > 0 ? medico.DuracionConsultaMin : 20;

            var citaOcupada = await _context.Cita
                .Include(c => c.EstadoCita)
                .AnyAsync(c =>
                    c.IdMedico == IdMedico &&
                    c.FechaHora.Date == ahora.Date &&
                    c.FechaHora < ahora.AddMinutes(duracion) &&
                    c.FechaHora.AddMinutes(duracion) > ahora &&
                    c.EstadoCita != null &&
                    (c.EstadoCita.NombreEstado == "Agendada" || c.EstadoCita.NombreEstado == "En Consulta"));

            var estadoEnEspera = await _context.EstadoCita
                .FirstOrDefaultAsync(e => e.NombreEstado == "En Espera");

            if (estadoEnEspera == null)
            {
                TempData["Mensaje"] = "No se encontró el estado En Espera en la base de datos.";
                await CargarMedicos();
                return View();
            }

            if (tieneHorario && !citaOcupada)
            {
                string codigoTurno =
                await CodigoTurnoService.GenerarAsync(_context);

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

                TempData["Mensaje"] = $"Llegada registrada. Turno asignado: {codigoTurno}. El paciente fue agregado a la fila de espera.";
            }
            else
            {
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

                TempData["Mensaje"] = "El paciente quedó registrado en la lista de espera porque no hay disponibilidad inmediata.";
            }

            return RedirectToAction(nameof(WalkIn));
        }


        // GET: Recepcion/ReprogramarCancelada/5
        [HttpGet]
        [Authorize(Roles = "Recepcionista,Administrador")]
        public async Task<IActionResult> ReprogramarCancelada(int id)
        {
            var cita = await _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.Medico)
                .Include(c => c.EstadoCita)
                .FirstOrDefaultAsync(c => c.IdCita == id);

            if (cita == null)
                return NotFound();

            if (cita.EstadoCita?.NombreEstado != "Cancelada")
            {
                TempData["Mensaje"] =
                    "Solo se pueden reprogramar citas canceladas.";

                return RedirectToAction(nameof(Index));
            }

            return View(cita);
        }

        // POST: Recepcion/ReprogramarCancelada
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Recepcionista,Administrador")]
        public async Task<IActionResult> ReprogramarCancelada(
            int idCita,
            DateTime fecha,
            TimeSpan hora)
        {
            var cita = await _context.Cita
                .Include(c => c.EstadoCita)
                .FirstOrDefaultAsync(c => c.IdCita == idCita);

            if (cita == null)
                return NotFound();

            if (cita.EstadoCita?.NombreEstado != "Cancelada")
            {
                TempData["Error"] =
                    "La cita ya no está cancelada.";

                return RedirectToAction(nameof(Index));
            }

            DateTime nuevaFechaHora = fecha.Date.Add(hora);

            // VALIDAR FECHA FUTURA
            if (nuevaFechaHora <= DateTime.Now)
            {
                TempData["Error"] =
                    "Seleccione una fecha y hora futura.";

                return RedirectToAction(
                    nameof(ReprogramarCancelada),
                    new { id = idCita });
            }

            // OBTENER EL DÍA DE LA SEMANA
            int diaSemana = fecha.DayOfWeek == DayOfWeek.Sunday
                ? 7
                : (int)fecha.DayOfWeek;

            // VALIDAR HORARIO DEL MÉDICO
            bool horarioValido = await _context.HorarioMedico
                .AnyAsync(h =>
                    h.IdMedico == cita.IdMedico &&
                    h.DiaSemana == diaSemana &&
                    hora >= h.HoraInicio &&
                    hora < h.HoraFin);

            if (!horarioValido)
            {
                TempData["Error"] =
                    "El médico no atiende en la fecha y hora seleccionadas.";

                return RedirectToAction(
                    nameof(ReprogramarCancelada),
                    new { id = idCita });
            }

            // VERIFICAR SI EL HORARIO ESTÁ OCUPADO
            bool horarioOcupado = await _context.Cita
                .Include(c => c.EstadoCita)
                .AnyAsync(c =>
                    c.IdMedico == cita.IdMedico &&
                    c.IdCita != idCita &&
                    c.FechaHora == nuevaFechaHora &&
                    c.EstadoCita != null &&
                    c.EstadoCita.NombreEstado != "Cancelada");

            if (horarioOcupado)
            {
                TempData["Error"] =
                    "El horario seleccionado ya está ocupado.";

                return RedirectToAction(
                    nameof(ReprogramarCancelada),
                    new { id = idCita });
            }

            // BUSCAR ESTADO AGENDADA
            var estadoAgendada = await _context.EstadoCita
                .FirstOrDefaultAsync(e =>
                    e.NombreEstado == "Agendada");

            if (estadoAgendada == null)
            {
                TempData["Error"] =
                    "No se encontró el estado Agendada.";

                return RedirectToAction(
                    nameof(ReprogramarCancelada),
                    new { id = idCita });
            }

            // ACTUALIZAR LA CITA EXISTENTE
            cita.FechaHora = nuevaFechaHora;
            cita.IdEstadoCita = estadoAgendada.IdEstadoCita;
            cita.HoraCheckIn = null;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "La cita cancelada fue reprogramada correctamente.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    fecha = nuevaFechaHora.ToString("yyyy-MM-dd"),
                    estado = "Agendada"
                });
        }   

    }
}