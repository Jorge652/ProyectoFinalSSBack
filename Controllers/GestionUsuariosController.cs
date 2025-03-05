using ProyectoSS.Permisos;
using ProyectoSS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace ProyectoSS.Controllers
{
    public class GestionUsuariosController : Controller
    {
        static string cadena = "Data Source=ANDRES\\ANDRES;Initial Catalog=DB_ACCESO;Integrated Security=true";

        [ValidarSesion]
        [ValidarRol(1)] // Solo accesible por el admin
        public ActionResult EditarUsuarios()
        {
            List<Usuario> usuarios = new List<Usuario>();

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                SqlCommand cmd = new SqlCommand("SELECT IdUsuario, Nombres, FechaNacimiento, Correo FROM USUARIO WHERE Rol = 2", cn);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    usuarios.Add(new Usuario
                    {
                        IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                        Nombres = dr["Nombres"].ToString(),
                        FechaNacimiento = Convert.ToDateTime(dr["FechaNacimiento"]),
                        Correo = dr["Correo"].ToString()
                    });
                }
            }

            return View(usuarios);
        }

        [ValidarSesion]
        [ValidarRol(1)]
        [HttpGet]
        public ActionResult Editar(int id)
        {
            Usuario usuario = null;

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                SqlCommand cmd = new SqlCommand("SELECT IdUsuario, Nombres, FechaNacimiento, Correo FROM USUARIO WHERE IdUsuario = @IdUsuario AND Rol = 2", cn);
                cmd.Parameters.AddWithValue("@IdUsuario", id);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    usuario = new Usuario
                    {
                        IdUsuario = Convert.ToInt32(dr["IdUsuario"]),
                        Nombres = dr["Nombres"].ToString(),
                        FechaNacimiento = Convert.ToDateTime(dr["FechaNacimiento"]),
                        Correo = dr["Correo"].ToString()
                    };
                }
            }

            if (usuario == null)
            {
                return HttpNotFound();
            }

            return View(usuario);
        }

        [ValidarSesion]
        [ValidarRol(1)]
        [HttpPost]
        public ActionResult Editar(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection cn = new SqlConnection(cadena))
                {
                    SqlCommand cmd = new SqlCommand("UPDATE USUARIO SET Nombres = @Nombres, FechaNacimiento = @FechaNacimiento, Correo = @Correo WHERE IdUsuario = @IdUsuario AND Rol = 2", cn);
                    cmd.Parameters.AddWithValue("@Nombres", usuario.Nombres);
                    cmd.Parameters.AddWithValue("@FechaNacimiento", usuario.FechaNacimiento);
                    cmd.Parameters.AddWithValue("@Correo", usuario.Correo);
                    cmd.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);

                    cn.Open();
                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("EditarUsuarios");
            }

            return View(usuario);
        }
    }
}
