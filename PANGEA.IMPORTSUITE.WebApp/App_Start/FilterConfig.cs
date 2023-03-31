using PANGEA.IMPORTSUITE.WebApp.Controllers;
using System.Web;
using System.Web.Mvc;

namespace PANGEA.IMPORTSUITE.WebApp
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new CheckSessionOutAttribute());
            
        }
    }
}
