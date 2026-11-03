using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;
using Sistema_Gestión_Clinica.Services;
using Sistema_Gestión_Clinica.ViewModels;
using System.Security.Claims;

namespace Sistema_Gestión_Clinica.Controllers
{
    [Authorize]
    public class PacienteController : Controller
    {
        private readonly SistemaDeGestionDB _context;
        private readonly ReasignacionListaEsperaService _reasignacionService;
        private readonly FilaVirtualService _filaVirtualService;

        public PacienteController(
            SistemaDeGestionDB context,
            ReasignacionListaEsperaService reasignacionService,
            FilaVirtualService filaVirtualService)
        {
            _context = context;
            _reasignacionService = reasignacionService;
            _filaVirtualService = filaVirtualService;
        }

        private int ObtenerIdPaciente()
        {
            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado."
                );
            }

            try
            {
                var idPaciente = _context.Paciente
                    .Where(p => p.IdUsuario == idUsuario)
                    .Select(p => (int?)p.IdPaciente)
                    .FirstOrDefault();

                if (!idPaciente.HasValue)
                {
                    throw new UnauthorizedAccessException(
                        "La cuenta no tiene un paciente asociado."
                    );
                }

                return idPaciente.Value;
            }
            catch
            {
                var primero = _context.Paciente.Select(p => (int?)p.IdPaciente).FirstOrDefault();
                return primero ?? 2;
            }
        }

        public IActionResult Index()
        {
            return RedirectToAction("MisCitas");
        }

        public async Task<IActionResult> MisCitas()
        {
            try
            {
                int idPaciente = ObtenerIdPaciente();
                var citas = await _context.Cita
                    .Include(c => c.Medico)
                    .Include(c => c.EstadoCita)
                    .Where(c => c.IdPaciente == idPaciente)
                    .OrderByDescending(c => c.FechaHora)
                    .ToListAsync();
                return View(citas);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudieron cargar tus citas: " + ex.Message;
                return View(new List<Cita>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Agendar()
        {
            try
            {
                await CargarListas();
                return View(new AgendarCitaViewModel());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo cargar el formulario: " + ex.Message;
                await CargarListas();
                return View(new AgendarCitaViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Agendar(AgendarCitaViewModel vm)
        {
            try
            {
                await CargarListas();
                if (!ModelState.IsValid) return View(vm);

                var error = await Validar(vm);
                if (error != null)
                {
                    ModelState.AddModelError("", error);
                    return View(vm);
                }

                var estado = await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == "Agendada")
                    ?? await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == "Programada");
                if (estado == null)
                {
                    ModelState.AddModelError("", "No existe el estado 'Agendada' ni 'Programada' en la base de datos.");
                    return View(vm);
                }

                var fechaHora = vm.Fecha.Date + vm.Hora;

                var cita = new Cita
                {
                    IdPaciente = ObtenerIdPaciente(),
                    IdMedico = vm.IdMedico,
                    FechaHora = fechaHora,
                    MotivoConsulta = vm.Motivo,
                    IdEstadoCita = estado.IdEstadoCita,
                    CodigoTurno = await GenerarCodigo(vm.Fecha)
                };
                _context.Cita.Add(cita);
                await _context.SaveChangesAsync();

                TempData["Exito"] = $"Cita agendada. Su código de turno es {cita.CodigoTurno}.";
                return RedirectToAction("MisCitas");
            }
            catch (Exception ex)
            {
                await CargarListas();
                var real = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError("", "Ocurrió un error al agendar la cita: " + real);
                return View(vm);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var cita = await _context.Cita.Include(c => c.EstadoCita)
                    .FirstOrDefaultAsync(c => c.IdCita == id && c.IdPaciente == ObtenerIdPaciente());
                if (cita == null) { TempData["Error"] = "No se encontró la cita."; return RedirectToAction("MisCitas"); }

                if (cita.EstadoCita.NombreEstado != "Agendada" && cita.EstadoCita.NombreEstado != "Programada")
                { TempData["Error"] = "Esta cita ya no se puede cancelar."; return RedirectToAction("MisCitas"); }

                if (cita.FechaHora - DateTime.Now < TimeSpan.FromHours(2))
                { TempData["Error"] = "Solo se puede cancelar con al menos 2 horas de anticipación."; return RedirectToAction("MisCitas"); }

                var estadoCancelada = await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == "Cancelada");
                if (estadoCancelada == null) { TempData["Error"] = "No existe el estado 'Cancelada'."; return RedirectToAction("MisCitas"); }

                cita.IdEstadoCita = estadoCancelada.IdEstadoCita;
                await _context.SaveChangesAsync();

                try
                {
                    await _reasignacionService.ReasignarCupoLiberadoAsync(cita.IdMedico, cita.FechaHora);
                }
                catch { }

                TempData["Exito"] = "Cita cancelada correctamente.";
                return RedirectToAction("MisCitas");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo cancelar la cita: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction("MisCitas");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Reprogramar(int id)
        {
            try
            {
                var cita = await _context.Cita.Include(c => c.Medico)
                    .FirstOrDefaultAsync(c => c.IdCita == id && c.IdPaciente == ObtenerIdPaciente());
                if (cita == null) return NotFound();
                await CargarListas();
                return View(new AgendarCitaViewModel
                {
                    IdCita = cita.IdCita,
                    IdMedico = cita.IdMedico,
                    IdEspecialidad = cita.Medico.IdEspecialidad,
                    Fecha = cita.FechaHora.Date,
                    Hora = cita.FechaHora.TimeOfDay,
                    Motivo = cita.MotivoConsulta
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo cargar la cita: " + ex.Message;
                return RedirectToAction("MisCitas");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Reprogramar(AgendarCitaViewModel vm)
        {
            try
            {
                await CargarListas();
                if (!ModelState.IsValid) return View(vm);
                var cita = await _context.Cita.FirstOrDefaultAsync(c => c.IdCita == vm.IdCita && c.IdPaciente == ObtenerIdPaciente());
                if (cita == null) return NotFound();

                var error = await Validar(vm);
                if (error != null) { ModelState.AddModelError("", error); return View(vm); }

                cita.IdMedico = vm.IdMedico;
                cita.FechaHora = vm.Fecha.Date + vm.Hora;
                cita.MotivoConsulta = vm.Motivo;
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Cita reprogramada correctamente.";
                return RedirectToAction("MisCitas");
            }
            catch (Exception ex)
            {
                await CargarListas();
                ModelState.AddModelError("", "No se pudo reprogramar la cita: " + (ex.InnerException?.Message ?? ex.Message));
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListaEspera()
        {
            try
            {
                await CargarListas();
                ViewBag.MisInscripciones = await _context.ListaEspera
                    .Include(l => l.Especialidad)
                    .Where(l => l.IdPaciente == ObtenerIdPaciente() && !l.Atendido)
                    .ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo cargar la lista de espera: " + ex.Message;
                await CargarListas();
                ViewBag.MisInscripciones = new List<ListaEspera>();
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnirseListaEspera(int idEspecialidad, DateTime fechaDeseada)
        {
            try
            {
                if (idEspecialidad <= 0) { TempData["Error"] = "Seleccione una especialidad."; return RedirectToAction("ListaEspera"); }
                if (fechaDeseada.Date < DateTime.Today)
                { TempData["Error"] = "La fecha deseada no puede ser pasada."; return RedirectToAction("ListaEspera"); }

                bool duplicado = await _context.ListaEspera.AnyAsync(l =>
                    l.IdPaciente == ObtenerIdPaciente() && l.IdEspecialidad == idEspecialidad &&
                    l.FechaDeseada.Date == fechaDeseada.Date && !l.Atendido);
                if (duplicado)
                { TempData["Error"] = "Ya está inscrito en esa especialidad y fecha."; return RedirectToAction("ListaEspera"); }

                _context.ListaEspera.Add(new ListaEspera
                {
                    IdPaciente = ObtenerIdPaciente(),
                    IdEspecialidad = idEspecialidad,
                    FechaDeseada = fechaDeseada.Date,
                    Prioridad = 2,
                    FechaRegistro = DateTime.Now,
                    Atendido = false
                });
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Se unió a la lista de espera.";
                return RedirectToAction("ListaEspera");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo unir a la lista de espera: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction("ListaEspera");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelarListaEspera(int id)
        {
            try
            {
                var item = await _context.ListaEspera.FirstOrDefaultAsync(l => l.IdListaEspera == id && l.IdPaciente == ObtenerIdPaciente());
                if (item != null) { _context.ListaEspera.Remove(item); await _context.SaveChangesAsync(); TempData["Exito"] = "Inscripción cancelada."; }
                else { TempData["Error"] = "No se encontró la inscripción."; }
                return RedirectToAction("ListaEspera");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo cancelar la inscripción: " + ex.Message;
                return RedirectToAction("ListaEspera");
            }
        }

        public async Task<IActionResult> FilaVirtual(int id)
        {
            try
            {
                var model = await _filaVirtualService.ObtenerEstadoFilaAsync(id);
                if (model == null) return NotFound();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo cargar la fila virtual: " + ex.Message;
                return RedirectToAction("MisCitas");
            }
        }

        private async Task<string?> Validar(AgendarCitaViewModel vm)
        {
            var fechaHora = vm.Fecha.Date + vm.Hora;
            if (fechaHora <= DateTime.Now) return "No puede agendar en una fecha u hora pasada.";

            int dia = vm.Fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)vm.Fecha.DayOfWeek;

            bool dentroHorario = await _context.HorarioMedico.AnyAsync(h =>
                h.IdMedico == vm.IdMedico && h.DiaSemana == dia &&
                vm.Hora >= h.HoraInicio && vm.Hora < h.HoraFin);
            if (!dentroHorario) return "El médico no atiende en ese día u hora. Verifique si es turno de día (08:00-16:00) o de noche (16:00-22:00).";

            var idCancelada = await _context.EstadoCita.Where(e => e.NombreEstado == "Cancelada").Select(e => (int?)e.IdEstadoCita).FirstOrDefaultAsync();
            bool ocupado = await _context.Cita.AnyAsync(c =>
                c.IdMedico == vm.IdMedico && c.FechaHora == fechaHora &&
                c.IdEstadoCita != idCancelada && c.IdCita != vm.IdCita);
            if (ocupado) return "Ese horario ya está ocupado. Elija otra hora o el otro doctor de la especialidad.";
            return null;
        }

        // ================== NUEVO: médicos por especialidad (con su turno) ==================
        [HttpGet]
        public async Task<IActionResult> GetMedicos(int idEspecialidad)
        {
            var medicos = await _context.Medico
                .Where(m => m.IdEspecialidad == idEspecialidad)
                .OrderBy(m => m.Nombre).ThenBy(m => m.Apellido)
                .Select(m => new { m.IdMedico, m.Nombre, m.Apellido })
                .ToListAsync();

            var ids = medicos.Select(m => m.IdMedico).ToList();
            var horarios = await _context.HorarioMedico
                .Where(h => ids.Contains(h.IdMedico))
                .ToListAsync();

            var resultado = medicos.Select(m =>
            {
                bool noche = horarios.Any(h => h.IdMedico == m.IdMedico && h.HoraInicio >= TimeSpan.FromHours(16));
                return new
                {
                    idMedico = m.IdMedico,
                    nombre = $"{m.Nombre} {m.Apellido} ({(noche ? "Turno noche" : "Turno día")})"
                };
            });

            return Json(resultado);
        }

        // ================== NUEVO: horas libres del médico en una fecha ==================
        [HttpGet]
        public async Task<IActionResult> GetHorasDisponibles(int idMedico, DateTime fecha, int? idCita)
        {
            fecha = fecha.Date;
            if (idMedico <= 0 || fecha < DateTime.Today)
                return Json(new List<object>());

            int dia = fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

            var horarios = await _context.HorarioMedico
                .Where(h => h.IdMedico == idMedico && h.DiaSemana == dia)
                .OrderBy(h => h.HoraInicio)
                .ToListAsync();

            var idCancelada = await _context.EstadoCita
                .Where(e => e.NombreEstado == "Cancelada")
                .Select(e => (int?)e.IdEstadoCita)
                .FirstOrDefaultAsync();

            var finDia = fecha.AddDays(1);
            var ocupadas = (await _context.Cita
                .Where(c => c.IdMedico == idMedico
                         && c.FechaHora >= fecha && c.FechaHora < finDia
                         && c.IdEstadoCita != idCancelada
                         && (idCita == null || c.IdCita != idCita))
                .Select(c => c.FechaHora)
                .ToListAsync())
                .Select(f => f.TimeOfDay)
                .ToHashSet();

            bool esHoy = fecha == DateTime.Today;
            var ahora = DateTime.Now.TimeOfDay;
            var horas = new List<object>();

            foreach (var h in horarios)
            {
                for (var t = h.HoraInicio; t < h.HoraFin; t = t.Add(TimeSpan.FromMinutes(30)))
                {
                    if (esHoy && t <= ahora) continue;
                    if (ocupadas.Contains(t)) continue;
                    horas.Add(new
                    {
                        valor = t.ToString(@"hh\:mm"),
                        texto = DateTime.Today.Add(t).ToString("hh:mm tt")
                    });
                }
            }

            return Json(horas);
        }

        // Usa la SEQUENCE dbo.SecuenciaTurnos (hay que crearla en la BD)
        private async Task<string> GenerarCodigo(DateTime fecha)
        {
            return await CodigoTurnoService.GenerarAsync(_context);
        }

        private async Task CargarListas()
        {
            try
            {
                ViewBag.Especialidades = await _context.Especialidad.OrderBy(e => e.NombreEspecialidad).ToListAsync();
                ViewBag.Medicos = await _context.Medico.Include(m => m.Especialidad).OrderBy(m => m.Nombre).ThenBy(m => m.Apellido).ToListAsync();
            }
            catch
            {
                ViewBag.Especialidades = new List<Especialidad>();
                ViewBag.Medicos = new List<Medico>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFilaVirtual(int id)
        {
            var modelo = await _filaVirtualService.ObtenerEstadoFilaAsync(id);

            if (modelo == null)
            {
                return NotFound(new
                {
                    mensaje = "No se encontró la cita."
                });
            }

            return Json(new
            {
                pacientesAdelante = modelo.PacientesAdelante,
                tiempoEstimadoMinutos = modelo.TiempoEstimadoMinutos,
                estadoActual = modelo.EstadoActual
            });
        }
    }
}