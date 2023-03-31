using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using PANGEA.IMPORTSUITE.WebApp.Models;
using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System.Collections.Generic;
using System.Web.Routing;


namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    [Authorize]
    public partial class HomeController : BaseController
    {

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {

            GET_APPLICATION_NAME();

            ViewBag.USER_LOGGED = Request.LogonUserIdentity.Name;

            //GENERAL PARAMETERS SESSION FROM WEBCONFIG


            bool use365 = false;
            try
            {
                use365 = System.Configuration.ConfigurationManager.AppSettings["USE365"].Equals("T");

                Session["365CNN"] = UtilTool.ObtenerParametro("CNN", curConnection);

            } 
            catch { }

            Session["USE365"] = use365;

            bool noUseLogin = false;
            try
            {
                noUseLogin = System.Configuration.ConfigurationManager.AppSettings["NOLOGIN"].Equals("T");
            } 
            catch { }



            //IF NO LOGIN Goes directly to the Dash Board
            if (noUseLogin)
            {
                Session["NOLOGIN"] = noUseLogin;

                AccessNoLogin();

                return RedirectToAction("Dashboard", new RouteValueDictionary(new { controller = "DIRECTAX", action = "Dashboard" }));

            }

            ViewBag.EnvMessage = System.Configuration.ConfigurationManager.AppSettings["ENVMSG"];

            return View();
        }


        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginModel model, string returnUrl)
        {

            if (!ModelState.IsValid)
                return View(model);


            try
            {
                int result = ValidateLogin(model.UserName, persistCookie: model.RememberMe);

                switch (result)
                {

                    case 1:
                        //Send the user to the first menu option 
                        string[] redirectOption = Session["InitialPage"].ToString().Split('/');

                        if (!redirectOption[2].Contains("?"))
                            return RedirectToAction(redirectOption[2], redirectOption[1]);
                        else
                        {
                            string urlParam = redirectOption[2].Split('?')[1].Split('=')[1];
                            string curAction = redirectOption[2].Split('?')[0];
                            return RedirectToAction(curAction, new RouteValueDictionary(new { controller = redirectOption[1], action = curAction, TPL = urlParam }));
                        }

                    case 2:
                        return RedirectToLocal(returnUrl);

                    default:
                        ModelState.AddModelError("", "Invalid login attempt.");
                        return View(model);

                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            ViewBag.USER_LOGGED = Request.LogonUserIdentity.Name;
            return View(model);


        }


        public int ValidateLogin(string LogonUserIdentity, bool persistCookie)
        {

            var LogonUser = LogonUserIdentity.Split('\\');

            string domain = LogonUser[0];
            string username = LogonUser[1];

            //Execeute procedure to populare PROFILE
            try
            {
                SQLBase.ExecuteQuery("EXEC CFG_01_PROFILE ", _web._sqlCnn);
            }
            catch
            {

            }


            bool fullControl = UtilTool.ObtenerParametro("FULLCONTROL", curConnection).Equals("T");

            //Creado la Session del Usuario
            //S_USER USER = _web._dbx.S_USERs.Where(f => f.USERNAME == LogonUserIdentity).First();
            CFG_07_AX_USERPROFILE profile = _web._dbx.CFG_07_AX_USERPROFILEs.Where(f => f.USERID == username).FirstOrDefault();

            if (profile == null && !fullControl)
                throw new Exception("User " + LogonUserIdentity + " not allowed. Please contact system administrator.");

            S_USER USER = new S_USER
            {
                USERNAME = (fullControl ? username : profile.USERID),
                ACTIVE = true
            };


            string fullAdminRol = UtilTool.ObtenerParametro("ADMINROL", curConnection);

            //SI pertenece al ROL DE FULL ACCESS
            if (profile != null && profile.PROFILEID == fullAdminRol)
                fullControl = true;

            Session["curUser"] = USER;
            Session["curProfileID"] = USER.ROWID_ROL;
            Session["fullControl"] = fullControl;
            Session["MODE"] = "UPLOAD";
            Session["LOGONUSER"] = username;
            Session["MULTICOMPANY"] = UtilTool.ObtenerParametro("MULTICOMPANY", curConnection).Equals("T");

            List<S_MENU> opcionesMenu = null;

            if (fullControl)
                opcionesMenu = GENERATE_USER_MENU(USER.ROWID_ROL);
            else
            {
                //Traer de las tablas de AX lo permisos para ese usuario
                List<CFG_07_AX_USERPROFILE> profileList = _web._dbx.CFG_07_AX_USERPROFILEs.Where(f => f.USERID == username).ToList();
                Session["profileList"] = profileList;
                opcionesMenu = GENERATE_USER_MENU_DIRECTAX(profileList);

            }

            Session["curUser_Menu"] = opcionesMenu;

            Session["InitialPage"] = "/DIRECTAX/Dashboard";
            /*
            if (!string.IsNullOrEmpty(USER.M_ROL.DEFAULT_PAGE))
                Session["InitialPage"] = USER.M_ROL.DEFAULT_PAGE; //opcionesMenu.OrderBy(f => f.SEQ).First().URL;
            else
                Session["InitialPage"] = opcionesMenu.Where(f => f.S_MENU1 != null).OrderBy(f => f.SEQ).First().URL;
            */
            Session["curTemplates"] = _web._dbx.T_TEMPLATEs.Where(f => f.ACTIVE == true).ToList();

            return 1;
        }


        private void AccessNoLogin()
        {
            var LogonUser = Request.LogonUserIdentity.Name.Split('\\');

            string domain = LogonUser[0];

            string username = LogonUser[1];

            //var LogonUser = Request.LogonUserIdentity.Name;

            S_USER USER = new S_USER { USERNAME = username, ACTIVE = true };

            Session["curUser"] = USER;

            Session["curProfileID"] = USER.ROWID_ROL;

            Session["fullControl"] = true;

            Session["MODE"] = "UPLOAD";

            Session["LOGONUSER"] = username;

            List<S_MENU> opcionesMenu = GENERATE_USER_MENU(USER.ROWID_ROL);

            Session["curUser_Menu"] = opcionesMenu;

            Session["InitialPage"] = "/DIRECTAX/Dashboard";

            Session["curTemplates"] = _web._dbx.T_TEMPLATEs.Where(f => f.ACTIVE == true).ToList();

        }


        /*
        public int ValidateLogin_OLD(string LogonUserIdentity, bool persistCookie)
        {

            var LogonUser = LogonUserIdentity.Split('\\');

            string domain = LogonUser[0];
            string username = LogonUser[1];

            //Creado la Session del Usuario
            S_USER USER = _web._dbx.S_USERs.Where(f => f.USERNAME == LogonUserIdentity).First();

            Session["curUser"] = USER;
            Session["curProfileID"] = USER.ROWID_ROL;
            Session["fullControl"] = USER.M_ROL.FULL_CONTROL;
            Session["MODE"] = "UPLOAD";

            List<S_MENU> opcionesMenu = GENERATE_USER_MENU(USER.ROWID_ROL);

            Session["curUser_Menu"] = opcionesMenu;

            if (!string.IsNullOrEmpty(USER.M_ROL.DEFAULT_PAGE))
                Session["InitialPage"] = USER.M_ROL.DEFAULT_PAGE; //opcionesMenu.OrderBy(f => f.SEQ).First().URL;
            else
                Session["InitialPage"] = opcionesMenu.Where(f => f.S_MENU1 != null).OrderBy(f => f.SEQ).First().URL;

            Session["curTemplates"] = _web._dbx.T_TEMPLATEs.Where(f => f.ACTIVE == true).ToList();

            return 1;
        }
        */


        private void ResetCurSession()
        {
            Session.Clear();
            Session.RemoveAll();
        }


        private List<S_MENU> GENERATE_USER_MENU(int ID_ROL)
        {

            List<S_MENU> userMenuFinal = new List<S_MENU>();

            //Basado en el Rol(es) del Usuario se obtiene la lista de opciones
            _web = new _WEB(curConnection);

            if ((bool)Session["fullControl"] == true)
                userMenuFinal.AddRange(_web._dbx.S_MENUs.Where(f => f.ACTIVE == true).ToList());

            else
            {
                foreach (S_MENU_ROL curOption in _web._dbx.S_MENU_ROLs.Where(f => f.ROWID_ROL == ID_ROL && f.S_MENU.ACTIVE == true).ToList())
                {

                    //Add Opcion Padre
                    if (curOption.S_MENU.PARENTOPTION != null && !userMenuFinal.Contains(curOption.S_MENU.S_MENU1))
                    {
                        if (!userMenuFinal.Any(f => f.ROWID == curOption.S_MENU.PARENTOPTION))
                            userMenuFinal.Add(curOption.S_MENU.S_MENU1);
                    }

                    //Add Opcion Hija
                    if (!userMenuFinal.Any(f => f.ROWID == curOption.S_MENU.ROWID))
                        userMenuFinal.Add(curOption.S_MENU);

                }
            }

            if (userMenuFinal.Count > 0)
                return userMenuFinal;
            else
                throw new Exception("Not options defined for current user.");

        }


        private List<S_MENU> GENERATE_USER_MENU_DIRECTAX(List<CFG_07_AX_USERPROFILE> menuList)
        {

            List<S_MENU> userMenuFinal = new List<S_MENU>();

            //Basado en el Rol(es) del Usuario se obtiene la lista de opciones
            _web = new _WEB(curConnection);


            foreach (int? userProfile in menuList.Select(f => f.MENUOPTION).Distinct())
            {
                if (userProfile == null)
                    continue;

                S_MENU curOption = _web._dbx.S_MENUs.Where(f => f.ROWID == userProfile).FirstOrDefault();

                if (curOption == null)
                    continue;


                //Add Opcion Padre
                if (curOption.PARENTOPTION != null && !userMenuFinal.Contains(curOption.S_MENU1))
                {
                    if (!userMenuFinal.Any(f => f.ROWID == curOption.PARENTOPTION))
                        userMenuFinal.Add(curOption.S_MENU1);
                }

                //Add Opcion Hija
                if (!userMenuFinal.Any(f => f.ROWID == curOption.ROWID))
                    userMenuFinal.Add(curOption);

            }


            if (userMenuFinal.Count > 0)
                return userMenuFinal;
            else
                return null;
        }


        private void GET_APPLICATION_NAME()
        {
            if (Session["APPLICATION_NAME"] == null)
            {
                string appName = UtilTool.ObtenerParametro("APPLICATION_NAME", curConnection);

                Session["APPLICATION_NAME"] = string.IsNullOrEmpty(appName) ? "PANGEA::Import Suite" : appName;
            }
        }
    

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion


        [NoCache]
        public ActionResult Test()
        {
            return View();
        }

    }
}