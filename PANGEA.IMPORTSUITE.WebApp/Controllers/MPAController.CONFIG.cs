using Excel;
using LINQtoCSV;
using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.AX_DIRECT_IMPORT;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Microsoft.VisualBasic.FileIO;
using System.Text;
using PANGEA.IMPORTSUITE.DataModel.MPA_RECONCILE_WS;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class MPAController : BaseController
    {
        
        /*
        [NoCache]
        public ActionResult BANKRECONCIL_01BASE()
        {
            string GUID = (Request.Params["GUID"] == null) ? UtilTool.ReturnGUID() : Request.Params["GUID"];
            ViewBag.GUID = GUID;

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                EXTRA_DATA = Request.Params["UPTYPE"]

            };

            DataSet listFileType = _web.sp_MPA_RECONCIL("FILETYPE", whObj);

            ViewBag.FITYPE_LIST = listFileType.Tables[0];

            return View();
        }


        [NoCache]
        public JsonResult BANKRECONCIL_01BASE_FILE_BROWSE(HttpPostedFileBase FILE, string GUID, string FITYPE)
        {

            string flag = "";

            Server.ScriptTimeout = 3600;

            try
            {
                //GUID = UtilTool.ReturnGUID();
                string entity = "MPA";
                string module = FITYPE;

                if (string.IsNullOrEmpty(FILE.FileName))
                    throw new Exception("Please enter a file to process.");


                if (!( FILE.FileName.ToLower().Contains(".csv") || FILE.FileName.ToLower().Contains(".xls") || FILE.FileName.ToLower().Contains(".xlsx")))
                    throw new Exception("File extension not valid.");

                //Copy the file to Repository
                //YEAR//COUNTRY//MODULE
                string additionalPath = DateTime.Today.Year.ToString() + "\\" + entity + "\\" + module + "\\";

                flag = "SAVING FILE";
                string filePath = FILE_Save(FILE.FileName, additionalPath, GUID, Request.Files[0], "", true);


                //CUR FILE
                string[] nombre = FILE.FileName.Replace(" ", "").Split("\\".ToCharArray());
                string nombreFile = GUID + "." + nombre[nombre.Length - 1].Split('.')[1];

                string filePathWS = additionalPath + nombreFile;

                //SAVE RUNTIME RECORD IN DATABASE

                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    EXTRA_DATA = FITYPE,
                    EXTRA_DATA3 = filePath,
                    EXTRA_DATA4 = filePathWS,
                    EXTRA_DATA5 = nombre[nombre.Length - 1],
                    GUID = GUID

                };

                DataSet listFileType = _web.sp_MPA_RECONCIL("SAVERUNTIME", whObj);


                object jsonData = new
                {
                    error = "",
                    success = true,
                    guid = GUID,
                    extension = (FILE.FileName.ToLower().Contains(".csv") ? "csv" : "")
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;

                return jsonResult;


            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = flag + ": " + e.Message;
                return Json(e.Message);
            }



        }


        [NoCache]
        public ActionResult BANKRECONCIL_02CONFIRM(string GUID, bool validate)
        {
            Server.ScriptTimeout = 3600;


            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                GUID = GUID
            };

            DataTable runtime_row = _web.sp_MPA_RECONCIL("GETRUNTIME", whObj).Tables[0];

            string excelConnStr = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + runtime_row.Rows[0]["FILEPATH"].ToString() + "; Extended Properties =Excel 8.0;";

            //OBTENIENDO LA HOJA PRINCIPAL
            OleDbConnection excelConn = new OleDbConnection(excelConnStr);
            excelConn.Open();

            DataTable dt_hojas = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

            excelConn.Close();

            return PartialView(dt_hojas);

        }



        [NoCache]
        public ActionResult BANKRECONCIL_03RESULT()
        {

            Server.ScriptTimeout = 3600;

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                GUID = Request["GUID"]
            };

            DataTable runtime_row = _web.sp_MPA_RECONCIL("GETRUNTIME", whObj).Tables[0];

            DataTable runtime_table = null;


            #region CSV

            if (runtime_row.Rows[0]["ORIFILENAME"].ToString().ToLower().Contains(".csv"))
            {
                runtime_table = new DataTable();

                //Column to save the error message.
                runtime_table.Columns.Add("TABLE_NAME");
                runtime_table.Columns.Add("MESSAGE");
                runtime_table.Columns.Add("COLOR");

                runtime_table.Rows.Add();


                //Call The service
                wsAdminFiles svc = new wsAdminFiles();

                try
                {

                    string swResult = svc.LoadNewFile(
                        runtime_row.Rows[0]["FTYPE"].ToString(),
                        runtime_row.Rows[0]["ORIFILENAME"].ToString(),
                        "",
                        runtime_row.Rows[0]["FILEPATHWS"].ToString().Replace("/", "\\"),
                        "JM");

                    runtime_table.Rows[0]["COLOR"] = "red";

                    if (string.IsNullOrEmpty(swResult))
                    {
                        swResult = "OK!";
                        runtime_table.Rows[0]["COLOR"] = "green";
                    }

                    runtime_table.Rows[0]["MESSAGE"] = swResult;

                }
                catch (Exception e)
                {
                    //Guardar el error en el RUNTIME     
                    runtime_table.Rows[0]["MESSAGE"] = "ERROR: " + e.Message;
                }

            }

            #endregion


            #region NOT CSV (XLS, XLSX)
            else
            {

                string sheetChecked = Request["SHEET"].ToString();

                if (string.IsNullOrEmpty(sheetChecked))
                    throw new Exception("No sheet(s) selected.");


                string excelConnStr = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + runtime_row.Rows[0]["FILEPATH"].ToString() + "; Extended Properties =Excel 8.0;";

                //OBTENIENDO LA HOJA PRINCIPAL
                OleDbConnection excelConn = new OleDbConnection(excelConnStr);
                excelConn.Open();

                runtime_table = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                excelConn.Close();


                //Column to save the error message.
                runtime_table.Columns.Add("MESSAGE");
                runtime_table.Columns.Add("COLOR");

                //Call The service
                wsAdminFiles svc = new wsAdminFiles();



                for (int i = 0; i < runtime_table.Rows.Count; i++)
                {
                    string sheetName = runtime_table.Rows[i]["TABLE_NAME"].ToString().Replace("$", "");

                    if (sheetChecked.Contains(sheetName))
                    {
                        try
                        {

                            string swResult = svc.LoadNewFile(
                                runtime_row.Rows[0]["FTYPE"].ToString(),
                                runtime_row.Rows[0]["ORIFILENAME"].ToString(),
                                sheetName,
                                runtime_row.Rows[0]["FILEPATHWS"].ToString().Replace("/", "\\"),
                                "JM");

                            runtime_table.Rows[i]["COLOR"] = "red";

                            if (string.IsNullOrEmpty(swResult))
                            {
                                swResult = "OK!";
                                runtime_table.Rows[i]["COLOR"] = "green";
                            }

                            runtime_table.Rows[i]["MESSAGE"] = swResult;

                        }
                        catch (Exception e)
                        {
                            //Guardar el error en el RUNTIME     
                            runtime_table.Rows[i]["MESSAGE"] = "ERROR: " + e.Message;
                        }

                    }
                }

            }
            #endregion


            return PartialView(runtime_table);

        }


        [NoCache]
        public ActionResult BANKRECONCIL_04SHOWREGS(string GUID)
        {

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                GUID = GUID,
                EXTRA_DATA = "",
                VALUE = ""
            };

            //Validacion del Template
            DataSet validationResult = _web.sp_MPA_RECONCIL("SHOWREGS", whObj);

            ViewBag.GUID = GUID;

            return PartialView("~/Views/DIRECTAX/IMPORT_04SHOWREGS.cshtml", validationResult.Tables[0]);

        }


        */

        #region :: FILESINFO

        // GET: PROFILE
        public ActionResult FILESINFO()
        {
            //RETURN DATASET FILES INFO
            DataSet Result = _web.sp_MPA_CONFIG("FILES", new File_Info());

            return View(Result.Tables[0]);
        }


        public ActionResult FILEINFO_NEW(int? id)
        {
            File_Info file= new File_Info();
            if (id != null)
                file = new File_Info
                {
                    id = Convert.ToInt32(id)
                };
            

            //RETURN DATASET FILES INFO
            DataSet Result = _web.sp_MPA_CONFIG("FILE", file);

            DataSet ResultTypes = _web.sp_MPA_CONFIG("FILE.TYPES", file);
            ViewBag.FilesTypes = ResultTypes.Tables[0];

            if (Result.Tables[0].Rows.Count > 0)
            {
                file = new File_Info
                {
                    id = Convert.ToInt32(Result.Tables[0].Rows[0]["id"]),
                    Name = Result.Tables[0].Rows[0]["name"].ToString(),
                    columnsQty = Convert.ToInt32(Result.Tables[0].Rows[0]["columnsQty"]),
                    ConciliateWith = Result.Tables[0].Rows[0]["ConciliateWith"].ToString(),
                    facilityCode = Result.Tables[0].Rows[0]["facilityCode"].ToString(),
                    filetype = Result.Tables[0].Rows[0]["filetype"].ToString(),
                    inactiveDate = Result.Tables[0].Rows[0]["inactiveDate"].ToString(),
                    runConciliation = Convert.ToInt32(Result.Tables[0].Rows[0]["runConciliation"]),
                    path = Result.Tables[0].Rows[0]["path"].ToString(),
                    rowsFrom = Convert.ToInt32(Result.Tables[0].Rows[0]["rowsFrom"]),
                    sourceFile = Result.Tables[0].Rows[0]["sourceFile"].ToString(),
                    merchantId = Result.Tables[0].Rows[0]["merchantId"].ToString()
                };

                return View(file);
            }

            return View(new File_Info());
        }

       


        public ActionResult FILEINFO_UPDATE(int id)
        {
            File_Info file = new File_Info();
            if (id != null)
                file = new File_Info
                {
                    id = Convert.ToInt32(id)
                };

            //RETURN DATASET FILES INFO
            DataSet Result = _web.sp_MPA_CONFIG("FILE", file);

            DataSet ResultTypes = _web.sp_MPA_CONFIG("FILE.TYPES", file);
            ViewBag.FilesTypes = ResultTypes.Tables[0];

            return View(Result.Tables[0]);
        }

        //public JsonResult FILEINFO_NEW(string sourcefile, string name,string path,string columnsQty,string rowsFrom,string runConciliate,string ConciliateWith,string facilityCode,string filetype, string typefile,string inactivaDate)
        public JsonResult FILEINFO_NEWDATA(File_Info file)
        {
            try
            {
                DataSet Result = _web.sp_MPA_CONFIG("NEW.FILE", file);

                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }

        //public JsonResult FILEINFO_UPDATEDATA(string id,string source, string name,string typefile)
        public JsonResult FILEINFO_UPDATEDATA(File_Info file)
        {
            try
            {
                DataSet Result = _web.sp_MPA_CONFIG("FILE.UPDATE", file);

                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }

        public JsonResult FILEINFO_DELETE(int id)
        {
            try
            {
                File_Info file = new File_Info();
                file = new File_Info
                {
                    id =id
                };

          
                DataSet Result = _web.sp_MPA_CONFIG("DELETE.FILE", file);

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



    }

}