using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using ProyectoSS.Models;

using System.Data.SqlClient;
using System.Data;

namespace ProyectoSS.Controllers
{
    public class AccesoController : Controller
    {
        //static string cadena = "Data Source=(ANDRES\ANDRES);Initial Catalog=DB_ACCESO;Integrated Security=true";
        static string cadena = "Data Source=ANDRES\\ANDRES;Initial Catalog=DB_ACCESO;Integrated Security=true";



        // GET: Acceso
        public ActionResult Login()
        {
            return View();
        }


        public ActionResult Registrar()
        {
            return View();
        }


        [HttpPost]
        public ActionResult Registrar(Usuario oUsuario)
        {
            bool registrado;
            string mensaje;

            if (oUsuario.Clave == oUsuario.ConfirmarClave)
            {
                oUsuario.Clave = ConvertirSha256(oUsuario.Clave);
            }
            else
            {
                ViewData["Mensaje"] = "Las contraseñas no coinciden";
                return View();
            }

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                SqlCommand cmd = new SqlCommand("sp_RegistrarUsuario", cn);
                cmd.Parameters.AddWithValue("Correo", oUsuario.Correo);
                cmd.Parameters.AddWithValue("Clave", oUsuario.Clave);
                cmd.Parameters.AddWithValue("Nombres", oUsuario.Nombres);
                cmd.Parameters.AddWithValue("FechaNacimiento", oUsuario.FechaNacimiento); // Agregamos la fecha de nacimiento
                cmd.Parameters.AddWithValue("Rol", 2); // Por defecto, usuario normal

                cmd.Parameters.Add("Registrado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 100).Direction = ParameterDirection.Output;
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                cmd.ExecuteNonQuery();

                registrado = Convert.ToBoolean(cmd.Parameters["Registrado"].Value);
                mensaje = cmd.Parameters["Mensaje"].Value.ToString();
            }

            ViewData["Mensaje"] = mensaje;

            if (registrado)
            {
                return RedirectToAction("Login", "Acceso");
            }
            else
            {
                return View();
            }
        }


        [HttpPost]
        public ActionResult Login(Usuario oUsuario)
        {
            oUsuario.Clave = ConvertirSha256(oUsuario.Clave);

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                SqlCommand cmd = new SqlCommand("sp_ValidarUsuario", cn);
                cmd.Parameters.AddWithValue("Correo", oUsuario.Correo);
                cmd.Parameters.AddWithValue("Clave", oUsuario.Clave);
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    oUsuario.IdUsuario = Convert.ToInt32(dr["IdUsuario"]);
                    oUsuario.Nombres = dr["Nombres"].ToString();
                    oUsuario.FechaNacimiento = Convert.ToDateTime(dr["FechaNacimiento"]);
                    oUsuario.Rol = Convert.ToInt32(dr["Rol"]);
                    oUsuario.IngresosMensuales = dr["IngresosMensuales"] != DBNull.Value ? Convert.ToDecimal(dr["IngresosMensuales"]) : 0;

                    Session["usuario"] = oUsuario;
                    Session["rol"] = oUsuario.Rol;

                    // Redirigir según el rol
                    if (oUsuario.Rol == 1)
                    {
                        return RedirectToAction("EditarUsuarios", "GestionUsuarios");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ViewData["Mensaje"] = "Usuario no encontrado";
                    return View();
                }
            }
        }




        public static string ConvertirSha256(string texto)
        {
            //using System.Text;
            //USAR LA REFERENCIA DE "System.Security.Cryptography"

            StringBuilder Sb = new StringBuilder();
            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(texto));

                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }

    }
}