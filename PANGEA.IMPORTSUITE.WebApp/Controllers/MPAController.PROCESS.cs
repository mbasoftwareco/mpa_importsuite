using Excel;
using LINQtoCSV;
using PANGEA.IMPORTSUITE.DataModel;
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
using PANGEA.IMPORTSUITE.DataModel.MPA_SERVICE;
using PANGEA.IMPORTSUITE.ErpFactory;
using PANGEA.IMPORTSUITE.DataModel.MPA_RECON_DEPOSIT;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class MPAController : BaseController
    {
        public object MPA_DIREC_IMPORT { get; private set; }

        [NoCache]
        public ActionResult BANKRECONCIL_PROCESSPANEL(string TYPE_PROCESS,string START_DATE, string END_DATE, string BATCHNO)
        {
            if (string.IsNullOrEmpty(START_DATE))
                START_DATE = DateTime.Today.AddMonths(-1).ToString("MM/dd/yyyy");

            if (string.IsNullOrEmpty(END_DATE))
                END_DATE = DateTime.Today.ToString("MM/dd/yyyy");

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                EXTRA_DATA = START_DATE,
                VALUE = END_DATE,
                EXTRA_DATA3 = TYPE_PROCESS,
                EXTRA_DATA4 = BATCHNO
            };


            DataSet validationResult = _web.sp_MPA_RECONCIL("PROCESSPANEL", whObj);

            ViewBag.TYPE_PROCESS = TYPE_PROCESS;
            ViewBag.START_DATE = START_DATE;
            ViewBag.END_DATE = END_DATE;
            ViewBag.BATCHNO = BATCHNO;

            ViewBag.AllowManual = UtilTool.CheckPermit("MPA_BR_Manual", Session["LOGONUSER"].ToString(), _web._dbx);

            return View(validationResult.Tables[0]);

        }


        [NoCache]
        public JsonResult BANKRECONCIL_RECONCILE()
        {
            Server.ScriptTimeout = 3600;

            try
            {

                if (Request["RECON"] == null || string.IsNullOrEmpty(Request["RECON"]))
                    throw new Exception("No files selected to reconcile.");

                string regChecked = Request["RECON"].ToString();

                string resultMessage = "";

                string completed = "Conciliation process was ended successfully. ";

                bool hasPending = false;

                List<string> results = new List<string>();

                foreach (string curID in regChecked.Split(','))
                {
                    if (results.Contains(curID))
                        continue;

                    results.Add(curID);

                    //19 FEB Add batch to reconcile
                    string[] reconcile_data = curID.Split('-');

                    WH_Obj whObj = new WH_Obj
                    {
                        USUARIO = curUser,
                        GUID = reconcile_data[0],
                        EXTRA_DATA = reconcile_data[1]
                    };

                    //Validacion del Template
                    try
                    {
                        DataSet validationResult = _web.sp_MPA_RECONCIL("RECONCILE", whObj);

                        resultMessage += "\n\r" + curID + ": Completed!";

                        string reconResult = "0";

                        try { reconResult = validationResult.Tables[0].Rows[0][0].ToString(); }
                        catch { }

                        if ((reconResult == "0" || reconResult == "") && !hasPending)
                        {
                            completed = "Conciliation process ended with pending records. To process partially please contact your Manager, otherwise, upload the pending records to be fully reconcile. ";
                            hasPending = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        resultMessage += "\n\r" + curID + ": " + ex.Message;
                    }

                }

                

                object jsonData = new
                {
                    error = completed + resultMessage,
                    success = true,
                    guid = ""
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;

                return jsonResult;


            }
            catch (Exception e)
            {

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



        [NoCache]
        public JsonResult BANKRECONCIL_SENDERP()
        {

            Server.ScriptTimeout = 3600;

            string flag = "Init";


            try
            {

                if (Request["ERP"] == null || string.IsNullOrEmpty(Request["ERP"]))
                    throw new Exception("No files selected to send to ERP.");

                string resultMessage = "";
            
                //EPR
                string regChecked = Request["ERP"];

                if (!string.IsNullOrEmpty(regChecked))
                    resultMessage += process_ERP_Batches(regChecked, "ERP");


                //CHANGE STATUS TO BATCH PROCESSED

                WH_Obj whObj = new WH_Obj  { USUARIO = curUser  };

                try {  _web.sp_MPA_RECONCIL("DEPUREPANEL", whObj); } catch { }


                object jsonData = new
                {
                    error = "Process Completed." + resultMessage,
                    success = true,
                    guid = ""
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;

                return jsonResult;


            }
            catch (Exception e)
            {

                object jsonData = new
                {
                    error = flag + " " + e.Message,
                    success = false,
                    guid = ""
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;

                return jsonResult;

            }

        }



        private string process_ERP_Batches(string regChecked, string processType)
        {
            string flag = "Init";
            string resultMessage = "";

            foreach (string curID in regChecked.Split(','))
            {
                if (string.IsNullOrEmpty(curID))
                    continue;

                try
                {
                    flag = "Call resultDoc";


                    AX_MPA_Response resultDoc = null;


                    resultDoc = PrepareProcess_And_SendToErp(curID);


                    //JOURNAL
                    if (resultDoc.journal_OK)
                    {
                        flag = "Call JOURNAL";
                        UpdateERP_Result("OK", curID, resultDoc.journal_NUMBER, "JN");
                        resultMessage += "\n\r" + curID + ": JNL: " + resultDoc.journal_NUMBER;

                    }
                    else if (!string.IsNullOrEmpty(resultDoc.journal_NUMBER))
                    {
                        resultMessage += "\n\r" + curID + ": JNL: " + resultDoc.journal_NUMBER;
                    }

                    //DEPOSIT
                    if (resultDoc.deposit_OK)
                    {
                        flag = "Call deposit_OK";
                        UpdateERP_Result("OK", curID, resultDoc.deposit_NUMBER, "DEPOSIT");
                    }
                    else
                    {
                        UpdateERP_Result("ERROR", curID, resultDoc.deposit_NUMBER, "DEPOSIT");
                        flag = "Call deposit_ERROR";
                    }

                    resultMessage += ", DEP: " + resultDoc.deposit_NUMBER;

                }
                catch (Exception ex)
                {
                    resultMessage += "\n\r" + curID + ": " + flag + " " + ex.Message;

                    UpdateERP_Result("ERROR", curID, resultMessage, "JN");
                }
                
            }

            return resultMessage;

        }


        private void UpdateERP_Result(string resultType, string curID, string resultMessage, string resultDOC)
        {

            string[] curID_Batch = curID.Split('-');

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                EXTRA_DATA = resultType,
                GUID = curID_Batch[0],
                EXTRA_DATA3 = curID_Batch[1],
                VALUE = resultMessage
            };

            _web.sp_MPA_RECONCIL("ERP_RESULT_"+resultDOC, whObj);

        }



        private AX_MPA_Response PrepareProcess_And_SendToErp(string curID)
        {

            string flag = "Init-P";
            string entity = "MPA";
            string separator = ",";
            string GUID = Guid.NewGuid().ToString().Remove(8).ToUpper();
            string module = "BR";
            AX_MPA_Response response = new AX_MPA_Response { deposit_NUMBER = "" };

            try
            {
                string journalName = UtilTool.ObtenerParametro("RECON.JN", curConnection);

                string[] curID_Batch = curID.Split('-');  //0-HEADERID, 1-BATCHNUMBER

                WH_Obj whObj = new WH_Obj { USUARIO = curUser, EXTRA_DATA = "", GUID = curID_Batch[0], VALUE = curID_Batch[1] };

                flag = "ERPJOURNAL " + curID;

                DataSet dtJN = _web.sp_MPA_RECONCIL("ERPJOURNAL", whObj); //Obtiene data del Journal segun el curid to send to ERP

                flag = "after - dtJN";

                if (dtJN.Tables.Count == 0 || dtJN.Tables[0] == null)
                    throw new Exception("No data to send to ERP");

                string result = "";

                string onlyOneDeposit = dtJN.Tables[0].Rows[0]["OneDeposit"].ToString();


                if (dtJN.Tables[0].Rows[0]["Action"].ToString() == "JN")
                {

                    flag = "csvFile";

                    string csvFile = Create_CSV_File(GUID + ".csv", dtJN.Tables[0]);

                    csvFile = csvFile.Replace("/", "\\");

                    //Call th ax Service to ImportData
                    //CONNECTION PARAMETERS
                    string cnnType = "MPA.WS";  //_db.S_PARAMETERs.Where(f => f.CODE == "CNNTYPE").First().VALUE;
                    string cnnString = _db.S_PARAMETERs.Where(f => f.CODE == "CNNSTR").First().VALUE;
                    ConnectFactory axCnn = ConnectFactory.getConnectFactory(new ErpConnection { company = entity, cnnType = cnnType, cnnString = cnnString });

                    //Import
                    flag = "Import";
                    result = axCnn.ErpService().ImportData(csvFile, GUID, separator, GUID, module, entity);

                    //Validate
                    flag = "Validate";
                    result = axCnn.ErpService().ValidateData(GUID, module, entity);

                    //Process
                    string autoPost = "1";


                    //JOURNAL CREATION
                    flag = "JOURNAL CREATION";
                    result = axCnn.ErpService().ProcessData(GUID, module, entity, journalName, "JM", autoPost);
                    response.journal_NUMBER = result;
                    response.journal_OK = true;


                    //DEPOSIT CREATION
                    flag = "DEPOSIT CREATION";
                    try
                    {

                        DataModel.MPA_RECON_DEPOSIT.CallContext myContextR = new DataModel.MPA_RECON_DEPOSIT.CallContext { Company = entity };
                        
                        MPABankReconciliationClient ccr = new MPABankReconciliationClient();

                        string depositInfo = "<Payments><Payment><TransType>DATA</TransType><CreditType></CreditType><BatchNum>" + curID_Batch[1] + "</BatchNum><JournalNum>" + result + "</JournalNum></Payment></Payments>";

                        DataSet dsDeposit = UtilTool.GetDataSetFromString(depositInfo);

                        string ccresult = ccr.reconciliation(myContextR, dsDeposit);
                    
                        DataSet dsResponse = UtilTool.GetDataSetFromString(ccresult);

                        response.deposit_OK = false;

                        //Revisar el Dataset para sacar el Deposito
                        /*
                        try
                        {
                            string hasError = dsResponse.Tables[0].Rows[0]["IsError"].ToString();

                            if (hasError == "No")
                            {
                                response.deposit_NUMBER = dsResponse.Tables[0].Rows[0]["DepositSlip"].ToString();
                                response.deposit_OK = true;
                            }
                            else
                                response.deposit_NUMBER = dsResponse.Tables[0].Rows[0]["Description"].ToString();
                        }
                        catch
                        {

                        }
                        */


                        try
                        {
                            int kzi = 0;
                            string s_e_p = "";

                            foreach (System.Data.DataRow dd in dsResponse.Tables[0].Rows)
                            {
                                string hasError = dd["IsError"].ToString();

                                if (kzi > 0)
                                    s_e_p = ", ";

                                if (hasError == "No")
                                {
                                    if (!response.deposit_NUMBER.Contains(dd["DepositSlip"].ToString()))
                                        response.deposit_NUMBER += dd["DepositSlip"].ToString() + s_e_p;

                                    response.deposit_OK = true;
                                    kzi++;
                                }
                                //else
                                //errMsg += dd["Description"].ToString() + ", ";

                                
                            }
                        }
                        catch
                        {

                        }

                        /*
                         * Forma vieja de obtener los depositos ants del dataset.
                        try
                        {
                            if (ccresult[0].IsExeption)
                            {
                                response.deposit_NUMBER = ccresult[0].ExeptionMessage;
                                response.deposit_OK = false;
                            }
                            else
                            {
                                string depositNumber = "";
                                string separatorx = "";

                                for (int i = 0; i < ccresult.Length; i++)
                                {
                                    if (i > 0)
                                        separatorx = ", ";

                                    if (!string.IsNullOrEmpty(ccresult[i].BankDepositNum))
                                        depositNumber += separatorx + ccresult[i].BankDepositNum;
                                }

                                response.deposit_NUMBER = depositNumber;
                                response.deposit_OK = true;
                            }

                        }
                        catch
                        {

                        }
                        */

                    }
                    catch (Exception ex)
                    {
                        response.deposit_NUMBER = ex.Message;
                    }


                    result = "Journal:" + response.journal_NUMBER + ", Deposit: " + response.deposit_NUMBER;

                }
                else if (dtJN.Tables[0].Rows[0]["Action"].ToString() == "DEPOSIT")
                {
                    int count = 0;

                    MPABankReconciliationClient ccr = new MPABankReconciliationClient();

                    List<string> listOfProcessedRegs = new List<string>();

                    string errMsg = "";

                    response.deposit_OK = false;

                    string depositInfo = "";

                    foreach (System.Data.DataRow dr in dtJN.Tables[0].Rows)
                    {
                        count++;

                        if (count == 1)
                            continue;
            
                        string curjournal_NUMBER = dr["JOURNALNUMBER"].ToString();

                        flag = "DEPOSIT CREATION2";                        

                        //Recorre Different Journals
                        string[] listOfJournals = curjournal_NUMBER.Split(',');
                       
                        string batchNum = dr["PaymReference"].ToString();

                        string cardType = dr["MPACreditCardType"].ToString();

                        int pp =  0;

                        foreach (string curJN in listOfJournals)
                        {
                            if (string.IsNullOrEmpty(curJN.Trim()))
                                continue;

                            if (listOfProcessedRegs.Contains(batchNum + "-" + curJN.Trim() + "-" + cardType))
                                continue;                          

                            if (!string.IsNullOrEmpty(onlyOneDeposit))
                                depositInfo += "<Payment><TransType>" + onlyOneDeposit + "</TransType><CreditType>"+ cardType + "</CreditType><BatchNum>" + batchNum + "</BatchNum><JournalNum>" + curJN.Trim() + "</JournalNum></Payment>";
                            else
                                depositInfo += "<Payment><TransType>DATA</TransType><CreditType></CreditType><BatchNum>" + batchNum + "</BatchNum><JournalNum>" + curJN.Trim() + "</JournalNum></Payment>";

                            //Adding ig to the process
                            listOfProcessedRegs.Add(batchNum + "-" + curJN.Trim() + "-" + cardType);

                            pp++;
                        }

                    }


                    depositInfo = "<Payments>" + depositInfo + "</Payments>";

                    DataSet dsDeposit = UtilTool.GetDataSetFromString(depositInfo);

                    DataModel.MPA_RECON_DEPOSIT.CallContext myContextR = new DataModel.MPA_RECON_DEPOSIT.CallContext { Company = entity };

                    string ccresult = ccr.reconciliation(myContextR, dsDeposit);

                    DataSet dsResponse = UtilTool.GetDataSetFromString(ccresult);

                    try
                    {
                        foreach (System.Data.DataRow dd in dsResponse.Tables[0].Rows)
                        {
                            string hasError = dd["IsError"].ToString();

                            if (hasError == "No")
                            {
                                if (!response.deposit_NUMBER.Contains(dd["DepositSlip"].ToString()))
                                    response.deposit_NUMBER += dd["DepositSlip"].ToString() + ", ";

                                response.deposit_OK = true;
                            }
                            else
                                errMsg += dd["Description"].ToString() + ", ";

                        }
                    }
                    catch (Exception ex)
                    {

                    }


                    if (response.deposit_OK == false)
                        response.deposit_NUMBER = errMsg;   

                }

            }
            catch (Exception ex)
            {
                throw new Exception(flag + " " + ex.Message.Replace("'",""));
            }

            return response;

        }




        private string Create_CSV_File(string filename, DataTable dt)
        {
            //Return the CSV file path

            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName);

            sb.AppendLine(string.Join(",", columnNames));

            foreach (System.Data.DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());

                sb.AppendLine(string.Join(",", fields));

                /* poniendo finzalizadore sde cadena
                 foreach (DataRow row in dt.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => 
                      string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));
                    sb.AppendLine(string.Join(",", fields));
                }
                */
            }

            //Create and Save CSV file - La ruta sale de un parametro
            string uploadsFilesPath = UtilTool.ObtenerParametro("FILEUPLOAD", curConnection);

            uploadsFilesPath += DateTime.Today.Year.ToString() + "/RECONCILE";

            if (!Directory.Exists(uploadsFilesPath))
                Directory.CreateDirectory(uploadsFilesPath);

            uploadsFilesPath = Path.Combine(uploadsFilesPath, filename);

            System.IO.File.WriteAllText(uploadsFilesPath, sb.ToString());

            return uploadsFilesPath;

        }


        [NoCache]
        public JsonResult REMOVE_FILE(int rid)
        {

            Server.ScriptTimeout = 3600;

            try
            {

                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    GUID = rid.ToString()
                };

                _web.sp_MPA_RECONCIL("DELETE_RECON_FILE", whObj);


                object jsonData = new
                {
                    error = "Process Completed.",
                    success = true,
                    guid = ""
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;

                return jsonResult;

            }
            catch (Exception e)
            {

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


        [NoCache]
        public JsonResult REMOVE_BATCH(int rid, string batchNo)
        {

            Server.ScriptTimeout = 3600;

            try
            {

                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    GUID = rid.ToString(),
                    EXTRA_DATA = batchNo
                };

                _web.sp_MPA_RECONCIL("DELETE_RECON_BATCH", whObj);


                object jsonData = new
                {
                    error = "Process Completed.",
                    success = true,
                    guid = ""
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;

                return jsonResult;

            }
            catch (Exception e)
            {

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

        [NoCache]
        public ActionResult BANKRECONCIL_PROCESSPENDING(int rid, string batchNo)
        {

            WH_Obj whObj = new WH_Obj
            {
                USUARIO = curUser,
                ROWID = rid,
                EXTRA_DATA = batchNo

            };

            DataSet ds = _web.sp_MPA_RECONCIL("PENDING_RECORDS", whObj);
       
            ViewBag.Title = "Pending Records to Reconcile for Batch " + batchNo;

            //ds.Tables[0].Columns.Add("Facility");

            return PartialView(ds.Tables[0]);

        }
        [NoCache]
        public JsonResult UPDATE_BANKRECONCIL_PROCESSPENDING(int rid, string facility, string reconcile, string batchNo)
        {

            try
            {

                WH_Obj whObj = new WH_Obj
                {
                    USUARIO = curUser,
                    ROWID = rid,
                    EXTRA_DATA = facility,
                    VALUE = batchNo

                };

                DataSet result = null;

                if (reconcile == "F")
                {
                    result = _web.sp_MPA_RECONCIL("PENDING_SAVE", whObj);

                    if (result.Tables[0].Rows[0][0].ToString() == "0")
                    {
                        throw new Exception("");
                    }
                }
                else
                    result = _web.sp_MPA_RECONCIL("RECONCILE_MANUAL", whObj);
                

                object jsonData = new
                {
                    error = "Process Completed.",
                    success = true,
                    guid = ""
                };

                var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;

                return jsonResult;

            }
            catch (Exception e)
            {

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

    }

    public class AX_MPA_Response
    {
        public bool journal_OK { get; set; }
        public string journal_NUMBER { get; set; }

        public bool deposit_OK { get; set; }
        public string deposit_NUMBER { get; set; }
    }

}
 