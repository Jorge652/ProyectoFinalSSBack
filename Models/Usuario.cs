using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoSS.Models
{
	public class Usuario
	{
		public int IdUsuario { get; set; }
		public string Correo { get; set; }
		public string Clave { get; set; }

        public string ConfirmarClave { get; set; }

        // Agregar nuevos campos
        public string Nombres { get; set; }  // Nombres del usuario
        public DateTime FechaNacimiento { get; set; }  // Fecha de nacimiento

        // Agregar IngresosMensuales
        public decimal IngresosMensuales { get; set; }

        public int Rol { get; set; }  // Rol del usuario (puede ser 1 para admin, 2 para usuario normal, etc.)

        // Campos para manejar el resultado del registro
        public bool Registrado { get; set; }  // Indica si el registro fue exitoso o no
        public string Mensaje { get; set; }   // Mensaje de retroalimentación sobre el estado del registro
    }
}