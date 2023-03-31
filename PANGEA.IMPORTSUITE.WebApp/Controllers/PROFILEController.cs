using PANGEA.IMPORTSUITE.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class PROFILEController : BaseController
    {

        #region :: USERS

        // GET: PROFILE
        public ActionResult USERS()
        {
            return View(_web._dbx.S_USERs.OrderBy(f => f.USERNAME).ToList());
        }

        [NoCache]
        public JsonResult SYS_USER_ACTIVE(int ROWID)
        {

            try
            {
                S_USER USER = _web._dbx.S_USERs.Where(f => f.ROWID == ROWID).First();


                if (USER.ACTIVE == true)
                    USER.ACTIVE = false;
                else
                    USER.ACTIVE = true;

                USER.MODIFIEDBY = curUser.USERNAME;
                USER.MODIFIEDON = DateTime.Now;


                _web._dbx.SubmitChanges();


                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }


        public ActionResult NEW_USER(int? ROWID)
        {

            S_USER USER = _web._dbx.S_USERs.Where(f => f.ROWID == ROWID).FirstOrDefault();

            if (USER == null)
                USER = new S_USER();

            ViewBag.ROLES = _web._dbx.M_ROLs.Where(f => f.ACTIVE == true).ToList();

            return PartialView(USER);
        }

        [HttpPost]
        [NoCache]
        public JsonResult NEW_USER(int? ROWID, string USERNAME, int ROWID_ROL, string ACTIVE)
        {

            try
            {
                S_USER USER = _web._dbx.S_USERs.Where(f => f.ROWID == ROWID).FirstOrDefault();
                bool nuevo = false;

                if (USER == null)
                {
                    USER = new S_USER();
                    nuevo = true;
                    USER.CREATEDON = DateTime.Now;
                    USER.CREATEDBY = curUser.USERNAME;
                }
                else
                {
                    USER.CREATEDON = DateTime.Now;
                    USER.MODIFIEDBY = curUser.USERNAME;
                }

                USER.USERNAME = USERNAME;
                USER.ROWID_ROL = ROWID_ROL;
                USER.ACTIVE = (ACTIVE == "on") ? true : false;

                if (nuevo)
                    _web._dbx.S_USERs.InsertOnSubmit(USER);

                _web._dbx.SubmitChanges();


                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }

        #endregion




        #region :: ROLES

        public ActionResult ROLES()
        {
            return View(_web._dbx.M_ROLs.OrderBy(f => f.NAME).ToList());
        }

        [NoCache]
        public JsonResult SYS_ROLES_ACTIVE(int ROWID)
        {

            try
            {
                M_ROL ROL = _web._dbx.M_ROLs.Where(f => f.ROWID == ROWID).First();


                if (ROL.ACTIVE == true)
                    ROL.ACTIVE = false;
                else
                    ROL.ACTIVE = true;

                ROL.MODIFIEDBY = curUser.USERNAME;
                ROL.MODIFIEDON = DateTime.Now;


                _web._dbx.SubmitChanges();


                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }

        #endregion




        #region :: MENU


        public ActionResult MENU()
        {
            return View(_web._dbx.S_MENUs.OrderBy(f => f.NAME).ToList());
        }

        [NoCache]
        public JsonResult SYS_MENU_DESCRIPTION(int ROWID, string DESCRIPTION)
        {

            try
            {

                S_MENU MENU = _web._dbx.S_MENUs.Where(f => f.ROWID == ROWID).First();

                MENU.DESCRIPTION = DESCRIPTION;
                MENU.MODIFIEDBY = curUser.USERNAME;
                MENU.MODIFIEDON = DateTime.Now;


                _web._dbx.SubmitChanges();


                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }

        [NoCache]
        public JsonResult SYS_MENU_ACTIVE(int ROWID)
        {

            try
            {

                S_MENU MENU = _web._dbx.S_MENUs.Where(f => f.ROWID == ROWID).First();


                if (MENU.ACTIVE == true)
                    MENU.ACTIVE = false;
                else
                    MENU.ACTIVE = true;

                MENU.MODIFIEDBY = curUser.USERNAME;
                MENU.MODIFIEDON = DateTime.Now;


                _web._dbx.SubmitChanges();


                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }



        #region ROL MENU
        [NoCache]
        public ActionResult ROL_MENU(int ROWID_ROL)
        {

            M_ROL ROL = _web._dbx.M_ROLs.Where(f => f.ROWID == ROWID_ROL).First();

            //listado de las opciones del Rol
            List<S_MENU> menuOpcionesRol = new List<S_MENU>();

            foreach (S_MENU_ROL curOpcionMenuR in _web._dbx.S_MENU_ROLs.Where(x => x.ROWID_ROL == ROWID_ROL).OrderBy(f => f.S_MENU.SEQ))
                menuOpcionesRol.Add(curOpcionMenuR.S_MENU);

            //listado de todas las opciones disponibles
            List<S_MENU> menuOpcionesDisponibles = new List<S_MENU>();

            foreach (S_MENU curOpM in _web._dbx.S_MENUs.OrderBy(f => f.SEQ).ToList())
                if (!menuOpcionesRol.Any(f => f.ROWID == curOpM.ROWID))
                    menuOpcionesDisponibles.Add(curOpM);


            ViewBag.NombreRol = ROL.NAME;
            ViewBag.RolID = ROL.ROWID;
            ViewBag.MenuOpcionesDisponibles = menuOpcionesDisponibles.OrderBy(f => f.SEQ);
            ViewBag.MenuOpcionesRol = menuOpcionesRol;

            return View();
        }


        [HttpGet]
        public JsonResult OPTION_MENU_PROFILE(int ROWID_ROL)
        {
            try
            {
                // Agregar y Elmininar Rol_opcion Menu

                if (Request.Params["oper"] == "add")
                {
                    S_MENU_ROL newrop = new S_MENU_ROL
                    {

                        ROWID_MENU = int.Parse(Request.Params["opcionMenuId"]),
                        ROWID_ROL = ROWID_ROL,
                        CREATEDBY = curUser.USERNAME,
                        CREATEDON = DateTime.Now
                    };

                    _web._dbx.S_MENU_ROLs.InsertOnSubmit(newrop);
                    _web._dbx.SubmitChanges();

                    return Json("OK", JsonRequestBehavior.AllowGet);
                }

                else if (Request.Params["oper"] == "delete")
                {

                    S_MENU_ROL rop = _web._dbx.S_MENU_ROLs.FirstOrDefault(x => x.ROWID_MENU == int.Parse(Request.Params["opcionMenuId"]) && x.ROWID_ROL == ROWID_ROL);

                    _web._dbx.S_MENU_ROLs.DeleteOnSubmit(rop);
                    _web._dbx.SubmitChanges();

                    return Json("OK", JsonRequestBehavior.AllowGet);

                }
                else
                    return Json("OK", JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }
        #endregion



        #region USER MENU
        [NoCache]
        public ActionResult USER_MENU(string USERNAME)
        {

            S_USER USER = _web._dbx.S_USERs.Where(f => f.USERNAME == USERNAME).First();

            //listado de las opciones del Rol
            List<S_MENU> menuOpcionesUsuario = new List<S_MENU>();

            foreach (CFG_07_AX_USERPROFILE curOpcionMenuR in _web._dbx.CFG_07_AX_USERPROFILEs.Where(x => x.USERID == USERNAME && x.MENUOPTION != null).OrderBy(f => f.MENUOPTION))
            {
                //menuOpcionesUsuario.Add(curOpcionMenuR.S_MENU);
                menuOpcionesUsuario.Add(_web._dbx.S_MENUs.Where(f=>f.ROWID == curOpcionMenuR.MENUOPTION ).First());
            }

            //listado de todas las opciones disponibles
            List<S_MENU> menuOpcionesDisponibles = new List<S_MENU>();

            foreach (S_MENU curOpM in _web._dbx.S_MENUs.Where(f=>f.ACTIVE == true).OrderBy(f => f.SEQ).ToList())
                if (!menuOpcionesUsuario.Any(f => f.ROWID == curOpM.ROWID))
                    menuOpcionesDisponibles.Add(curOpM);


            ViewBag.NombreUsuario = USER.USERNAME;
            ViewBag.UserID = USER.USERNAME;
            ViewBag.MenuOpcionesDisponibles = menuOpcionesDisponibles.OrderBy(f => f.SEQ);
            ViewBag.menuOpcionesUsuario = menuOpcionesUsuario.OrderBy(f => f.SEQ);

            return View();
        }


        [HttpGet]
        public JsonResult OPTION_MENU_PROFILE_USER(string USERNAME)
        {
            try
            {
                // Agregar y Elmininar Rol_opcion Menu

                if (Request.Params["oper"] == "add")
                {
                    CFG_07_AX_USERPROFILE newrop = new CFG_07_AX_USERPROFILE
                    {

                        MENUOPTION = int.Parse(Request.Params["opcionMenuId"]),
                        USERID = USERNAME,
                        PROFILEID = "PS",
                        COMPANY = " "
                    };

                    _web._dbx.CFG_07_AX_USERPROFILEs.InsertOnSubmit(newrop);
                    _web._dbx.SubmitChanges();

                    return Json("OK", JsonRequestBehavior.AllowGet);
                }

                else if (Request.Params["oper"] == "delete")
                {

                    CFG_07_AX_USERPROFILE rop = _web._dbx.CFG_07_AX_USERPROFILEs.FirstOrDefault(x => x.MENUOPTION == int.Parse(Request.Params["opcionMenuId"]) && x.USERID == USERNAME);

                    _web._dbx.CFG_07_AX_USERPROFILEs.DeleteOnSubmit(rop);
                    _web._dbx.SubmitChanges();

                    return Json("OK", JsonRequestBehavior.AllowGet);

                }
                else
                    return Json("OK", JsonRequestBehavior.AllowGet);

            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message, JsonRequestBehavior.AllowGet);
            }
        }


        #endregion

        #endregion


    }
}