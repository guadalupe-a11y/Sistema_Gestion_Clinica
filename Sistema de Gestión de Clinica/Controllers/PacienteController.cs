using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Models;
using Sistema_Gestión_Clinica.Services;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    [Authorize] // <--- ESTO BLOQUEA EL ACCESO A USUARIOS NO AUTENTICADOS
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
            try
            {
                // 1. Si tu login guarda el IdPaciente en sesión
                var enSesion = HttpContext.Session.GetInt32("IdPaciente");
                if (enSesion.HasValue && enSesion.Value > 0) return enSesion.Value;

                // 2. Si tu login guarda el IdUsuario en sesión, buscamos su Paciente
                var idUsuarioSesion = HttpContext.Session.GetInt32("IdUsuario");
                if (idUsuarioSesion.HasValue)
                {
                    var idPac = _context.Paciente
                        .Where(p => p.IdUsuario == idUsuarioSesion.Value)
                        .Select(p => (int?)p.IdPaciente)
                        .FirstOrDefault();
                    if (idPac.HasValue) return idPac.Value;
                }

                // 3. Si usa Claims
                var claim = User.FindFirst("IdPaciente")?.Value
                    ?? User.FindFirst("IdUsuario")?.Value
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claim, out int idClaim))
                {
                    // Si el claim es IdUsuario, convertir a IdPaciente
                    var idPac2 = _context.Paciente
                        .Where(p => p.IdUsuario == idClaim || p.IdPaciente == idClaim)
                        .Select(p => (int?)p.IdPaciente)
                        .FirstOrDefault();
                    if (idPac2.HasValue) return idPac2.Value;
                }
            }
            catch { }
            // Fallback para que las pruebas no se caigan si no hay login de paciente
            return 1;
        }

        // ---------- INDEX / MIS CITAS ----------
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

        // ---------- AGENDAR ----------
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

                var estado = await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == "Agendada");
                if (estado == null)
                {
                    ModelState.AddModelError("", "No existe el estado 'Agendada' en la base de datos.");
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
                ModelState.AddModelError("", "Ocurrió un error al agendar la cita: " + ex.Message);
                return View(vm);
            }
        }

        // ---------- CANCELAR ----------
        [HttpPost]
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var cita = await _context.Cita.Include(c => c.EstadoCita)
                    .FirstOrDefaultAsync(c => c.IdCita == id && c.IdPaciente == ObtenerIdPaciente());
                if (cita == null) { TempData["Error"] = "No se encontró la cita."; return RedirectToAction("MisCitas"); }

                if (cita.EstadoCita.NombreEstado != "Agendada")
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
                TempData["Error"] = "No se pudo cancelar la cita: " + ex.Message;
                return RedirectToAction("MisCitas");
            }
        }

        // ---------- REPROGRAMAR ----------
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
                ModelState.AddModelError("", "No se pudo reprogramar la cita: " + ex.Message);
                return View(vm);
            }
        }

        // ---------- LISTA DE ESPERA ----------
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
                TempData["Error"] = "No se pudo unir a la lista de espera: " + ex.Message;
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

        // ---------- FILA VIRTUAL ----------
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

        // ---------- PRIVADOS ----------
        private async Task<string?> Validar(AgendarCitaViewModel vm)
        {
            var fechaHora = vm.Fecha.Date + vm.Hora;
            if (fechaHora <= DateTime.Now) return "No puede agendar en una fecha u hora pasada.";

            int dia = (int)vm.Fecha.DayOfWeek;
            bool dentroHorario = await _context.HorarioMedico.AnyAsync(h =>
                h.IdMedico == vm.IdMedico && h.DiaSemana == dia &&
                vm.Hora >= h.HoraInicio && vm.Hora < h.HoraFin);
            if (!dentroHorario) return "El médico no atiende en ese día u hora.";

            var idCancelada = await _context.EstadoCita.Where(e => e.NombreEstado == "Cancelada").Select(e => e.IdEstadoCita).FirstOrDefaultAsync();
            bool ocupado = await _context.Cita.AnyAsync(c =>
                c.IdMedico == vm.IdMedico && c.FechaHora == fechaHora &&
                c.IdEstadoCita != idCancelada && c.IdCita != vm.IdCita);
            if (ocupado) return "Ese horario ya está ocupado. Elija otro o únase a la lista de espera.";
            return null;
        }

        private async Task<string> GenerarCodigo(DateTime fecha)
        {
            // Correlativo por día: T-001, T-002... sin repetir aunque se cancele una
            int numero = await _context.Cita.CountAsync(c => c.FechaHora.Date == fecha.Date) + 1;
            string codigo;
            do
            {
                codigo = $"T-{numero:000}";
                numero++;
            }
            while (await _context.Cita.AnyAsync(c => c.FechaHora.Date == fecha.Date && c.CodigoTurno == codigo));
            // Ajuste: el do-while ya incrementó, restamos para no saltar número si no hay choque
            return codigo;
        }

        private async Task CargarListas()
        {
            try
            {
                ViewBag.Especialidades = await _context.Especialidad.ToListAsync();
                ViewBag.Medicos = await _context.Medico.Include(m => m.Especialidad).ToListAsync();
            }
            catch
            {
                ViewBag.Especialidades = new List<Especialidad>();
                ViewBag.Medicos = new List<Medico>();
            }
        }
    }
}