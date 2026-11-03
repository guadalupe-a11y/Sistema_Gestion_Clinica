
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    public class IndicadoresController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public IndicadoresController(SistemaDeGestionDB context)
        {
            _context = context;
        }

        // GET: Indicadores/Index?fecha=2026-11-02
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fecha)
        {
            // Utilizar la fecha elegida o la fecha actual
            DateTime fechaSeleccionada =
                fecha?.Date ?? DateTime.Today;

            // Límite superior del filtro
            DateTime fechaSiguiente =
                fechaSeleccionada.AddDays(1);

            // Consultar únicamente las citas de la fecha seleccionada
            var resumen = await _context.Cita
                .Where(c =>
                    c.FechaHora >= fechaSeleccionada &&
                    c.FechaHora < fechaSiguiente)
                .GroupBy(c => 1)
                .Select(g => new IndicadoresViewModel
                {
                    Fecha = fechaSeleccionada,

                    TotalCitas = g.Count(),

                    CitasPendientes = g.Count(c =>
                        c.EstadoCita != null &&
                        c.EstadoCita.NombreEstado == "Agendada"),

                    PacientesEnEspera = g.Count(c =>
                        c.EstadoCita != null &&
                        c.EstadoCita.NombreEstado == "En Espera"),

                    CitasFinalizadas = g.Count(c =>
                        c.EstadoCita != null &&
                        c.EstadoCita.NombreEstado == "Finalizada"),

                    CitasCanceladas = g.Count(c =>
                        c.EstadoCita != null &&
                        c.EstadoCita.NombreEstado == "Cancelada"),

                    PacientesNoPresentados = g.Count(c =>
                        c.EstadoCita != null &&
                        c.EstadoCita.NombreEstado == "No-Show")
                })
                .FirstOrDefaultAsync();

            // Si no hay citas, mostrar los indicadores en cero
            var modelo = resumen ?? new IndicadoresViewModel
            {
                Fecha = fechaSeleccionada
            };

            return View(modelo);
        }
    }
}
