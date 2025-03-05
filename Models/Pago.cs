using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoSS.Models
{
    public class Pago
    {
        public int Id { get; set; }

        public int PrestamoId { get; set; }

        public int UsuarioId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }

        public virtual Prestamo Prestamo { get; set; }
        public virtual Usuario Usuario { get; set; }


    }
}