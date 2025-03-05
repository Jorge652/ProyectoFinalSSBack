using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ProyectoSS.Models
{
    public class Prestamo
    {
        public int Id { get; set; }
        public int idUsuario { get; set; }
        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Range(1, 1000000, ErrorMessage = "El monto debe ser mayor que 0.")]
        public decimal Monto { get; set; }
        public int Plazo { get; set; } // En meses
        public decimal TasaInteres { get; set; }
        public string TipoPago { get; set; } // "Fijo" o "Decreciente"
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; } // "Pendiente", "Aprobado", "Pagado", "Cancelado"
        public decimal SaldoPendiente { get; set; }
    }
}