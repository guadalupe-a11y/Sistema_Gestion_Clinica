using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_Gestión_Clinica.Models;

namespace Sistema_Gestión_Clinica.ViewModels
{
    public class MedicosViewModel
    {
        public List<Medico> Medicos { get; set; } = new List<Medico>();
        public List<SelectListItem> Especialidades { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Consultorios { get; set; } = new List<SelectListItem>();
    }
}