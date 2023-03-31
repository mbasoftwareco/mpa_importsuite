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
using PANGEA.IMPORTSUITE.DataModel.AX_IMPORT;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class PROCESSController : BaseController
    {

        #region :: IMPORT TEMPLATE




        //VIEW CON VALIDACION DEL IMPORT
        public ActionResult IMPORT_TEMPLATE_VALIDATE_RESULT(string GUID, bool mustValidate)
        {
            //ROWID_TEMPLATE = 1;
            //GUID = "20F0DA82";

            T_TEMPLATE_RUNTIME upTplRuntime = _web._dbx.T_TEMPLATE_RUNTIMEs.Where(f => f.GUID == GUID).First();

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                GUID = GUID,
                ROWID = upTplRuntime.ROWID_TEMPLATE,
            };

            //STATUS DEFINITION
            //0 - INTIAL
            //2 - ERROR
            //100 - VALIDATED
            //1 - PROCESSED OK
            //10 - POSTED



            //Validacion del Template
            DataSet validationResult = null;

            if (mustValidate)
                validationResult = VALIDATON_ProcessTemplate(whObj, upTplRuntime);
            else
                validationResult = _web.sp_IMPORT_DATA("TEMPLATE_VALIDATION_LINE_END", whObj);


            return PartialView(validationResult.Tables[0]);

        }


        #region :: IMPORT_TEMPLATE_RESULT

        public ActionResult IMPORT_TEMPLATE_RESULT(int ROWID_TEMPLATE, string GUID, string FILTER)
        {

            return View();
        }

        [NoCache]
        public JsonResult IMPORT_TEMPLATE_RESULT_DATA(int ROWID_TEMPLATE, string GUID, string FILTER)
        {

            WH_Obj WH_Obj = new WH_Obj
            {
                USUARIO = curUser,
                GUID = GUID,
                ROWID = ROWID_TEMPLATE,
                EXTRA_DATA = FILTER
            };


            DataSet dsResult = _web.sp_IMPORT_DATA("TMP_DATA", WH_Obj);


            #region ::  HEADER

            List<string> colHeaders = new List<string>();
            foreach (DataColumn colm in dsResult.Tables[0].Columns)
                colHeaders.Add(colm.Caption);

            #endregion


            #region ::  DATA

            //List<string[]> data = new List<string[]>();
            //foreach (System.Data.DataRow row in dsResult.Tables[0].Rows)
            //    data.Add(row.ItemArray.Select(x => x.ToString()).ToArray());

            DataTable dtTable = dsResult.Tables[0];
            List<T_TMP_UPLOAD_RECORD> data = dtTable.DataTableToList<T_TMP_UPLOAD_RECORD>();

            #endregion


            #region ::  COLUMNS          

            Array columns = (from listado in dsResult.Tables[1].AsEnumerable()
                             select new Handsontable()
                             {
                                 data = listado["data"].ToString(),
                                 type = listado["type"].ToString(),
                                 format = listado["format"].ToString(),
                                 dateFormat = listado["dateFormat"].ToString(),
                                 width = listado["width"].ToString()
                             }).ToArray();

            #endregion


            object result = new { header = colHeaders, columns = columns, data = data };

            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [NoCache]
        public JsonResult UPDATE_TMP_DATA(int ROWID, string GUID, string FIELD, string VALUE)
        {
            try
            {

                WH_Obj WH_Obj = new WH_Obj
                {
                    USUARIO = curUser,
                    ROWID = ROWID,
                    FIELD = FIELD,
                    VALUE = VALUE,
                    GUID = GUID,
                };


                DataSet dsResult = _web.sp_IMPORT_DATA("UPDATE_TMP_DATA", WH_Obj);


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


        #endregion



        public void IMPORT_TEMPLATE_SENDERP(int ROWID_TEMPLATE, string GUID)
        {
            string ini = DateTime.Now.ToString();

            //GUID = "20F0DA82";

            //save data on file

            //#1. Get Config folder from general parameters where put Dataser file to upload
            //string uploadFilePath = @"C:\JM-PROJECTS\DEV_PANGEA\PANGEA.IMPORTSUITE\RESOURCES";
            string uploadFilePath = @"C:\PANGEA_IMPORTSUITE\RESOURCES";
 

            uploadFilePath = Path.Combine(uploadFilePath, ROWID_TEMPLATE.ToString() + "_" + GUID + ".txt");

            #region Prepare SQL Sentence

            List<T_TEMPLATE_FIELD> fieldList =  _web._dbx.T_TEMPLATE_FIELDs.Where(f => f.ROWID_TEMPLATE == ROWID_TEMPLATE)
                                                .OrderBy(f => f.SEQUENCE).ToList();

            string sqlQuery = "SELECT ";

            
            foreach (T_TEMPLATE_FIELD field in fieldList)
            {
                if (field.SEQUENCE > 1)
                    sqlQuery += ",";

                sqlQuery += " C" + field.SEQUENCE + " AS [" + field.FIELD_DEST_NAME.Trim() + "] " ;

            }

            sqlQuery += " FROM [dbo].[T_TMP_UPLOAD_RECORDS] where GUID = '" + GUID + "'";

            #endregion

            //Create a DataSet from SQL TABLE
            //Save DataSet to FILE
            DataSet ds = SQLBase.ReturnDataSet(sqlQuery,  _web._sqlCnn);

            ds.WriteXml(uploadFilePath);
          
            string fin1 = DateTime.Now.ToString();


            //CALL THE ERP METHOD
             PANGEA_IMPORT_SUITEClient svcCliente = new PANGEA_IMPORT_SUITEClient();
             string result = svcCliente.dataUploadByTemplate(new CallContext { Company = "MPA" }, uploadFilePath, "TESTING "+ GUID, "GL" );


            string fin2 = DateTime.Now.ToString();

            /*
               <Table>
    <TRANDATE>01/12/2016</TRANDATE>
    <ACCOUNTTYPE>Ledger</ACCOUNTTYPE>
    <MAINACCOUNT>7300</MAINACCOUNT>
    <DIM-COUNTRY>US</DIM-COUNTRY>
    <DIM-STATE>AK</DIM-STATE>
    <DIM-SITE>USAK5007</DIM-SITE>
    <DIM-DEPARTMENT />
    <DIM-LEASE />
    <DIM-PARTNERS />
    <LEGALENTITY>VBA2</LEGALENTITY>
    <DESCRIPTION>Dep Exp Entity Reclass</DESCRIPTION>
    <DEBIT />
    <CREDIT>465.02</CREDIT>
    <REV_ENTRY>No</REV_ENTRY>
    <REV_DATE />
  </Table>
             */
        }






    }

}