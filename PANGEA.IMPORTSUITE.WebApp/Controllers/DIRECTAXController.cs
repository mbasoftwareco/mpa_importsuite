using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.AX_DIRECT_IMPORT;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using PANGEA.IMPORTSUITE.ErpFactory;
using System.IO;
using System.Security.Principal;
using System.ComponentModel;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class DIRECTAXController : BaseController
    {

        [NoCache]
        public ActionResult Dashboard()
        {

            //Execeute procedure to populare PROFILE
            try
            {
                SQLBase.ExecuteQuery("EXEC CFG_01_PROFILE ", _web._sqlCnn);
            }
            catch
            {

            }

            return View();
        }


        #region 01_BASE

        // GET: PROFILE
        [NoCache]
        public ActionResult IMPORT_01BASE()
        {

            if (Request.Params["GUID"] == null)
                ViewBag.GUID = UtilTool.ReturnGUID();

            //List of available templates
            ViewBag.TPL = Request.Params["TPL"];

            //List of available countries
            List<LIST_SELECTION> entityList = (from tem in _web._dbx.M_METAMASTERs.Where(f => f.CLASS_CODE == "ENTITY").OrderBy(f => f.NAME)
                                               select new LIST_SELECTION
                                               {
                                                   VALUE = tem.CODE,
                                                   TEXT = tem.CODE + " - " + tem.NAME
                                               }).Distinct().ToList();

            //If not is a full control user, entity list must be refined to allowd companies
            if ((bool)Session["fullControl"] != true && (bool)Session["MULTICOMPANY"] != true)
            {
                List<LIST_SELECTION> newEntityList = new List<LIST_SELECTION>();

                List<CFG_07_AX_USERPROFILE> profileList = (List<CFG_07_AX_USERPROFILE>)Session["profileList"];

                S_MENU sMenu = _web._dbx.S_MENUs.Where(f => f.NAME == Request.Params["TPL"]).FirstOrDefault();

                foreach (LIST_SELECTION curReg in entityList)
                {
                    if (sMenu != null && profileList.Any(f => f.MENUOPTION == sMenu.ROWID && f.COMPANY == curReg.VALUE))
                        newEntityList.Add(curReg);
                }

                entityList = newEntityList;

            }

            #region REFINATE VALID COMPANIES

            T_TEMPLATE curTemplate = _web._dbx.T_TEMPLATEs.Where(f => f.NAME == Request.Params["TPL"]).FirstOrDefault();

            if (!string.IsNullOrEmpty(curTemplate.FOLDER))
                entityList = entityList.Where(f => curTemplate.FOLDER.Contains(f.VALUE)).ToList();

            ViewBag.SELECT_ENTITY = 1;
            if (curTemplate.CUSTOM_VALIDATIONS.Contains("MULTICOMPANY"))
                ViewBag.SELECT_ENTITY = 0;

            #endregion



            ViewBag.ENTITY_LIST = entityList;

            if (entityList.Count == 1)
                ViewBag.ENTITY = entityList[0].VALUE;


            try { ViewBag.SETUP_ENTITY = _db.S_PARAMETERs.Where(f => f.CODE == "SETUP_ENTITY").First().VALUE; } catch { }

            return View();
        }

        [NoCache]
        public JsonResult IMPORT_01BASE_01GETFILE_BROWSE(HttpPostedFileBase FILE, string FOLDER_FILE, string GUID, string TPL)
        {
            T_TEMPLATE_RUNTIME tplRuntime = null;
            string flag = "";

            Server.ScriptTimeout = 3600;

            try
            {
                GUID = UtilTool.ReturnGUID();
                string entity = Request["ENTITY"];
                string module = TPL;


                T_TEMPLATE curTemplate = _web._dbx.T_TEMPLATEs.Where(f => f.NAME == TPL).FirstOrDefault();

                if (curTemplate == null)
                    throw new Exception("Template not found.");

                T_TEMPLATE_RUNTIME tmpRun =  _web._dbx.T_TEMPLATE_RUNTIMEs.FirstOrDefault(f => f.ORIFILENAME == FILE.FileName && f.ENTITY == entity);

                if (tmpRun != null)
                {
                    if (!string.IsNullOrEmpty(tmpRun.MESSAGE))
                        throw new Exception("Error: " + tmpRun.MESSAGE.Replace("\n"," ").Replace("'"," "));                    
                    else
                        throw new Exception("A file with same name was already processed. " + FILE.FileName);
                }

                if (string.IsNullOrEmpty(FILE.FileName))
                    throw new Exception("Please enter a file to process.");


                //Copy the file to Repository
                //YEAR//COUNTRY//MODULE
                string additionalPath = "UPLOADED\\"; //DateTime.Today.Year.ToString() + "/" + entity + "/" + module + "/";

                flag = "SAVING FILE";
                string filePath = FILE_Save(FILE.FileName, additionalPath, GUID, Request.Files[0], "", false);

                string[] nombre = FILE.FileName.Replace(" ", "").Split("\\".ToCharArray());

                //File Columns   
                flag = "CSV";
                CSVFileSetup fileColumns = FILE_GetColumns(filePath, ";");
                if (fileColumns.columnNum <= 2)
                    fileColumns = FILE_GetColumns(filePath, ",");

                //Crate the Template Runtime
                tplRuntime = new T_TEMPLATE_RUNTIME
                {
                    CREATEDBY = curUser.USERNAME,
                    CREATEDON = DateTime.Now,
                    GUID = GUID,

                    ENTITY = entity,
                    MODULE = module,

                    FILENAME = filePath,
                    ROWID_TEMPLATE = curTemplate.ROWID,
                    STATUS_ID = Constantes.STATUS_READY_TO_PROCESS,
                    MESSAGE = "",
                    COLUMNS = fileColumns.columnList,
                    ORIFILENAME = FILE_GetShortName(FILE.FileName)
                };

                flag = "DB TPL RUNTIME";
                _web._dbx.T_TEMPLATE_RUNTIMEs.InsertOnSubmit(tplRuntime);

                _web._dbx.SubmitChanges();


                //PCOL, PTPA, PTCR, PTSV, PPER
                flag = "AX MSG";

                string result = "";

                if (use365 == true)
                {
                    #region USE 365

                    string readText = System.IO.File.ReadAllText(filePath, Encoding.Default);

                    AX365_Params cur365 = new AX365_Params
                    {
                        entity = entity,
                        _parmURLFileCSV = readText,
                        _parmFileName = FILE.FileName,
                        _parmSeparator = fileColumns.separator,
                        _parmGUID = GUID,
                        _parmModule = module,
                        _ENV = Session["365CNN"].ToString(),
                        _parmUserId = curUser.USERNAME,
                        curConnection = curConnection

                    };


                    result = AX365.AX365_import(cur365);


                    if (result == "OK")
                    {
                        try
                        {

                            result = AX365.AX365_validate(cur365, curTemplate.VALIDATIONS);

                            tplRuntime.STATUS_ID = (result == "OK") ? Constantes.STATUS_READY_TO_PROCESS : Constantes.STATUS_WITH_ERROR;

                            _web._dbx.SubmitChanges();

                        }
                        catch (Exception ex)
                        {

                        }

                    }
                    else
                        throw new Exception(flag + ": " + result);


                    #endregion
                }
                else
                {
                    //CONNECTION PARAMETERS
                    string cnnType = _db.S_PARAMETERs.Where(f => f.CODE == "CNNTYPE").First().VALUE;
                    string cnnString = _db.S_PARAMETERs.Where(f => f.CODE == "CNNSTR").First().VALUE;


                    ConnectFactory axCnn = ConnectFactory.getConnectFactory(new ErpConnection { company = entity, cnnType = cnnType, cnnString = cnnString });

                    result = axCnn.ErpService().ImportData(filePath, FILE.FileName, fileColumns.separator, GUID, module, entity);

    
                    //If OK Call Validation
                    if (result == "OK")
                    {
                        try
                        {
                            result = axCnn.ErpService().ValidateData(GUID, module, entity);

                            tplRuntime.STATUS_ID = (result == "OK") ? Constantes.STATUS_READY_TO_PROCESS : Constantes.STATUS_WITH_ERROR;

                            _web._dbx.SubmitChanges();

                        }
                        catch { }

                    }
                    else
                        throw new Exception(flag + ": " + result);

                }


                object jsonData = new
                {
                    error = "",
                    success = true,
                    guid = GUID
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet); 

                jsonResult.MaxJsonLength = int.MaxValue;


                Session["FileFullName"] = FILE.FileName;
                Session["SigleFileName"] = nombre[nombre.Length - 1];

                return jsonResult;


            }
            catch (Exception e)
            {
                //Guardar el error en el RUNTIME
                if (tplRuntime != null)
                {
                    tplRuntime.MESSAGE = flag + ": " + e.Message;
                    _web._dbx.SubmitChanges();
                }

                Response.StatusCode = 500;
                Response.StatusDescription = flag + ": " + e.Message;
                return Json(e.Message, JsonRequestBehavior.AllowGet);
            }



        }



        #endregion


        [NoCache]
        public ActionResult IMPORT_02CONFIRM(string GUID, bool validate)
        {
            Server.ScriptTimeout = 3600;

            //ROWID_TEMPLATE = 1;
            //GUID = "20F0DA82";


            T_TEMPLATE_RUNTIME tplR = _web._dbx.T_TEMPLATE_RUNTIMEs.Where(f => f.GUID == GUID).First();

            //Validacion del Template
            DataSet validationResult = null;


            if (use365 == true)
            {

                AX365_Params cur365 = new AX365_Params
                {
                    entity = tplR.ENTITY,
                    _parmGUID = GUID,
                    _parmModule = tplR.MODULE,
                    _ENV = Session["365CNN"].ToString(),
                    _parmUserId = curUser.USERNAME,                    
                    curConnection = curConnection
                };


                if (validate)
                    AX365.AX365_validate(cur365, "T");

                //Obtain the dataset direclty from 365 Server
                cur365._parmRegType = "SUMMARY";
                validationResult = AX365.AX365_showrecords(cur365);

            }
            else
            {


                if (validate)
                {
                    //CONNECTION PARAMETERS
                    string cnnType = _db.S_PARAMETERs.Where(f => f.CODE == "CNNTYPE").First().VALUE;
                    string cnnString = _db.S_PARAMETERs.Where(f => f.CODE == "CNNSTR").First().VALUE;

                    //Call th ax Service to ImportData
                    ConnectFactory axCnn = ConnectFactory.getConnectFactory(new ErpConnection { company = tplR.ENTITY, cnnType = cnnType, cnnString = cnnString });

                    axCnn.ErpService().ValidateData(GUID, tplR.MODULE, tplR.ENTITY);

                }

                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    GUID = GUID,
                    ROWID = tplR.ROWID_TEMPLATE,
                    EXTRA_DATA = tplR.MODULE

                };
                validationResult = _web.sp_IMPORT_DIRECTAX("PENDING_RECORDS", whObj);

            }

            if (use365 == true)
                return PartialView("IMPORT_02CONFIRM_365", validationResult.Tables[0]);
            else
                return PartialView(validationResult.Tables[0]);

        }


        [NoCache]
        public JsonResult IMPORT_03_SENDERP(string GUID)
        {
            Server.ScriptTimeout = 3600;


            T_TEMPLATE_RUNTIME curTemplate = _web._dbx.T_TEMPLATE_RUNTIMEs.Where(f => f.GUID == GUID).First();


            try
            {

                string journalName = curTemplate.T_TEMPLATE.JOURNALNAME;

                string result = "";

                if (use365 == true)
                {

                    AX365_Params cur365 = new AX365_Params
                    {
                        entity = curTemplate.ENTITY,
                        _parmGUID = GUID,
                        _parmModule = curTemplate.MODULE,
                        _ENV = Session["365CNN"].ToString(),
                        _parmUserId = Session["LOGONUSER"].ToString(),
                        _parmJournal = journalName,
                        curConnection = curConnection
                    };

                    result = AX365.AX365_process(cur365);
                }
                else
                {
                    //CONNECTION PARAMETERS
                    string cnnType = _db.S_PARAMETERs.Where(f => f.CODE == "CNNTYPE").First().VALUE;
                    string cnnString = _db.S_PARAMETERs.Where(f => f.CODE == "CNNSTR").First().VALUE;

                    string autopost = (curTemplate.T_TEMPLATE.AUTOPOST == true) ? "1" : "0";


                    //Call th ax Service to ImportData
                    ConnectFactory axCnn = ConnectFactory.getConnectFactory(new ErpConnection { company = curTemplate.ENTITY, cnnType = cnnType, cnnString = cnnString });
                    result = axCnn.ErpService().ProcessData(GUID, curTemplate.MODULE, curTemplate.ENTITY, journalName, Session["LOGONUSER"].ToString(), autopost);
                }



                if (result.ToUpper().StartsWith("ERROR"))
                    throw new Exception(result.Replace("ERROR: ", ""));


                curTemplate.STATUS_ID = Constantes.STATUS_SENT_TO_ERP;

                curTemplate.MESSAGE = result;

                _web._dbx.SubmitChanges();

                if (!string.IsNullOrEmpty(journalName))
                    result = "Journal # " + result + " created on " + journalName;

                object jsonData = new
                {
                    error = "Process Completed. " + result,
                    success = true,
                    guid = result
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;


                #region  MOVE FILE TO PROCESSED

                try
                {

                    string movePath = UtilTool.ObtenerParametro("FILEUPLOAD", curConnection) + curTemplate.MODULE + "\\PROCESSED\\";

                    string oriFile = Session["FileFullName"].ToString(); //.Replace("\\\\", "\\");

                    string destFile = movePath + curTemplate.ENTITY + "_"  + Session["SigleFileName"].ToString();

                    if (!Directory.Exists(movePath))
                        Directory.CreateDirectory(movePath);

                    //System.IO.File.Move(oriFile, destFile);

                    UtilMoveFile(oriFile, destFile);

                }
                catch (Exception ex)
                {
                    //throw;
                }

                #endregion


                return jsonResult;


            }
            catch (Exception e)
            {
                //Guardar el error en el RUNTIME
                curTemplate.STATUS_ID = Constantes.STATUS_WITH_ERROR;
                curTemplate.MESSAGE = e.Message;
                _web._dbx.SubmitChanges();

                /*
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
                */

                object jsonData = new
                {
                    error = e.Message,
                    success = false,
                    guid = ""
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;

                return jsonResult;

            }

        }

        public static void UtilMoveFile(string oriFile,string destFile)
        {

            IntPtr token;

            string[] curUser = UtilTool.ObtenerParametro("IMPERSONATE", curConnection).Split('|');

            if (!NativeMethods.LogonUser(curUser[0], curUser[2], curUser[1],
                /* this.userName,
                this.domain,
                this.password,*/
                NativeMethods.LogonType.NewCredentials,
                NativeMethods.LogonProvider.Default,
                out token))
            {
                throw new Win32Exception();
            }

            try
            {
                IntPtr tokenDuplicate;

                if (!NativeMethods.DuplicateToken(
                    token,
                    NativeMethods.SecurityImpersonationLevel.Impersonation,
                    out tokenDuplicate))
                {
                    throw new Win32Exception();
                }

                try
                {
                    using (WindowsImpersonationContext impersonationContext =
                        new WindowsIdentity(tokenDuplicate).Impersonate())
                    {
                        // Do stuff with your share here.

                        System.IO.File.Move(oriFile, destFile);

                        impersonationContext.Undo();
                        return;
                    }
                }
                finally
                {
                    if (tokenDuplicate != IntPtr.Zero)
                    {
                        if (!NativeMethods.CloseHandle(tokenDuplicate))
                        {
                            // Uncomment if you need to know this case.
                            ////throw new Win32Exception();
                        }
                    }
                }
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    if (!NativeMethods.CloseHandle(token))
                    {
                        // Uncomment if you need to know this case.
                        ////throw new Win32Exception();
                    }
                }
            }
        }




        [NoCache]
        public ActionResult IMPORT_04SHOWREGS(string GUID, string etype, string TPL)
        {

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                GUID = GUID,
                EXTRA_DATA = etype,
                VALUE = TPL
            };

            T_TEMPLATE_RUNTIME tplR = _web._dbx.T_TEMPLATE_RUNTIMEs.Where(f => f.GUID == GUID).First();

            string[] fileName = tplR.FILENAME.Split("/".ToCharArray());

            ViewBag.Title = fileName[fileName.Length - 1] + " - " + etype + " Records. ";

            //Validacion del Template
            DataSet validationResult = null;

            if (use365 == true)
            {
                AX365_Params cur365 = new AX365_Params
                {
                    entity = tplR.ENTITY,
                    _parmGUID = GUID,
                    _parmModule = tplR.MODULE,
                    _ENV = Session["365CNN"].ToString(),
                    _parmRegType = etype,
                    curConnection = curConnection
                };

                //Obtain the dataset direclty from 365 Server
                validationResult = AX365.AX365_showrecords(cur365);

            }
            else
                validationResult = _web.sp_IMPORT_DIRECTAX("SHOWREGS", whObj);

            DataTable depureTable = validationResult.Tables[0].Copy();

            //Remover Columnas que  no esten en la lista
            if (!string.IsNullOrEmpty(tplR.COLUMNS))
            {

                foreach (DataColumn curColumn in validationResult.Tables[0].Columns)
                {
                    try
                    {
                        if (tplR.COLUMNS.ToUpper().Contains(curColumn.ColumnName.ToUpper() + "|"))
                            continue;

                        if (curColumn.ColumnName.ToUpper().Equals("ERRORMSG"))
                        {
                            depureTable.Columns[curColumn.ColumnName].SetOrdinal(0);
                            continue;
                        }

                        if (curColumn.ColumnName.ToUpper().Equals("JNL_SEQ"))
                        {
                            depureTable.Columns[curColumn.ColumnName].SetOrdinal(1);
                            continue;
                        }

                        //Remove column from datatable
                        depureTable.Columns.Remove(curColumn.ColumnName);

                    }
                    catch
                    {
                        continue;
                    }
                }
            }


            return PartialView(depureTable);

        }


        [NoCache]
        public JsonResult EXPORT_IMPORT_04SHOWREGS(string GUID, string etype, string TPL)
        {
            try
            {
                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    GUID = GUID,
                    EXTRA_DATA = etype,
                    VALUE = TPL
                };

                T_TEMPLATE_RUNTIME tplR = _web._dbx.T_TEMPLATE_RUNTIMEs.Where(f => f.GUID == GUID).First();

                string[] fileName = tplR.FILENAME.Split("/".ToCharArray());

                ViewBag.Title = fileName[fileName.Length - 1] + " - " + etype + " Records. ";

                //Validacion del Template
                DataSet validationResult = null;

                if (use365 == true)
                {
                    AX365_Params cur365 = new AX365_Params
                    {
                        entity = tplR.ENTITY,
                        _parmGUID = GUID,
                        _parmModule = tplR.MODULE,
                        _ENV = Session["365CNN"].ToString(),
                        _parmRegType = etype,
                        curConnection = curConnection
                    };

                    //Obtain the dataset direclty from 365 Server
                    validationResult = AX365.AX365_showrecords(cur365);
                }
                else
                    validationResult = _web.sp_IMPORT_DIRECTAX("SHOWREGS", whObj);

                string nombreFile = UtilTool.ReturnGUID() + ".xlsx";

                return Json(WriteExcelDocumentV3_OXML(validationResult, nombreFile));
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }



        [NoCache]
        public ActionResult IMPORT_HISTORY(FormCollection form)
        {

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                EXTRA_DATA = Request.Params["USER"],
                VALUE = Request.Params["MODULE"],
                EXTRA_DATA3 = Request.Params["ENTITY"],
                EXTRA_DATA4 = Request.Params["DATE1"],
                EXTRA_DATA5 = Request.Params["DATE2"]
            };


            //Validacion del Template
            DataSet validationResult = null;

            validationResult = _web.sp_IMPORT_DIRECTAX("HISTORY", whObj);

            DataSet filter = _web.sp_IMPORT_DIRECTAX("FILTER.HISTORY", whObj);
            ViewBag.Usuarios = filter.Tables[0];
            ViewBag.Modulos = filter.Tables[1];
            ViewBag.Entity = filter.Tables[2];


            return PartialView(validationResult.Tables[0]);

        }


        [NoCache]
        [HttpPost]
        public JsonResult EXPORT_IMPORT_HISTORY(FormCollection form)
        {
            try
            {

                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    EXTRA_DATA = Request.Params["USER"],
                    VALUE = Request.Params["MODULE"],
                    EXTRA_DATA3 = Request.Params["ENTITY"],
                    EXTRA_DATA4 = Request.Params["DATE1"],
                    EXTRA_DATA5 = Request.Params["DATE2"]
                };

                DataSet ds = _web.sp_IMPORT_DIRECTAX("HISTORY", whObj);


                string nombreFile = UtilTool.ReturnGUID() + ".xlsx";

                return Json(WriteExcelDocumentV3_OXML(ds, nombreFile));
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }



        [NoCache]
        //Cierra La Session Del Usuario
        public ActionResult ClosedSession()
        {
            Session.Clear();
            Session.RemoveAll();
            //return RedirectToAction("Login", "Account");
            return RedirectToAction("Login", "Home");
        }


        [NoCache]
        //Cierra La Session Del Usuario
        public ActionResult ChangeMenu(string mode)
        {
            Session["MODE"] = mode;
            return Redirect(Request.UrlReferrer.ToString());
        }



        [NoCache]
        public ActionResult IMPORT_HISTORY_RUNTIME(string TPL)
        {

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                EXTRA_DATA = TPL
            };

            //Validacion del Template
            DataSet validationResult = _web.sp_IMPORT_DIRECTAX("HISTORY_RUNTIME", whObj);


            return PartialView(validationResult.Tables[0]);

        }

        [NoCache]
        public ActionResult IMPORT_05SHOWDS()
        {
            var OPTION = Request.Params["OPTION"];

            WH_Obj whObj = new WH_Obj { USUARIO = curUser };

            DataSet validationResult = _web.sp_Views(OPTION, whObj);

            Session["IMPORT_05SHOWDS"] = validationResult;


            string Keyword = "";
            try { Keyword = Request.Params["SEARCH"].Trim(); }
            catch { }

   
            //Validacion del Template


            ViewBag.OPTION = OPTION;
            ViewBag.SEARCH = Keyword;

            if (!String.IsNullOrEmpty(Keyword))
            {
                validationResult = (DataSet)Session["IMPORT_05SHOWDS"];

                DataTable tmpSearch = SearchInAllColums(validationResult.Tables[0], Keyword, StringComparison.InvariantCultureIgnoreCase);

                validationResult = new DataSet();

                validationResult.Tables.Add(tmpSearch);

                return View(validationResult.Tables[0]);

            }
            else
            {
                return View(validationResult.Tables[0]);
            }

        }

        [NoCache]
        [HttpPost]
        public JsonResult EXPORT_IMPORT_05SHOWDS()
        {
            try
            {
                //Validacion del Template
                DataSet validationResult = null;

                validationResult = (DataSet)Session["IMPORT_05SHOWDS"];

                string nombreFile = UtilTool.ReturnGUID() + ".xlsx";

                return Json(WriteExcelDocumentV3_OXML(validationResult, nombreFile));
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }


        public static DataTable SearchInAllColums(DataTable table, string keyword, StringComparison comparison)
        {
            if (keyword.Equals(""))
            {
                return table;
            }
            DataRow[] filteredRows = table.Rows
                   .Cast<DataRow>()
                   .Where(r => r.ItemArray.Any(
                   c => c.ToString().IndexOf(keyword, comparison) >= 0))
                   .ToArray();

            if (filteredRows.Length == 0)
            {
                DataTable dtProcessesTemp = table.Clone();
                dtProcessesTemp.Clear();
                return dtProcessesTemp;
            }
            else
            {
                return filteredRows.CopyToDataTable();
            }
        }


    }

}