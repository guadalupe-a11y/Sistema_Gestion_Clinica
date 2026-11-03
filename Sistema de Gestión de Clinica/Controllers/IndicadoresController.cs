
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_Gestión_Clinica.Data;
using Sistema_Gestión_Clinica.ViewModels;

namespace Sistema_Gestión_Clinica.Controllers
{
    public class IndicadoresController : Controller
    {
        private readonly SistemaDeGestionDB _context;

        public IndicadoresController(
            SistemaDeGestionDB context)
        {
            _context = context;
        }

        // GET: Indicadores/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            DateTime hoy = DateTime.Today;
            DateTime manana = hoy.AddDays(1);

            var resumen = await _context.Cita
                .Where(c =>
                    c.FechaHora >= hoy &&
                    c.FechaHora < manana)
                .GroupBy(c => 1)
                .Select(g => new IndicadoresViewModel
                {
                    Fecha = hoy,

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

            var modelo = resumen ?? new IndicadoresViewModel
            {
                Fecha = hoy
            };

            return View(modelo);
        }
    }
}
