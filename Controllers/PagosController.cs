using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using ProyectoSS.Models;
using ProyectoSS.Permisos;

namespace ProyectoSS.Controllers
{
    [ValidarSesion]
    public class PagosController : Controller
    {
        static string cadena = "Data Source=ANDRES\\ANDRES;Initial Catalog=DB_ACCESO;Integrated Security=true";

        // GET: Pagos
        public ActionResult Index()
        {
            var usuario = (Usuario)Session["usuario"];
            List<Pago> pagos = new List<Pago>();

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = cn;
                // Si el usuario es administrador, se muestran todos los pagos; de lo contrario, solo los del usuario.
                if (usuario.Rol == 1)
                {
                    cmd.CommandText = "SELECT * FROM PAGOS";
                }
                else
                {
                    cmd.CommandText = "SELECT * FROM PAGOS WHERE UsuarioId = @UsuarioId";
                    cmd.Parameters.AddWithValue("@UsuarioId", usuario.IdUsuario);
                }

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Pago pago = new Pago()
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        PrestamoId = dr["PrestamoId"] != DBNull.Value ? Convert.ToInt32(dr["PrestamoId"]) : 0,
                        UsuarioId = dr["UsuarioId"] != DBNull.Value ? Convert.ToInt32(dr["UsuarioId"]) : 0,
                        Monto = dr["Monto"] != DBNull.Value ? Convert.ToDecimal(dr["Monto"]) : 0.0m,
                        FechaPago = dr["FechaPago"] != DBNull.Value ? Convert.ToDateTime(dr["FechaPago"]) : DateTime.MinValue
                    };
                    pagos.Add(pago);
                }
            }
            return View(pagos);
        }

        // GET: Pagos/Details/5
        public ActionResult Details(int id)
        {
            Pago pago = null;
            using (SqlConnection cn = new SqlConnection(cadena))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM PAGOS WHERE Id = @Id", cn);
                cmd.Parameters.AddWithValue("@Id", id);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    pago = new Pago()
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        PrestamoId = Convert.ToInt32(dr["PrestamoId"]),
                        UsuarioId = Convert.ToInt32(dr["UsuarioId"]),
                        Monto = Convert.ToDecimal(dr["Monto"]),
                        FechaPago = Convert.ToDateTime(dr["FechaPago"])
                    };
                }
            }
            if (pago == null)
                return HttpNotFound();
            return View(pago);
        }

        // GET: Pagos/Create
        public ActionResult Create(int prestamoId)
        {
            var usuario = (Usuario)Session["usuario"];
            if (usuario == null)
                return RedirectToAction("Login", "Acceso");

            Pago pago = new Pago
            {
                PrestamoId = prestamoId,
                UsuarioId = usuario.IdUsuario,
                FechaPago = DateTime.Now
            };

            ViewBag.PrestamoId = prestamoId;

            return View(pago);
        }

        // POST: Pagos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Pago pago)
        {
            var usuario = (Usuario)Session["usuario"];
            if (usuario == null)
                return RedirectToAction("Login", "Acceso");

            if (!ModelState.IsValid)
                return View(pago);

            // Asignar la fecha actual al pago
            pago.FechaPago = DateTime.Now;

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                cn.Open();

                // Verificar si el usuario existe en la tabla USUARIO
                SqlCommand cmdVerificarUsuario = new SqlCommand("SELECT COUNT(*) FROM USUARIO WHERE IdUsuario = @UsuarioId", cn);
                cmdVerificarUsuario.Parameters.AddWithValue("@UsuarioId", usuario.IdUsuario);
                int usuarioExiste = (int)cmdVerificarUsuario.ExecuteScalar();

                if (usuarioExiste == 0)
                {
                    ModelState.AddModelError("", "El usuario no existe.");
                    return View(pago);
                }

                // Buscar el préstamo más reciente que esté en estado "Pendiente" y que aún tenga saldo por pagar
                SqlCommand cmd = new SqlCommand(
                    "SELECT TOP 1 IdPrestamo, SaldoPendiente FROM PRESTAMOS " +
                    "WHERE IdUsuario = @UsuarioId AND Estado = 'Pendiente' AND SaldoPendiente > 0 " +
                    "ORDER BY IdPrestamo DESC", cn);
                cmd.Parameters.AddWithValue("@UsuarioId", usuario.IdUsuario);

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        ModelState.AddModelError("", "No tienes préstamos pendientes para realizar pagos.");
                        return View(pago);
                    }

                    // Asignar el préstamo correcto
                    pago.PrestamoId = Convert.ToInt32(dr["IdPrestamo"]);
                    decimal saldoPendiente = dr["SaldoPendiente"] != DBNull.Value ? Convert.ToDecimal(dr["SaldoPendiente"]) : 0.0m;

                    dr.Close();

                    // Validar que aún haya saldo por pagar
                    if (saldoPendiente <= 0)
                    {
                        ModelState.AddModelError("", "El préstamo ya ha sido pagado en su totalidad.");
                        return View(pago);
                    }

                    // Verificar que el pago no sea mayor al saldo pendiente
                    if (pago.Monto > saldoPendiente)
                    {
                        ModelState.AddModelError("", "El monto del pago excede el saldo pendiente.");
                        return View(pago);
                    }

                    // Insertar el pago en la base de datos
                    SqlCommand cmdInsertar = new SqlCommand(
                        "INSERT INTO PAGOS (PrestamoId, UsuarioId, Monto, FechaPago) VALUES (@PrestamoId, @UsuarioId, @Monto, @FechaPago)", cn);
                    cmdInsertar.Parameters.AddWithValue("@PrestamoId", pago.PrestamoId);
                    cmdInsertar.Parameters.AddWithValue("@UsuarioId", usuario.IdUsuario);
                    cmdInsertar.Parameters.AddWithValue("@Monto", pago.Monto);
                    cmdInsertar.Parameters.AddWithValue("@FechaPago", pago.FechaPago);
                    cmdInsertar.ExecuteNonQuery();

                    // Actualizar el saldo pendiente del préstamo
                    decimal nuevoSaldo = saldoPendiente - pago.Monto;
                    SqlCommand cmdActualizar = new SqlCommand("UPDATE PRESTAMOS SET SaldoPendiente = @SaldoPendiente WHERE IdPrestamo = @PrestamoId", cn);
                    cmdActualizar.Parameters.AddWithValue("@SaldoPendiente", nuevoSaldo);
                    cmdActualizar.Parameters.AddWithValue("@PrestamoId", pago.PrestamoId);
                    cmdActualizar.ExecuteNonQuery();

                    // Si el saldo pendiente es 0, cambiar el estado del préstamo a "Pagado"
                    if (nuevoSaldo == 0)
                    {
                        SqlCommand cmdFinalizar = new SqlCommand("UPDATE PRESTAMOS SET Estado = 'Pagado' WHERE IdPrestamo = @PrestamoId", cn);
                        cmdFinalizar.Parameters.AddWithValue("@PrestamoId", pago.PrestamoId);
                        cmdFinalizar.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToAction("Index");
        }


        // GET: Pagos/Delete/5
        public ActionResult Delete(int id)
        {
            Pago pago = null;
            using (SqlConnection cn = new SqlConnection(cadena))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM PAGOS WHERE Id = @Id", cn);
                cmd.Parameters.AddWithValue("@Id", id);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    pago = new Pago()
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        PrestamoId = Convert.ToInt32(dr["PrestamoId"]),
                        UsuarioId = Convert.ToInt32(dr["UsuarioId"]),
                        Monto = Convert.ToDecimal(dr["Monto"]),
                        FechaPago = Convert.ToDateTime(dr["FechaPago"])
                    };
                }
            }
            if (pago == null) return HttpNotFound();
            return View(pago);
        }

        // POST: Pagos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection cn = new SqlConnection(cadena))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM PAGOS WHERE Id = @Id", cn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }
    }
}