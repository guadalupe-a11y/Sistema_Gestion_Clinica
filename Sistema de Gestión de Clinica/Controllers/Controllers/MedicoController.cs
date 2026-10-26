using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.Helpers;
using Sistema_Gestión_Clinica.Models;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    [Authorize(Roles = "Medico")]
    public class MedicoController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public MedicoController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // ------------------------------------------------------------------
        // Panel del día
        // ------------------------------------------------------------------

        // GET: Medico
        public async Task<IActionResult> Index()
        {
            var medico = await ObtenerMedicoActualAsync();
            if (medico == null) return SinPerfil();

            return View(await ConstruirPanelAsync(medico));
        }

        // GET: Medico/FilaPartial  (lo consulta el fetch cada pocos segundos, sin recargar la página)
        public async Task<IActionResult> FilaPartial()
        {
            var medico = await ObtenerMedicoActualAsync();
            if (medico == null) return SinPerfil();

            return PartialView("_Fila", await ConstruirPanelAsync(medico));
        }

        // ------------------------------------------------------------------
        // Llamar siguiente (solo desde "En Espera", solo el médico asignado)
        // ------------------------------------------------------------------

        // POST: Medico/LlamarSiguiente   (idCita opcional: si no viene, se toma el siguiente de la cola)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LlamarSiguiente(int? idCita)
        {
            var medico = await ObtenerMedicoActualAsync();
            if (medico == null) return SinPerfil();

            try
            {
                var hoy = DateTime.Today;
                var manana = hoy.AddDays(1);

                // Si ya hay alguien en consulta, primero hay que terminar esa atención.
                var enConsulta = await _context.Cita
                    .Where(c => c.IdMedico == medico.IdMedico
                             && c.FechaHora >= hoy && c.FechaHora < manana
                             && c.EstadoCita!.NombreEstado == EstadosCita.EnConsulta)
                    .FirstOrDefaultAsync();

                if (enConsulta != null)
                {
                    TempData["Error"] = "Ya tiene un paciente en consulta. Finalice esa atención antes de llamar al siguiente.";
                    return RedirectToAction(nameof(Atender), new { idCita = enConsulta.IdCita });
                }

                Cita? cita;
                if (idCita.HasValue)
                {
                    cita = await _context.Cita.Include(c => c.EstadoCita)
                        .FirstOrDefaultAsync(c => c.IdCita == idCita.Value);
                    if (cita == null) return NotFound();
                    if (cita.IdMedico != medico.IdMedico)
                        return Denegar("Esta cita está asignada a otro médico.");
                }
                else
                {
                    cita = await _context.Cita.Include(c => c.EstadoCita)
                        .Where(c => c.IdMedico == medico.IdMedico
                                 && c.FechaHora >= hoy && c.FechaHora < manana
                                 && c.EstadoCita!.NombreEstado == EstadosCita.EnEspera)
                        .OrderBy(c => c.HoraCheckIn ?? c.FechaHora)
                        .FirstOrDefaultAsync();

                    if (cita == null)
                    {
                        TempData["Error"] = "No hay pacientes en espera. Los pacientes deben hacer check-in en recepción.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                if (cita.EstadoCita?.NombreEstado != EstadosCita.EnEspera)
                {
                    return Denegar($"No se puede llamar a este paciente: la cita está en estado '{cita.EstadoCita?.NombreEstado}'. " +
                                   "Solo se llama a pacientes con check-in realizado (En Espera).");
                }

                var estadoConsulta = await ObtenerEstadoAsync(EstadosCita.EnConsulta);
                cita.IdEstadoCita = estadoConsulta.IdEstadoCita;
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Atender), new { idCita = cita.IdCita });
            }
            catch (DbUpdateException)
            {
                return Denegar("No se pudo actualizar la cita. Intente de nuevo.");
            }
        }

        // ------------------------------------------------------------------
        // Consulta en curso
        // ------------------------------------------------------------------

        // GET: Medico/Atender?idCita=5
        [HttpGet]
        public async Task<IActionResult> Atender(int idCita)
        {
            var medico = await ObtenerMedicoActualAsync();
            if (medico == null) return SinPerfil();

            var cita = await CargarCitaAsync(idCita);
            if (cita == null) return NotFound();
            if (cita.IdMedico != medico.IdMedico)
                return Denegar("Esta cita está asignada a otro médico.");

            if (cita.EstadoCita?.NombreEstado != EstadosCita.EnConsulta)
                return Denegar($"Solo se puede atender una cita 'En Consulta'. Estado actual: '{cita.EstadoCita?.NombreEstado}'.");

            var vm = await ConstruirAtencionAsync(cita);
            return View(vm);
        }

        // POST: Medico/FinalizarConsulta
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarConsulta(AtencionFormViewModel model)
        {
            var medico = await ObtenerMedicoActualAsync();
            if (medico == null) return SinPerfil();

            var cita = await CargarCitaAsync(model.IdCita);
            if (cita == null) return NotFound();
            if (cita.IdMedico != medico.IdMedico)
                return Denegar("Esta cita está asignada a otro médico.");

            // RN-09 / RN-13: solo se finaliza desde "En Consulta".
            if (cita.EstadoCita?.NombreEstado != EstadosCita.EnConsulta)
                return Denegar($"No se puede finalizar una cita en estado '{cita.EstadoCita?.NombreEstado}'. " +
                               "Solo las citas 'En Consulta' pueden finalizarse.");

            if (!ModelState.IsValid)
            {
                await ConstruirAtencionAsync(cita, model);
                return View("Atender", model);
            }

            try
            {
                var estadoFinal = await ObtenerEstadoAsync(EstadosCita.Finalizada);

                var registro = await _context.RegistroAtencion.FirstOrDefaultAsync(r => r.IdCita == cita.IdCita);
                if (registro == null)
                {
                    registro = new RegistroAtencion { IdCita = cita.IdCita };
                    _context.RegistroAtencion.Add(registro);
                }

                registro.NotasConsulta = Limpiar(model.Notas);
                registro.Diagnostico = Limpiar(model.Diagnostico);
                registro.Indicaciones = Limpiar(model.Indicaciones);
                cita.IdEstadoCita = estadoFinal.IdEstadoCita;

                // Un solo SaveChanges: el registro y el cambio de estado se guardan juntos o no se guarda nada.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "No se pudo guardar la atención. Verifique los datos e intente de nuevo.");
                await ConstruirAtencionAsync(cita, model);
                return View("Atender", model);
            }

            TempData["Mensaje"] = $"Consulta del turno {cita.CodigoTurno} finalizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // No-Show
        // ------------------------------------------------------------------

        // POST: Medico/MarcarNoShow
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarNoShow(int idCita)
        {
            var medico = await ObtenerMedicoActualAsync();
            if (medico == null) return SinPerfil();

            var cita = await CargarCitaAsync(idCita);
            if (cita == null) return NotFound();
            if (cita.IdMedico != medico.IdMedico)
                return Denegar("Esta cita está asignada a otro médico.");

            // RN-12: solo si NO hizo check-in y ya pasó el tiempo de tolerancia.
            if (cita.EstadoCita?.NombreEstado != EstadosCita.Agendada)
                return Denegar($"Solo se puede marcar No-Show una cita 'Agendada' sin check-in. Estado actual: '{cita.EstadoCita?.NombreEstado}'.");

            var limite = cita.FechaHora.AddMinutes(ReglasCita.ToleranciaNoShowMin);
            if (DateTime.Now < limite)
                return Denegar($"Aún no puede marcarse como No-Show: debe esperar hasta las {limite:hh:mm tt} " +
                               $"({ReglasCita.ToleranciaNoShowMin} min después de la hora de la cita).");

            try
            {
                var estado = await ObtenerEstadoAsync(EstadosCita.NoShow);
                cita.IdEstadoCita = estado.IdEstadoCita;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Denegar("No se pudo registrar el No-Show. Intente de nuevo.");
            }

            TempData["Mensaje"] = $"El turno {cita.CodigoTurno} fue marcado como No-Show.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // Editar atención ya finalizada (RN-16: solo el médico que la creó; nunca se elimina)
        // ------------------------------------------------------------------

        // GET: Medico/Atencion?idCita=5
        [HttpGet]
        public async Task<IActionResult> Atencion(int idCita)
        {
            var medico = await ObtenerMedicoActualAsync();
            if (medico == null) return SinPerfil();

            var cita = await CargarCitaAsync(idCita);
            if (cita == null) return NotFound();
            if (cita.IdMedico != medico.IdMedico)
                return Denegar("Solo el médico que registró la atención puede consultarla o modificarla.");

            var registro = await _context.RegistroAtencion.AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdCita == idCita);
            if (registro == null)
                return Denegar("Esta cita todavía no tiene una atención registrada.");

            var vm = new AtencionFormViewModel
            {
                IdCita = idCita,
                Notas = registro.NotasConsulta,
                Diagnostico = registro.Diagnostico,
                Indicaciones = registro.Indicaciones
            };
            await ConstruirAtencionAsync(cita, vm);
            return View(vm);
        }

        // POST: Medico/Atencion
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Atencion(AtencionFormViewModel model)
        {
            var medico = await ObtenerMedicoActualAsync();
            if (medico == null) return SinPerfil();

            var cita = await CargarCitaAsync(model.IdCita);
            if (cita == null) return NotFound();
            if (cita.IdMedico != medico.IdMedico)
                return Denegar("Solo el médico que registró la atención puede modificarla.");

            var registro = await _context.RegistroAtencion.FirstOrDefaultAsync(r => r.IdCita == model.IdCita);
            if (registro == null)
                return Denegar("Esta cita todavía no tiene una atención registrada.");

            if (!ModelState.IsValid)
            {
                await ConstruirAtencionAsync(cita, model);
                return View(model);
            }

            try
            {
                registro.NotasConsulta = Limpiar(model.Notas);
                registro.Diagnostico = Limpiar(model.Diagnostico);
                registro.Indicaciones = Limpiar(model.Indicaciones);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "No se pudo guardar la modificación. Intente de nuevo.");
                await ConstruirAtencionAsync(cita, model);
                return View(model);
            }

            TempData["Mensaje"] = "Atención actualizada correctamente.";
            return RedirectToAction("Ver", "Expediente", new { id = cita.IdPaciente });
        }

        // ------------------------------------------------------------------
        // Métodos privados
        // ------------------------------------------------------------------

        private async Task<Medico?> ObtenerMedicoActualAsync()
        {
            var idUsuario = User.ObtenerIdUsuario();
            if (idUsuario == null) return null;

            return await _context.Medico
                .Include(m => m.Especialidad)
                .Include(m => m.Consultorio)
                .FirstOrDefaultAsync(m => m.IdUsuario == idUsuario.Value);
        }

        private IActionResult SinPerfil() =>
            StatusCode(StatusCodes.Status403Forbidden, "Su usuario no tiene un perfil de médico asociado. Contacte al administrador.");

        private IActionResult Denegar(string mensaje)
        {
            TempData["Error"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        private async Task<Cita?> CargarCitaAsync(int idCita)
        {
            return await _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.EstadoCita)
                .FirstOrDefaultAsync(c => c.IdCita == idCita);
        }

        private async Task<EstadoCita> ObtenerEstadoAsync(string nombre)
        {
            var estado = await _context.EstadoCita.FirstOrDefaultAsync(e => e.NombreEstado == nombre);
            if (estado == null)
            {
                estado = new EstadoCita { NombreEstado = nombre };
                _context.EstadoCita.Add(estado);
                await _context.SaveChangesAsync();
            }
            return estado;
        }

        private static string? Limpiar(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        private async Task<AtencionFormViewModel> ConstruirAtencionAsync(Cita cita, AtencionFormViewModel? origen = null)
        {
            var exp = await _context.Expediente.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdPaciente == cita.IdPaciente);

            var vm = origen ?? new AtencionFormViewModel();
            vm.IdCita = cita.IdCita;
            vm.IdPaciente = cita.IdPaciente;
            vm.CodigoTurno = cita.CodigoTurno;
            vm.Paciente = $"{cita.Paciente?.Nombre} {cita.Paciente?.Apellido}".Trim();
            vm.DUI = cita.Paciente?.DUI;
            vm.Telefono = cita.Paciente?.Telefono;
            vm.Motivo = cita.MotivoConsulta;
            vm.Estado = cita.EstadoCita?.NombreEstado;
            vm.FechaHora = cita.FechaHora;
            vm.GrupoSanguineo = exp?.GrupoSanguineo;
            vm.Alergias = exp?.Alergias;
            vm.Antecedentes = exp?.Antecedentes;
            return vm;
        }

        private async Task<MedicoPanelViewModel> ConstruirPanelAsync(Medico medico)
        {
            var hoy = DateTime.Today;
            var manana = hoy.AddDays(1);

            var citas = await _context.Cita.AsNoTracking()
                .Where(c => c.IdMedico == medico.IdMedico && c.FechaHora >= hoy && c.FechaHora < manana)
                .OrderBy(c => c.FechaHora)
                .Select(c => new CitaPanelItem
                {
                    IdCita = c.IdCita,
                    IdPaciente = c.IdPaciente,
                    CodigoTurno = c.CodigoTurno,
                    Paciente = c.Paciente!.Nombre + " " + c.Paciente.Apellido,
                    FechaHora = c.FechaHora,
                    Motivo = c.MotivoConsulta,
                    Estado = c.EstadoCita!.NombreEstado,
                    HoraCheckIn = c.HoraCheckIn
                })
                .ToListAsync();

            // Cola de espera: por orden de check-in (o por hora de cita si no hay check-in registrado).
            var cola = citas.Where(c => c.Estado == EstadosCita.EnEspera)
                            .OrderBy(c => c.HoraCheckIn ?? c.FechaHora)
                            .ToList();
            for (int i = 0; i < cola.Count; i++) cola[i].PosicionCola = i + 1;

            return new MedicoPanelViewModel
            {
                NombreMedico = $"Dr/a. {medico.Nombre} {medico.Apellido}",
                Especialidad = medico.Especialidad?.NombreEspecialidad ?? "Sin especialidad",
                Consultorio = medico.Consultorio?.NombreConsultorio ?? "Sin consultorio",
                Citas = citas,
                EnConsulta = citas.FirstOrDefault(c => c.Estado == EstadosCita.EnConsulta),
                Siguiente = cola.FirstOrDefault(),
                Total = citas.Count,
                EnEspera = cola.Count,
                Agendadas = citas.Count(c => c.Estado == EstadosCita.Agendada),
                Finalizadas = citas.Count(c => c.Estado == EstadosCita.Finalizada),
                NoShows = citas.Count(c => c.Estado == EstadosCita.NoShow)
            };
        }
    }
}
