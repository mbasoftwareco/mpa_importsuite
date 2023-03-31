using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.Util;


namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class PROCESSController : BaseController
    {

        #region :: IMPORT TEMPLATE

        // GET: PROFILE
        public ActionResult IMPORT_TEMPLATE()
        {

            if (Request.Params["GUID"] == null)
                ViewBag.GUID = UtilTool.ReturnGUID();

            //List of available templates
            ViewBag.TEMPLATES = (from tem in _web._dbx.T_TEMPLATEs
                                 select new LIST_SELECTION
                                 {
                                     VALUE = tem.ROWID.ToString(),
                                     TEXT = tem.NAME
                                 }).Distinct().OrderBy(f => f.TEXT).ToList();

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                GUID = ViewBag.GUID,
                ROWID = (Request.Params["ROWID_TEMPLATE"] == null) ? 0 : int.Parse(Request["ROWID_TEMPLATE"])
            };

            ViewBag.OPEN_TRANSACTION = _web.sp_IMPORT_DATA("OPEN_TRANSACTION", whObj).Tables[0];


            return View();
        }


        //Files on Folder Setup for the template
        [NoCache]
        public JsonResult IMPORT_TEMPLATE_GET_FILE_FOLDER(int ROWID_TEMPLATE)
        {

            List<LIST_SELECTION> LIST_SELECTION = new List<LIST_SELECTION>();
            string path = "";

            try
            {
                path = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == ROWID_TEMPLATE).First().FOLDER;
            }
            catch { }

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                string[] fileEntries = Directory.GetFiles(path);
                foreach (string fileName in fileEntries)
                    LIST_SELECTION.Add(new DataModel.Util.LIST_SELECTION { VALUE = path + "\\" + Path.GetFileName(fileName), TEXT = Path.GetFileName(fileName) });

            }

            return Json(LIST_SELECTION.ToList(), JsonRequestBehavior.AllowGet);
        }



        #endregion


        #region :: LOAD FILE

        public JsonResult IMPORT_DATA(HttpPostedFileBase FILE, string FOLDER_FILE, string GUID, int TEMPLATE)
        {

            try
            {


                T_TEMPLATE TEMP = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == TEMPLATE).FirstOrDefault();

                if (TEMP == null)
                    throw new Exception("Template not found.");


                T_TEMPLATE_RUNTIME tplRuntime = null;

                if (FILE != null)
                    tplRuntime = PREPARE_DATA_FROM_FILE(TEMPLATE, FILE, GUID, "");

                else if (!string.IsNullOrEmpty(FOLDER_FILE))
                    tplRuntime = PREPARE_DATA_FROM_FILE(TEMPLATE, null, GUID, FOLDER_FILE);

                else
                    throw new Exception("Please select or upload a file");



                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    GUID = GUID,
                    ROWID = TEMPLATE,
                };


                //VALIDATE The File Records
                VALIDATON_ProcessTemplate(whObj, tplRuntime);


                return Json("OK");

            }
            catch (Exception e)
            {
                //Guardar el error en el RUNTIME

                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }

        /// <summary>
        /// Prepara un dataset segun la extension del file para pasarlo al BULK INSERT
        /// </summary>
        /// <param name="ROWID_TEMPLATE"></param>
        /// <param name="data_file"></param>
        /// <param name="GUID"></param>
        public T_TEMPLATE_RUNTIME PREPARE_DATA_FROM_FILE(int ROWID_TEMPLATE, HttpPostedFileBase uploadFile, string GUID, string filePath)
        {
            string ext = "";

            //Archivo de la lista del folder
            if (!string.IsNullOrEmpty(filePath))
                ext = Path.GetExtension(filePath).ToUpper().Replace(".","");
            else
            {
                ext = Path.GetExtension(uploadFile.FileName).ToUpper().Replace(".", ""); 
                filePath = Save_File(uploadFile.FileName);
            }

            if (!_web.GET_MMASTER("FILE.EXTENSIONS").Any(f => f.CODE.ToUpper() == ext.ToUpper()))
                throw new Exception("Extension " + ext + " is not valid.");



            switch (ext)
            {

                case "CSV":
                    (new BULK_COPY_PLAINTEXT_FILE(curConnection)).LOAD_PLAINTEXT_INTO_SQL(ROWID_TEMPLATE, filePath, GUID, ",");
                    break;

                case "XLSX":
                    (new BULK_COPY_PLAINTEXT_FILE(curConnection)).LOAD_XLSX_INTO_SQL(ROWID_TEMPLATE, filePath, GUID);
                    break;


                case "TAB":
                    (new BULK_COPY_PLAINTEXT_FILE(curConnection)).LOAD_PLAINTEXT_INTO_SQL(ROWID_TEMPLATE, filePath, GUID, "\t");
                    break;

                default:
                    throw new Exception("Extension [" + ext + "] not defined");
            }

            //Crate the Template Runtime
            T_TEMPLATE_RUNTIME tplRuntime = new T_TEMPLATE_RUNTIME
            {
                CREATEDBY = curUser.USERNAME,
                CREATEDON = DateTime.Now,
                GUID = GUID,
                FILENAME = filePath,
                ROWID_TEMPLATE = ROWID_TEMPLATE,
                STATUS_ID = Constantes.STATUS_READY_TO_PROCESS,
                MESSAGE = ""
                
            };

            _web._dbx.T_TEMPLATE_RUNTIMEs.InsertOnSubmit(tplRuntime);

            _web._dbx.SubmitChanges();

            return tplRuntime;

        }


        private string Save_File(string data_file)
        {
            string[] nombre = data_file.Replace(" ", "").Split("\\".ToCharArray());

            string oriName = nombre[nombre.Length - 1];

            string nombreFile = Guid.NewGuid().ToString().Substring(0, 8) + "_" + nombre[nombre.Length - 1];

            var uploadedFile = Request.Files[0];

            string uploadsFilesPath = Server.MapPath("/FILE_LOADED/");

            if (!Directory.Exists(uploadsFilesPath))
                Directory.CreateDirectory(uploadsFilesPath);

            var filePath = Path.Combine(uploadsFilesPath, nombreFile);

            uploadedFile.SaveAs(filePath);

            return filePath;
        }


        private DataTable PREPARE_FILE_XLSX(string filePath)
        {

            String sSheetName;
            String sConnection;
            DataTable dtTablesList;
            OleDbCommand oleExcelCommand;
            OleDbDataReader oleExcelReader;
            OleDbConnection oleExcelConnection;

            /*
                sConnection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Test.xls;Extended Properties=""Excel 12.0;HDR=No;IMEX=1"""

                oleExcelConnection = New OleDbConnection(sConnection)
                oleExcelConnection.Open()

                dtTablesList = oleExcelConnection.GetSchema("Tables")

                If dtTablesList.Rows.Count > 0 Then
                    sSheetName = dtTablesList.Rows(0)("TABLE_NAME").ToString
                End If

                dtTablesList.Clear()
                dtTablesList.Dispose()

                If sSheetName <> "" Then

                    oleExcelCommand = oleExcelConnection.CreateCommand()
                    oleExcelCommand.CommandText = "Select * From [" & sSheetName & "]"
                    oleExcelCommand.CommandType = CommandType.Text

                    oleExcelReader = oleExcelCommand.ExecuteReader

                    nOutputRow = 0

                    While oleExcelReader.Read

                    End While

                    oleExcelReader.Close()

                End If

                oleExcelConnection.Close();
                */

            return null;
        }



        #endregion





        private DataSet VALIDATON_ProcessTemplate(WH_Obj curObj, T_TEMPLATE_RUNTIME curTplRuntime)
        {

            #region PROCESS VALIDATIONS

            string sqlQueryHeader = "";
            string sqlQueryLines = "";


            #region GLOBAL VALIDATION - Prepare SQL Sentences to Execute

            string globalError = "";

            List<M_METAMASTER> validation_GLOBAL_List = UtilTool.GetMMASTER("VALIDATION.GLOBAL", curConnection);

            _web.sp_IMPORT_DATA("TEMPLATE_VALIDATION_GLOBAL_INIT", curObj);


            foreach (string cur_validation in curTplRuntime.T_TEMPLATE.VALIDATIONS.Split(';'))
            {

                if (string.IsNullOrEmpty(cur_validation))
                    continue;

                try
                {
                    string curSqlSentence = validation_GLOBAL_List.Where(f => f.CODE == cur_validation).First().NOTES;

                    if (string.IsNullOrEmpty(curSqlSentence))
                        continue;

                    curSqlSentence = VALIDATION_Global_ReplaceData(curSqlSentence, curTplRuntime.T_TEMPLATE, curObj.GUID);

                    sqlQueryHeader += curSqlSentence + "\n";

                }
                catch (Exception ex)
                {
                    globalError = "VALIDATION " + cur_validation + ": " + ex.Message + "\n";
                    
                }

            }



            if (!string.IsNullOrEmpty(globalError))
            {
                curTplRuntime.MESSAGE = globalError;
                curTplRuntime.STATUS_ID = Constantes.STATUS_WITH_ERROR;
                _web._dbx.SubmitChanges();
            }
            else
                //Validation Execution - GLOBAL 
                SQLBase.ExecuteQuery(sqlQueryHeader, _web._sqlCnn);

            


            #endregion

            //PREPARE RECORDS TO VALIDATION
            _web.sp_IMPORT_DATA("TEMPLATE_VALIDATION_LINE_INIT", curObj);

            #region LINE-FIELD VALIDATION - Prepare SQL Sentences to Execute

            //LINE VALIDATION
            List<M_METAMASTER> validation_LINE_List = UtilTool.GetMMASTER("VALIDATION.LINE", curConnection);

            try
            {
                //For each column prepare the validations
                foreach (T_TEMPLATE_FIELD curFiled in curTplRuntime.T_TEMPLATE.T_TEMPLATE_FIELDs.OrderBy(f => f.SEQUENCE))
                {

                    foreach (string cur_validation in curFiled.VALIDATIONS.Split(';'))
                    {

                        if (string.IsNullOrEmpty(cur_validation))
                            continue;

                        M_METAMASTER curValidation = validation_LINE_List.Where(f => f.CODE == cur_validation).First();

                        if (string.IsNullOrEmpty(curValidation.NOTES))
                            continue;

                        string curSqlSentence = VALIDATION_Line_ReplaceData(curValidation, curTplRuntime.T_TEMPLATE, curFiled, curObj.GUID);

                        sqlQueryLines += curSqlSentence + "\n";

                    }

                }

            }
            catch (Exception ex)
            {
                curTplRuntime.MESSAGE += ex.Message;
                _web._dbx.SubmitChanges();
            }

            #endregion


            #region CUSTOM VALIDATIONS

            if (!string.IsNullOrEmpty(curTplRuntime.T_TEMPLATE.CUSTOM_VALIDATIONS))
                _web.sp_IMPORT_DATA_CUSTOM_VALIDATIONS(curTplRuntime.T_TEMPLATE.CUSTOM_VALIDATIONS, curObj);

            #endregion  

            //Validation Execution - GLOBAL + LINES
            SQLBase.ExecuteQuery(sqlQueryLines, _web._sqlCnn);

            //CLOSE VALIDATION PROCESS - GET SUMMARY
            DataSet dsResult = _web.sp_IMPORT_DATA("TEMPLATE_VALIDATION_LINE_END", curObj);

            #endregion

            return dsResult;

        }




        //Replace data to create SQL Validation Sentences
        private string VALIDATION_Global_ReplaceData(string curSqlSentence, T_TEMPLATE curTemplate, string curGUID)
        {

            curSqlSentence = curSqlSentence.Replace("__GUID", curGUID);

            if (curSqlSentence.Contains("__DEBIT_COL"))
            {

                try
                {
                    string debitColumn = "C" + curTemplate.T_TEMPLATE_FIELDs.Where(f => f.COLUMN_TYPE == "DB").First().SEQUENCE.ToString();
                    curSqlSentence = curSqlSentence.Replace("__DEBIT_COL", debitColumn);
                }
                catch
                {
                    throw new Exception("No DEBIT column defined in the Template.");
                }
            }

            if (curSqlSentence.Contains("__CREDIT_COL"))
            {

                try
                {
                    string creditColumn = "C" + curTemplate.T_TEMPLATE_FIELDs.Where(f => f.COLUMN_TYPE == "CR").First().SEQUENCE.ToString();
                    curSqlSentence = curSqlSentence.Replace("__CREDIT_COL", creditColumn);
                }
                catch
                {
                    throw new Exception("No CREDIT column defined in the Template.");
                }
            }


            if (curSqlSentence.Contains("__SEQUENCE"))
            {

                try
                {
                    string seqColumn = "C" + curTemplate.T_TEMPLATE_FIELDs.Where(f => f.COLUMN_TYPE == "SQ").First().SEQUENCE.ToString();
                    curSqlSentence = curSqlSentence.Replace("__SEQUENCE", seqColumn);
                }
                catch
                {
                    throw new Exception("No SEQUENCE column defined in the Template.");
                }
            }


            if (curSqlSentence.Contains("__OFFSET_ACCOUNT"))
            {

                try
                {
                    string seqColumn = "C" + curTemplate.T_TEMPLATE_FIELDs.Where(f => f.COLUMN_TYPE == "OF").First().SEQUENCE.ToString();
                    curSqlSentence = curSqlSentence.Replace("__OFFSET_ACCOUNT", seqColumn);
                }
                catch
                {
                    throw new Exception("No OFFSET_ACCOUNT column defined in the Template.");
                }
            }




            return curSqlSentence;
        }


        //Replace data to create SQL Validation Sentences
        private string VALIDATION_Line_ReplaceData(M_METAMASTER validation, T_TEMPLATE curTemplate, T_TEMPLATE_FIELD curField, string curGUID)
        {
            string curSqlSentence = validation.NOTES;

            curSqlSentence = curSqlSentence.Replace("__GUID", curGUID);

            curSqlSentence = curSqlSentence.Replace("__FIELDNAME", curField.FIELD_SOURCE_NAME);

            curSqlSentence = curSqlSentence.Replace("__CUR_COLUMN", "C" + curField.SEQUENCE.ToString());

            curSqlSentence = curSqlSentence.Replace("__CATEGORY", curField.FIELD_DEST_NAME);

            if (validation.ROWID_LIST != null)
                curSqlSentence = curSqlSentence.Replace("__ID_LIST", validation.ROWID_LIST.ToString());


            if (curSqlSentence.Contains("__ENTITY"))
            {

                try
                {
                    string entityColumn = curTemplate.T_TEMPLATE_FIELDs.Where(f => f.COLUMN_TYPE == "EN").First().SEQUENCE.ToString();
                    curSqlSentence = curSqlSentence.Replace("__ENTITY", entityColumn);
                }
                catch
                {
                    throw new Exception("No ENTITY column defined in the Template.\n");
                }
            }


            return curSqlSentence;
        }


    }

}