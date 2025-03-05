using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ProyectoSS.Permisos
{
    public class ValidarRol : ActionFilterAttribute
    {
        private readonly int _rolRequerido;

        public ValidarRol(int rolRequerido)
        {
            _rolRequerido = rolRequerido;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var usuario = (ProyectoSS.Models.Usuario)HttpContext.Current.Session["usuario"];

            if (usuario == null || usuario.Rol != _rolRequerido)
            {
                // Si no tiene el rol requerido, redirigirlo a Home/Index
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    }
                );
            }

            base.OnActionExecuting(filterContext);
        }
    }
}