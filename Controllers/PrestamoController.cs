using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using ProyectoSS.Models;
using ProyectoSS.Permisos;

namespace ProyectoSS.Controllers
{
    [ValidarSesion]
    public class PrestamosController : Controller
    {
        static string cadena = "Data Source=ANDRES\\ANDRES;Initial Catalog=DB_ACCESO;Integrated Security=true";

        // GET: Prestamos
        public ActionResult Index()
        {
            var usuario = (Usuario)Session["usuario"];
            List<Prestamo> prestamos = new List<Prestamo>();

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = cn;
                // Si el usuario es administrador, se muestran todos los préstamos; de lo contrario, solo los del usuario.
                if (usuario.Rol == 1)
                {
                    cmd.CommandText = "SELECT * FROM PRESTAMOS";
                }
                else
                {
                    cmd.CommandText = "SELECT * FROM PRESTAMOS WHERE IdUsuario = @IdUsuario";
                    cmd.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
                }

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Prestamo prestamo = new Prestamo()
                    {
                        Id = Convert.ToInt32(dr["IdPrestamo"]),
                        idUsuario = Convert.ToInt32(dr["IdUsuario"]),
                        Monto = Convert.ToDecimal(dr["Monto"]),
                        Plazo = Convert.ToInt32(dr["PlazoMeses"]),
                        TasaInteres = Convert.ToDecimal(dr["TasaInteres"]),
                        TipoPago = dr["TipoPago"].ToString(),
                        FechaSolicitud = Convert.ToDateTime(dr["FechaSolicitud"]),
                        Estado = dr["Estado"].ToString(),
                        SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"])

                    };
                    prestamos.Add(prestamo);
                }
            }
            return View(prestamos);
        }

        // GET: Prestamos/Details/5
        public ActionResult Details(int id)
        {
            Prestamo prestamo = null;

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM PRESTAMOS WHERE IdPrestamo = @Id", cn);
                cmd.Parameters.AddWithValue("@Id", id);

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    prestamo = new Prestamo
                    {
                        Id = Convert.ToInt32(dr["IdPrestamo"]),
                        idUsuario = Convert.ToInt32(dr["IdUsuario"]),
                        Monto = Convert.ToDecimal(dr["Monto"]),
                        Plazo = Convert.ToInt32(dr["PlazoMeses"]),
                        TasaInteres = Convert.ToDecimal(dr["TasaInteres"]),
                        TipoPago = dr["TipoPago"].ToString(),
                        FechaSolicitud = Convert.ToDateTime(dr["FechaSolicitud"]),
                        Estado = dr["Estado"].ToString(),
                        SaldoPendiente = Convert.ToDecimal(dr["SaldoPendiente"])
                    };
                }
            }

            if (prestamo == null)
            {
                return HttpNotFound();
            }

            // Generar la tabla de amortización
            var tablaAmortizacion = GenerarTablaAmortizacion(prestamo);
            ViewBag.TablaAmortizacion = tablaAmortizacion;

            return View(prestamo);
        }
        private List<Amortizacion> GenerarTablaAmortizacion(Prestamo prestamo)
        {
            List<Amortizacion> tabla = new List<Amortizacion>();

            decimal saldoPendiente = prestamo.Monto;
            decimal tasaMensual = prestamo.TasaInteres / 100 / 12;
            decimal cuotaMensual = prestamo.TipoPago == "Fijo"
                ? (prestamo.Monto * tasaMensual) / (1 - (decimal)Math.Pow(1 + (double)tasaMensual, -prestamo.Plazo))
                : 0;

            for (int i = 1; i <= prestamo.Plazo; i++)
            {
                decimal interes = saldoPendiente * tasaMensual;
                decimal capital = prestamo.TipoPago == "Fijo" ? cuotaMensual - interes : prestamo.Monto / prestamo.Plazo;
                decimal pagoTotal = capital + interes;

                saldoPendiente -= capital;

                tabla.Add(new Amortizacion
                {
                    NumeroPago = i,
                    FechaPago = prestamo.FechaSolicitud.AddMonths(i),
                    PagoTotal = Math.Round(pagoTotal, 2),
                    InteresPagado = Math.Round(interes, 2),
                    CapitalPagado = Math.Round(capital, 2),
                    SaldoPendiente = Math.Round(saldoPendiente, 2)
                });
            }

            return tabla;
        }


        // GET: Prestamos/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Prestamos/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Prestamo prestamo)
        {
            var usuario = (Usuario)Session["usuario"];

            if (usuario == null)
            {
                return RedirectToAction("Login", "Acceso");
            }

            // Validar que el usuario tiene ingresos mensuales
            if (usuario.IngresosMensuales <= 0)
            {
                ModelState.AddModelError("", "No puede solicitar un préstamo sin ingresos mensuales registrados.");
                return View(prestamo);
            }

            using (SqlConnection cn = new SqlConnection(cadena))
            {
                cn.Open();

                // Validar si el usuario ya tiene un préstamo pendiente
                SqlCommand cmdVerificarPendiente = new SqlCommand(
                    "SELECT COUNT(*) FROM PRESTAMOS WHERE IdUsuario = @IdUsuario AND Estado = 'Pendiente'", cn);
                cmdVerificarPendiente.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
                int prestamosPendientes = (int)cmdVerificarPendiente.ExecuteScalar();

                if (prestamosPendientes > 0)
                {
                    ModelState.AddModelError("", "No puede solicitar un nuevo préstamo hasta pagar el actual.");
                    return View(prestamo);
                }

                // Validar si el usuario ha tenido préstamos pagados
                SqlCommand cmdVerificarPagado = new SqlCommand(
                    "SELECT COUNT(*) FROM PRESTAMOS WHERE IdUsuario = @IdUsuario AND Estado = 'Pagado'", cn);
                cmdVerificarPagado.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
                int prestamosPagados = (int)cmdVerificarPagado.ExecuteScalar();

                if (prestamosPagados == 0)
                {
                    ModelState.AddModelError("", "Debe haber pagado al menos un préstamo antes de solicitar otro.");
                    return View(prestamo);
                }

                // Validaciones de monto y plazo
                decimal limiteMaximo = usuario.IngresosMensuales * 5;

                if (prestamo.Monto <= 0 || prestamo.Monto > limiteMaximo)
                {
                    ModelState.AddModelError("", "El monto solicitado es inválido o excede su límite de crédito.");
                    return View(prestamo);
                }

                if (prestamo.Plazo <= 0)
                {
                    ModelState.AddModelError("", "El plazo del préstamo debe ser mayor a 0.");
                    return View(prestamo);
                }

                if (prestamo.TasaInteres <= 0 || prestamo.TasaInteres > 100)
                {
                    ModelState.AddModelError("", "La tasa de interés debe estar entre 0.1% y 100%.");
                    return View(prestamo);
                }

                // Insertar el nuevo préstamo
                prestamo.FechaSolicitud = DateTime.Now;
                SqlCommand cmdInsertar = new SqlCommand(
                    "INSERT INTO PRESTAMOS (IdUsuario, Monto, PlazoMeses, TasaInteres, TipoPago, FechaSolicitud, Estado, SaldoPendiente) " +
                    "VALUES (@IdUsuario, @Monto, @PlazoMeses, @TasaInteres, @TipoPago, @FechaSolicitud, 'Pendiente', @SaldoPendiente)", cn);

                cmdInsertar.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
                cmdInsertar.Parameters.AddWithValue("@Monto", prestamo.Monto);
                cmdInsertar.Parameters.AddWithValue("@PlazoMeses", prestamo.Plazo);
                cmdInsertar.Parameters.AddWithValue("@TasaInteres", prestamo.TasaInteres);
                cmdInsertar.Parameters.AddWithValue("@TipoPago", prestamo.TipoPago);
                cmdInsertar.Parameters.AddWithValue("@FechaSolicitud", prestamo.FechaSolicitud);
                cmdInsertar.Parameters.AddWithValue("@SaldoPendiente", prestamo.Monto);
                cmdInsertar.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }        
        

       

    public class Amortizacion
    {
        public int NumeroPago { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal PagoTotal { get; set; }
        public decimal InteresPagado { get; set; }
        public decimal CapitalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
    }
}
