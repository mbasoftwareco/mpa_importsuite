using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class MASTERController : BaseController
    {

      

        #region :: PARAMETERS

        // GET: PROFILE
        public ActionResult PARAMETERS()
        {
            List<S_PARAMETER> list = _web._dbx.S_PARAMETERs.Where(f => f.EDIT == true).OrderBy(f => f.SEQUENCE).ToList();
            ViewBag.KEY = "";

            if (!string.IsNullOrEmpty(Request["KEY"]))
            {
                list = list.Where(f => f.CODE.StartsWith(Request["KEY"].ToString())).ToList();
                ViewBag.KEY = Request["KEY"];
            }
            
            return View(list);
        }

        [HttpPost]
        public ActionResult PARAMETERS(FormCollection form)
        {

            try
            {
                foreach (var parm in _web._dbx.S_PARAMETERs.Where(f => f.EDIT == true).OrderBy(f => f.LABEL).ToList())
                    UtilTool.ActualizarParametro(parm.CODE, form[parm.CODE], curConnection);

                TempData["Success"] = UtilTool.ObtenerParametro(Constantes.PARM_MSG_COMPLETED, curConnection);

            }
            catch (Exception e)
            {
                TempData["Error"] = e.Message;
            }
            
            return RedirectToAction("PARAMETERS", new RouteValueDictionary(
                    new { controller = "MASTER", action = "PARAMETERS", KEY = Request["KEY"] }));

        }

        #endregion


    }
}