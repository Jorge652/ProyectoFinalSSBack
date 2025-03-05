using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoSS.Models
{
    public class Amortizacion
    {
        public int NumeroPago { get; set; }
        public decimal Cuota { get; set; }
        public decimal Interes { get; set; }
        public decimal Capital { get; set; }
        public decimal Saldo { get; set; }
    }
}