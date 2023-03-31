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
using System.Net;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class MPAController : BaseController
    {
        private readonly object JSONConvert;

        [NoCache]
        public ActionResult BANKRECONCIL_01BASE()
        {

            //PrepareProcess_And_SendToErp("0");


            string GUID = (Request.Params["GUID"] == null) ? UtilTool.ReturnGUID() : Request.Params["GUID"];

            ViewBag.GUID = GUID;

            ViewBag.UPTYPE = "";

            /*
            if (Request.Params["UPTYPE"] != null && Request.Params["UPTYPE"] != "")
            {
                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    EXTRA_DATA = Request.Params["UPTYPE"]
                };

                DataSet listFileType = _web.sp_MPA_RECONCIL("FILETYPE", whObj);

                ViewBag.FITYPE_LIST = listFileType.Tables[0];
                ViewBag.UPTYPE = Request.Params["UPTYPE"];
            }
            */

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


                if (!(FILE.FileName.ToLower().Contains(".csv") || FILE.FileName.ToLower().Contains(".xls") || FILE.FileName.ToLower().Contains(".xlsx")))
                    throw new Exception("File extension not valid.");

                //Copy the file to Repository
                //YEAR//COUNTRY//MODULE
                //string additionalPath = DateTime.Today.Year.ToString() + "\\" + entity + "\\" + module + "\\";
                string additionalPath =  "UPLOADED\\";

                flag = "SAVING FILE";
                string filePath = FILE_Save(FILE.FileName, additionalPath, GUID, Request.Files[0], "", true);


                //CUR FILE
                string[] nombre = FILE.FileName.Replace(" ", "").Split("\\".ToCharArray());
                string nombreFile = GUID + Path.GetExtension(FILE.FileName); //"." + nombre[nombre.Length - 1].Split('.')[1];

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


                Session["FileFullName"] = FILE.FileName;
                Session["SigleFileName"] = nombre[nombre.Length - 1];


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
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                try
                {

                    string filename = runtime_row.Rows[0]["ORIFILENAME"].ToString();
                    string filetoProcess = runtime_row.Rows[0]["FILEPATHWS"].ToString().Replace("/", "\\");
                    string loadedBy = ((S_USER)Session["curUser"]).USERNAME;


                    string swResult = svc.LoadNewFile(
                        "", //runtime_row.Rows[0]["FTYPE"].ToString() - el servicio detecta el filetype automaticamente
                        filename,
                        "",
                        filetoProcess,
                        loadedBy
                        );

                    runtime_table.Rows[0]["COLOR"] = "red";

                    if (!swResult.Contains("Error"))
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


                string resultMessage = "";

                //foreach (string curID in sheetChecked.Split(','))
                //{

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
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;


                for (int i = 0; i < runtime_table.Rows.Count; i++)
                {
                    string sheetName = runtime_table.Rows[i]["TABLE_NAME"].ToString().Replace("$", "");

                    if (sheetChecked.Contains(sheetName))
                    {
                        try
                        {
                            string filename = runtime_row.Rows[0]["ORIFILENAME"].ToString();
                            string sheet = sheetName.Replace("'", "").Replace("$", "");
                            string filetoProcess = runtime_row.Rows[0]["FILEPATHWS"].ToString().Replace("/", "\\");
                            string loadedBy = ((S_USER)Session["curUser"]).USERNAME;



                            string swResult = svc.LoadNewFile(

                                "", //runtime_row.Rows[0]["FTYPE"].ToString(),
                                filename,
                                sheet,
                                filetoProcess,
                                loadedBy
                                );

                            runtime_table.Rows[i]["COLOR"] = "red";

                            if (!swResult.Contains("Error"))
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

                //}

            }
            #endregion


            //MOVE FILE
            try
            {
                string movePath = UtilTool.ObtenerParametro("FILEUPLOAD", curConnection) + "RECONCILE\\PROCESSED\\";

                string oriFile = Session["FileFullName"].ToString(); //.Replace("\\\\", "\\");

                string destFile = movePath +  Session["SigleFileName"].ToString();


                if (!Directory.Exists(movePath))
                    Directory.CreateDirectory(movePath);

                //System.IO.File.Move(oriFile, destFile);

                DIRECTAXController.UtilMoveFile(oriFile, destFile);

            }
            catch (Exception ex)
            {

            }


            return PartialView(runtime_table);

        }


        [NoCache]
        public ActionResult BANKRECONCIL_04SHOWREGS(string GUID, int? recordsTotal)
        {

            ViewBag.GUID = GUID;

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                GUID = GUID,
                EXTRA_DATA = Request["RegType"],
                VALUE = "",
                EXTRA_DATA2 = "1",
                EXTRA_DATA3 = "100"
            };

            DataSet validationResult = _web.sp_MPA_RECONCIL("SHOWREGS", whObj);
            DataTable dt = validationResult.Tables[0];

            try
            {
                ViewBag.recordsTotal = (int)recordsTotal;
            }
            catch
            {
                if (validationResult.Tables[0].Rows.Count < 100)
                    ViewBag.recordsTotal = validationResult.Tables[0].Rows.Count;
                else
                    ViewBag.recordsTotal = 500;

            }

            return PartialView(dt);

        }


        [HttpGet]
        [NoCache]
        public JsonResult BANKRECONCIL_04SHOWREGS_DATA(string GUID, int draw, int pagination, int recordsTotal)
        {

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                GUID = GUID,
                EXTRA_DATA = Request["RegType"],
                VALUE = "",
                EXTRA_DATA2 = draw.ToString(),
                EXTRA_DATA3 = pagination.ToString()
            };



            //Validacion del Template
            DataSet validationResult = _web.sp_MPA_RECONCIL("SHOWREGS", whObj);
            DataTable dt = validationResult.Tables[0];


            int page = draw;


            System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> row;
            foreach (System.Data.DataRow dr in dt.Rows)
            {
                row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    row.Add(col.ColumnName, dr[col]);
                }
                rows.Add(row);
            }

            var listaRegistros = serializer.Serialize(rows);

            object jsonData = new { draw = page, recordsTotal = recordsTotal, recordsFiltered = recordsTotal, data = listaRegistros };

            var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;

        }


        [NoCache]
        public JsonResult EXPORT_IMPORT_04SHOWREGS(string GUID, string RegType, string batchNo)
        {
            try
            {
                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    GUID = GUID,
                    EXTRA_DATA = RegType,
                    VALUE = batchNo
                };

                ViewBag.Title = GUID + " - " + RegType + " Records. ";

                //Validacion del Template
                DataSet validationResult = null;

                validationResult = _web.sp_MPA_RECONCIL("SHOWREGS", whObj);

                string nombreFile = GUID + ".xlsx";

                return Json(WriteExcelDocumentV3_OXML(validationResult, nombreFile));

            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }


    }

}