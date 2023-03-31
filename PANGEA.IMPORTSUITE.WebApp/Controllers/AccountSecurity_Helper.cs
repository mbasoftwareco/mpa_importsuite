using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PANGEA.IMPORTSUITE.DataModel;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]


    public class CheckSessionOutAttribute : ActionFilterAttribute
    {

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {

            string controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName.ToLower();

            HttpSessionStateBase session = filterContext.HttpContext.Session;

            S_USER user = (S_USER)session["curUser"];


            //VALIDACIONES DE LOGIN
            if (!controllerName.Contains("home"))
            {

                var userMenu = session["curUser"];

                if (((user == null || userMenu == null) && (!session.IsNewSession)) || (session.IsNewSession))
                {
                    //send them off to the login page
                    var url = new UrlHelper(filterContext.RequestContext);
                    var loginUrl = url.Content("~/Home/Login");
                    filterContext.HttpContext.Response.Redirect(loginUrl, true);

                }
            }

            base.OnActionExecuting(filterContext);
        }

    }


    public class NoCache : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            filterContext.HttpContext.Response.Cache.SetValidUntilExpires(false);
            filterContext.HttpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            filterContext.HttpContext.Response.Cache.SetNoStore();

            base.OnResultExecuting(filterContext);
        }
    }


    public class AllowCrossSiteJsonAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Response != null)
                filterContext.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            base.OnResultExecuting(filterContext);
        }
    }

}