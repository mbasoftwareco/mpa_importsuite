using Excel;
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
using Microsoft.Reporting.WebForms;
using System.Drawing.Printing;
using System.Drawing.Imaging;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{

    public partial class MPAController : BaseController
    {


        #region BILLING INVOICE FLAT

        [NoCache]
        public ActionResult Billing_Invoice_FLAT()
        {

            _web._dbx.CommandTimeout = 3600;
            HttpContext.Server.ScriptTimeout = 3600000;
            string cnnstr = _web._dbx.Connection.ConnectionString;


            if (Request.Params["load"] == "1")
            {

                string strFilters = GetFilters();

                DataTable dtInvoice = SQLBase.ReturnDataSet_WithException("EXEC spBilling_Invoice_FLAT @OPTION = 'SEARCH' " + strFilters, new SqlConnection(cnnstr)).Tables[0];

                if (dtInvoice == null)
                {
                    ViewBag.Printer = "<font color=red>Error trying to get results. </font>";
                    return View();
                }

                dtInvoice = Invoice_PROCESS_ORDER_DataTable(dtInvoice, Request.Params["sidx"], Request.Params["sort"]);

                if (string.IsNullOrEmpty(Request.Params["sidx"].ToString()))
                    ViewBag.sidx = "Customer";
                else
                    ViewBag.sidx = Request.Params["sidx"];

                if (string.IsNullOrEmpty(Request.Params["sort"].ToString()))
                    ViewBag.sort = "ASC";
                else
                    ViewBag.sort = Request.Params["sort"];


                ViewBag.InvoiceList = dtInvoice;

                //This Session help to not go to database again
                Session["DT_INVOICE"] = dtInvoice;

            }

            else if (Request.Params["load"] == "2") //SORT User Session DATATABLE
            {

                DataTable dtInvoice = (DataTable)Session["DT_INVOICE"];

                dtInvoice = Invoice_PROCESS_ORDER_DataTable(dtInvoice, Request.Params["sidx"], Request.Params["sort"]);

                if (string.IsNullOrEmpty(Request.Params["sidx"].ToString()))
                    ViewBag.sidx = "Customer";
                else
                    ViewBag.sidx = Request.Params["sidx"];

                if (string.IsNullOrEmpty(Request.Params["sort"].ToString()))
                    ViewBag.sort = "ASC";
                else
                    ViewBag.sort = Request.Params["sort"];


                ViewBag.InvoiceList = dtInvoice;

            }

            return View();

        }



        private string GetFilters()
        {
            string strFilters = "";

            if (!string.IsNullOrEmpty(Request.Params["SITE"]))
                strFilters += ", @SITE='" + Request.Params["SITE"].ToUpper() + "'";


            if (!string.IsNullOrEmpty(Request.Params["ENTITY"]))
            {
                string tmpEntity = "";

                string[] entityKey = Request.Form.GetValues("ENTITY");

                foreach (string curKey in entityKey)
                    tmpEntity += "#" + curKey;

                strFilters += ", @ENTITY='" + tmpEntity + "#'";
            }


            if (!string.IsNullOrEmpty(Request.Params["FDELIVERY"]))
            {
                string tmpDel = "";

                string[] delKey = Request.Form.GetValues("FDELIVERY");

                foreach (string curKey in delKey)
                    tmpDel += "#" + curKey;

                strFilters += ", @DELIVERY='" + tmpDel + "#'";
            }



            if (!string.IsNullOrEmpty(Request.Params["KEY"]))
                strFilters += ", @KEY='" + Request.Params["KEY"].ToUpper() + "'";

            if (!string.IsNullOrEmpty(Request.Params["dateFrom"]))
                strFilters += ", @INV1='" + Request.Params["dateFrom"].ToUpper() + "'";

            if (!string.IsNullOrEmpty(Request.Params["dateTo"]))
                strFilters += ", @INV2='" + Request.Params["dateTo"].ToUpper() + "'";


            if (!string.IsNullOrEmpty(Request.Params["creaFrom"]))
                strFilters += ", @CRE1='" + Request.Params["creaFrom"].ToUpper() + "'";

            if (!string.IsNullOrEmpty(Request.Params["creaTo"]))
                strFilters += ", @CRE2='" + Request.Params["creaTo"].ToUpper() + "'";



            return strFilters;

        }




        [NoCache]
        public string Billing_Invoice_PROCESS_FLAT()
        {

            _web._dbx.CommandTimeout = 86000;
            HttpContext.Server.ScriptTimeout = 8600000;
            string xGUID = Guid.NewGuid().ToString().Remove(8).ToUpper();

            /* 
            string strFilters = GetFilters();

            DataTable dtInvoice = SQLBase.ReturnDataTable("EXEC spBilling_Invoice_FLAT @OPTION = 'SEARCH' " + strFilters, "INV", new SqlConnection(cnnstr));

            dtInvoice = Invoice_PROCESS_ORDER_DataTable(dtInvoice, Request.Params["sidx"], Request.Params["sort"]);
            */

            DataTable dtInvoice = ((DataTable)Session["DT_INVOICE"]);

            string invoiceList = "";

            foreach (DataRow curInv in dtInvoice.Rows)
            {
                if (Request.Params["_" + curInv["RECID"].ToString()] == "1")
                    invoiceList += curInv["INVOICEID"].ToString() + ", ";
            }


            if (string.IsNullOrEmpty(invoiceList))
                return "Error: No invoices to process";


            string msg = "";
            try
            {
                if (Request.Params["load"] == "P")
                    msg = Billing_Invoice_SaveToPrint(invoiceList, xGUID);

                else if (Request.Params["load"] == "E")
                    //msg = Billing_Invoice_SendEmail(invoiceList);
                    //New Version that send allthe process to be executed in background
                    //For the pangea Service.  JM 3/AGO/2017
                    msg = Billing_Invoice_SendEmail_V2(invoiceList, xGUID);

            }
            catch (Exception ex)
            {
                msg = "Error: " + ex.Message;
                return msg;
            }


            return "ok|Invoices will be printed, process could take some minutes !" + msg;
        }



        public DataTable Invoice_PROCESS_ORDER_DataTable(DataTable regList, string sidx, string sort)
        {
            DataView dv = regList.DefaultView;

            if (sidx == "Lease")
                dv.Sort = "lease " + sort;


            else if (sidx == "Entity")
                dv.Sort = "LegalEntity " + sort;


            else if (sidx == "Customer")
                dv.Sort = "INVOICEACCOUNT " + sort;


            else if (sidx == "Site")
                dv.Sort = "Site " + sort;


            else if (sidx == "Invoice")
                dv.Sort = "INVOICEID " + sort;

            else if (sidx == "Date")
                dv.Sort = "INVOICEDATE " + sort;


            else if (sidx == "Amount")
                dv.Sort = "amount " + sort;


            else if (sidx == "CreatedOn")
                dv.Sort = "CreDate " + sort;


            else if (sidx == "DeliveryMethod")
                dv.Sort = "EMAIL " + sort;

            else
                dv.Sort = "INVOICEACCOUNT " + sort;

            DataTable sortedDT = dv.ToTable();

            regList = sortedDT;

            return regList;

        }


        [NoCache]
        //JM 10/OCT/2017 Export Invoice INfo to Excel
        public String Billing_Invoice_PROCESS_FLAT_Export()
        {
            string strFile = "Invoices";

            DataTable dtInvoice = ((DataTable)Session["DT_INVOICE"]);

            //DT-INVOICEDATE	INVOICEACCOUNT	INVOICEID	CURRENCYCODE	amount	Customer	lease	Site	RECID	ImportKey	EMAIL	CreDate	InvFormType	LegalEntity	DESCRIPTION	ADQUISITION

            //EXCEL-Code,Lease,Entity,Customer,Site,Invoice,Date,Amount, CreatedOn, Delivery Method, Acquisition 

            //NEW-SEQ, lease, LegalEntity, INVOICEACCOUNT+Customer, Site, INVOICEID, INVOICEDATE, amount, CreDate, EMAIL, ADQUISITION

            DataSet ds = new DataSet();

            DataTable newTable = dtInvoice.DefaultView.ToTable(false, "lease", "LegalEntity", "INVOICEACCOUNT", "Customer", "Site", "INVOICEID", "INVOICEDATE", "amount", "CreDate", "EMAIL", "ADQUISITION");

            newTable.Columns["lease"].ColumnName = "Lease";
            newTable.Columns["LegalEntity"].ColumnName = "Entity";
            newTable.Columns["INVOICEACCOUNT"].ColumnName = "Customer ID";
            newTable.Columns["Customer"].ColumnName = "Customer Name";
            newTable.Columns["Site"].ColumnName = "Site";
            newTable.Columns["INVOICEID"].ColumnName = "Invoice";
            newTable.Columns["INVOICEDATE"].ColumnName = "Date";
            newTable.Columns["amount"].ColumnName = "Amount";
            newTable.Columns["CreDate"].ColumnName = "CreatedOn";
            newTable.Columns["EMAIL"].ColumnName = "Delivery Method";
            newTable.Columns["ADQUISITION"].ColumnName = "Acquisition";


            //INSERT Identity column
            DataColumn c = new DataColumn("Code");
            c.AutoIncrement = true;
            c.AutoIncrementSeed = 1;
            c.AutoIncrementStep = 1;
            newTable.Columns.Add(c);
            c.SetOrdinal(0);

            //Set values for existing rows
            for (int i = 1; i <= newTable.Rows.Count; i++)
                newTable.Rows[i - 1]["Code"] = i;

            ds.Tables.Add(newTable);

            return WriteExcelDocument(ds, strFile + "_" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".xlsx");

        }


        private string WriteExcelDocument(DataSet ds, string fileName)
        {

            string name = fileName;
            string finalPath = Server.MapPath("/PrintReport/");

            if (!Directory.Exists(finalPath))
                Directory.CreateDirectory(finalPath);

            ExportToExcelOXML.CreateExcelDocument(ds, finalPath + name);

            return "/PrintReport/" + name;
        }



        #endregion



        public string Billing_Invoice_SendEmail_V2(string invList, string xGUID)
        {
            HttpContext.Server.ScriptTimeout = 8600000;

            List<string> processedInvoices = new List<string>();

            IMPORTSUITE_DAODataContext _db = new IMPORTSUITE_DAODataContext();

            //Lista de CustomerInvoices
            List<vw_AX_CustomerInvoice> axCustomerInvoiceList = _db.vw_AX_CustomerInvoices.ToList();
            //Lista de AX_contacts
            List<vw_AX_CustomerInvoices_Contact> axContactList = _db.vw_AX_CustomerInvoices_Contacts.ToList();


            foreach (string invoice in invList.Split(','))
            {

                if (string.IsNullOrEmpty(invoice.Trim()))
                    continue;

                //JM : 2017/JUL/14  Check the processed list to prevent duplicity
                if (processedInvoices.Contains(invoice.Trim()))
                    continue;

                processedInvoices.Add(invoice.Trim());


                //vw_AX_CustomerInvoice invTransaction = _db.vw_AX_CustomerInvoices.Where(f => f.INVOICEID.Trim() == invoice.Trim()).First();
                vw_AX_CustomerInvoice invTransaction = axCustomerInvoiceList.Where(f => f.INVOICEID.Trim() == invoice.Trim()).First();

                string fmType = string.IsNullOrEmpty(invTransaction.InvFormType) ? "A" : invTransaction.InvFormType;

                //string rdlPath = Path.Combine(Server.MapPath("~/Forms"), "VB_INVOICE_" + fmType + ".rdl");
                string rdlPath = UtilTool.ObtenerParametro("INVOICE.FORM", curConnection);

                string mail_invoice_List = "";

                //List<vw_AX_CustomerInvoices_Contact> custContactList = _db.vw_AX_CustomerInvoices_Contacts.Where(f => f.ACCOUNTNUM == invTransaction.INVOICEACCOUNT).ToList();
                List<vw_AX_CustomerInvoices_Contact> custContactList = axContactList.Where(f => f.ACCOUNTNUM == invTransaction.INVOICEACCOUNT).ToList();

                //Check for the contact table when record have EMAIL or FAX (electronic Fax)
                foreach (vw_AX_CustomerInvoices_Contact contact in custContactList)
                {
                    try
                    {
                        ReportFormHelper ph = new ReportFormHelper();

                        if (contact.TypeName == "E-Mail" && contact.LOCATOR.Contains("@"))
                        {
                            //Buscar los CC Emails
                            string cc_address = "";

                            foreach (vw_AX_CustomerInvoices_Contact cce in custContactList.Where(f => f.TypeName == "CCMail"))
                                cc_address += cce.LOCATOR + ";";

                            //CC + Primary Mail Contact
                            cc_address += contact.LOCATOR;

                            try
                            {
                                //New V2 sends the process to background JM 3/AGO/2017
                                ph.Document_SendByEmail_V2(rdlPath, invTransaction, cc_address, "INVOICE", xGUID);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                continue;

                            }

                        }
                        else if (contact.TypeName == "Mail" || contact.TypeName == "Fax")
                            mail_invoice_List += invTransaction.INVOICEID + ",";


                    }
                    catch (Exception ex)
                    {
                        return "Error: " + ex.Message;
                    }
                }


                //Sent to print the MAIL Invoices
                if (!string.IsNullOrEmpty(mail_invoice_List))
                    Billing_Invoice_SaveToPrint(mail_invoice_List, xGUID);

            }

            //Run send email process
            //Sned mail will be processed in the task schedule.
            //MailSender.SendPendingMessages();

            return "ok|Process completed !";

        }


        public string Billing_Invoice_SaveToPrint(string invList, string xGUID)
        {
            HttpContext.Server.ScriptTimeout = 3600000;

            string curPrinter = "";

            string curUser = "";
            try { curUser = Request.LogonUserIdentity.Name.ToString(); }
            catch { }

            foreach (string invoice in invList.Split(','))
            {
                if (string.IsNullOrEmpty(invoice))
                    continue;

                //Bypass if the invoice is already in the print list
                if (_web._dbx.T_PrinterPools.Any(f => f.DocNumber.Trim() == invoice.Trim() && f.ProcessDate != null && f.ProcessDate.Value.Date == DateTime.Today))
                    continue;

                try
                {
                    T_PrinterPool pp = new T_PrinterPool
                    {
                        DocNumber = invoice.Trim(),
                        DocType = "INV",
                        Printer = curPrinter,
                        PrintUser = curUser,
                        RegDate = DateTime.Now,
                        xGUID = xGUID
                    };

                    _web._dbx.T_PrinterPools.InsertOnSubmit(pp);
                    _web._dbx.SubmitChanges();
                }
                catch (Exception ex)
                {
                    return "Error: " + ex.Message;
                }
            }

            return "ok|Invoices will be printed, process could take some minutes !";

        }



        public ActionResult Invoice_Form(string invoice_id)
        {
            LocalReport lr = new LocalReport();

            invoice_id = invoice_id.Trim();

            //Apr 30 Look for right Form fil por ecah legal entity
            string RDL_FORM = UtilTool.getRDLFromInvoice(invoice_id);

            //string path = Path.Combine(Server.MapPath("~/Forms"), "VB_INVOICE.rdl");
            string path = Path.Combine(Server.MapPath("~/PrintReport/Forms"), RDL_FORM);

            if (System.IO.File.Exists(path))
            {
                lr.ReportPath = path;
            }
            else
            {
                throw new Exception("Report not found: " + path);
            }


          
            DataSet cm = SQLBase.ReturnDataSet("EXEC spGetInvoiceData  @Invoice_ID='" + invoice_id + "'", new SqlConnection((new IMPORTSUITE_DAODataContext()).Connection.ConnectionString));

            for (int i = 0; i < cm.Tables.Count; i++)
                lr.DataSources.Add(new ReportDataSource("DataSet" + (i + 1).ToString(), cm.Tables[i]));

            string reportType = "PDF";
            string mimeType;
            string encoding;
            string fileNameExtension = "PDF";



            string deviceInfo =

            "<DeviceInfo>" +
            "  <OutputFormat>" + "PDF" + "</OutputFormat>" +
            "  <PageWidth>8.5in</PageWidth>" +
            "  <PageHeight>11in</PageHeight>" +
            "  <MarginTop>0.5cm</MarginTop>" +
            "  <MarginLeft>0cm</MarginLeft>" +
            "  <MarginRight>0cm</MarginRight>" +
            "  <MarginBottom>0.5cm</MarginBottom>" +
            "</DeviceInfo>";

            Warning[] warnings;
            string[] streams;
            byte[] renderedBytes;

            renderedBytes = lr.Render(
                reportType,
                deviceInfo,
                out mimeType,
                out encoding,
                out fileNameExtension,
                out streams,
                out warnings);


            string downloadFile = invoice_id + "_" + Guid.NewGuid().ToString().Remove(8) + ".pdf";
            return File(renderedBytes, mimeType, downloadFile);
        }


        [NoCache]
        public ActionResult Billing_Delivery_Report()
        {
            HttpContext.Server.ScriptTimeout = 3600000;
            return View();

        }

        [NoCache]
        public JsonResult Billing_Delivery_Data(string sidx, string sord, int page, int rows)
        {

          
            HttpContext.Server.ScriptTimeout = 3600000;

            DataSet ds = SQLBase.ReturnDataSet("EXEC spBilling_PrintReport @OPTION = 1", 
                new SqlConnection((new IMPORTSUITE_DAODataContext()).Connection.ConnectionString));

            DataTable dt = ds.Tables[0];

            int pageIndex = page; // get the requested page
            int pageSize = rows; // get how many rows we want to have into the grid
            string sidxx = sidx;
            int startRow = (pageIndex * pageSize) + 1;
            int totalRecords = dt.Rows.Count;
            int totalPages;
            int start;
            if (totalRecords > 0)
                totalPages = (int)Math.Ceiling((float)totalRecords / (float)pageSize);
            else
                totalPages = 0;

            if (pageIndex > totalPages)
                pageIndex = totalPages;

            if (totalRecords == 0) //si no hay registros
                start = 0;
            else
                start = pageSize * pageIndex - pageSize;


            object jsonData = null;

            //FileName                                                                                             
            //rDate      Invoice     ToPrint     Printed     ToEmail     EmailSent   NoContact

            Array listaRegistros = (from listado in dt.AsEnumerable()
                                    select new
                                    {
                                        cell = new object[] {
                                        listado["FileName"],
                                        listado["fDate"],
                                        listado["Invoice"],
                                        listado["ToPrint"],
                                        listado["ToEmail"],
                                        listado["NoContact"],
                                        listado["Printed"],
                                        listado["EmailSent"],
                                        listado["AmountToPrint"],
                                        listado["AmountToEmail"],
                                        listado["AmountNoContact"],

                                     }
                                    }).Skip(start).Take(pageSize).ToArray();



            jsonData = new
            {
                total = totalPages,
                pageIndex,
                records = totalRecords,
                rows = listaRegistros,
            };

            var jsonResult = Json(jsonData, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;


        }

        [NoCache]
        public String Export_Billing_Delivery()
        {
            string strFile = Request["curFile"];

            DataSet ds = SQLBase.ReturnDataSet("EXEC spBilling_PrintReport @OPTION = 2, @FILE='" + strFile + "'",
                new SqlConnection((new IMPORTSUITE_DAODataContext()).Connection.ConnectionString));

            return WriteExcelDocument(ds, strFile + "_" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".xlsx");

        }

        [NoCache]
        public ActionResult Billing_Delivery()
        {

            var StartDate = Request["StartDate"];
            var EndDate = Request["EndDate"];
            ViewBag.DatesIsNull = false;

            if (string.IsNullOrEmpty(StartDate) || string.IsNullOrEmpty(EndDate))
            {
                ViewBag.DatesIsNull = true;
                return View();

            }


            DataSet ds = SQLBase.ReturnDataSet("EXEC spBilling_Delivery @OPTION ='1', @START_DATE ='" + StartDate + "', @END_DATE ='" + EndDate + "'",
              new SqlConnection((new IMPORTSUITE_DAODataContext()).Connection.ConnectionString));


            ViewBag.Data = ds.Tables[0];

            return View();

        }

        [NoCache]
        public ActionResult Billing_Delivery_Details()
        {

            var StartDate = Request["StartDate"];
            var EndDate = Request["EndDate"];
            var TypeRecord = Request["Record"];


            DataSet ds = SQLBase.ReturnDataSet("EXEC spBilling_Delivery @OPTION ='2', @EXTRA_DATA ='" + TypeRecord + "', @START_DATE ='" + StartDate + "', @END_DATE ='" + EndDate + "'",
              new SqlConnection((new IMPORTSUITE_DAODataContext()).Connection.ConnectionString));

            ViewBag.Data = ds.Tables[0];

            return View(ds.Tables[0]);

        }
    }

    public class BatchPrintProcess
    {
        // documentList, appPath, printer, process
        public string rdlPath { get; set; }
        public string responsePath { get; set; }
        public string Printer { get; set; }
        public string DocNumber { get; set; }
        public ReportFormHelper RPFH { get; set; }
        public string fileName { get; set; }
    }


    public class ReportFormHelper
    {

        private static int m_currentPageIndex;

        private static IList<Stream> m_streams = null;

        private static string createdReportPath = null;


        public void Document_PrintNoThread(string reportPath, string responsePath, string curPrinter, string document_id)
        {

            try
            {
                if (!File.Exists(reportPath))
                    throw new Exception("Report file does not exists: " + reportPath);

                if (string.IsNullOrEmpty(curPrinter))
                    throw new Exception("No printer defined");

                //Rendering Report

                createdReportPath = responsePath;

                LocalReport lr = new LocalReport();

                PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
                lr.SetBasePermissionsForSandboxAppDomain(permissions);

                lr.ReportPath = reportPath;

                IMPORTSUITE_DAODataContext _xdb = new IMPORTSUITE_DAODataContext();

                DataSet cm = SQLBase.ReturnDataSet("EXEC spGetInvoiceData  @Invoice_ID='" + document_id + "'", new SqlConnection(_xdb.Connection.ConnectionString));

                for (int i = 0; i < cm.Tables.Count; i++)
                    lr.DataSources.Add(new ReportDataSource("DataSet" + (i + 1).ToString(), cm.Tables[i]));


                m_streams = new List<Stream>();

                m_currentPageIndex = 0;

                Export(lr, "IMAGE");

                m_currentPageIndex = 0;

                //Thread.Sleep(10000);
                //Print(curPrinter);

                Thread th = new Thread(new ParameterizedThreadStart(Print));
                th.Start(curPrinter);

            }
            catch
            {
                throw;
            }


        }



        #region Print Batch Methods

        private static Stream CreateStream(string name, string fileNameExtension, Encoding encoding, string mimeType, bool willSeek)
        {
            try
            {
                Stream stream = new FileStream(

                    Path.Combine(
                        createdReportPath, Guid.NewGuid() + name + "." + fileNameExtension),
                        FileMode.Create);

                m_streams.Add(stream);
                return stream;
            }
            catch { return null; }
        }


        private static void Export(LocalReport report, string renderFormat)
        {
            //renderFormat = "IMAGE", "PDF", 

            string deviceInfo = "<DeviceInfo>" +
            "  <OutputFormat>EMF</OutputFormat><ColorDepth>8</ColorDepth>" +
            "  <PageWidth>8.5in</PageWidth>" +
            "  <PageHeight>11in</PageHeight>" +
            "  <MarginTop>0.5cm</MarginTop>" +
            "  <MarginLeft>0.0in</MarginLeft>" +
            "  <MarginRight>0.0in</MarginRight>" +
            "  <MarginBottom>0.5cm</MarginBottom>" +

            // "  <ColorDepth>32</ColorDepth>" +
            // "  <DpiX>300</DpiX>" +
            //"  <DpiY>300</DpiY>" +

            "</DeviceInfo>";


            Warning[] warnings;

            try
            {
                report.Render(renderFormat, deviceInfo, CreateStream, out warnings);

                foreach (Stream stream in m_streams)
                    stream.Position = 0;

            }
            catch
            {
                throw;
            }


        }


        private static void PrintPage(object sender, PrintPageEventArgs ev)
        {
            try
            {
                Metafile pageImage = new Metafile(m_streams[m_currentPageIndex]);
                ev.Graphics.DrawImage(pageImage, ev.PageBounds);

                m_currentPageIndex++;
                ev.HasMorePages = (m_currentPageIndex < m_streams.Count);
            }
            catch { }
        }


        private static void Print(object printerName)
        {
            try
            {
                if (m_streams == null || m_streams.Count == 0)
                    return;

                PrintDocument printDoc = new PrintDocument();

                printDoc.PrinterSettings.PrinterName = printerName.ToString();


                if (!printDoc.PrinterSettings.IsValid)
                {
                    throw new Exception("Can't found printer " + printerName.ToString());
                }


                printDoc.PrintPage += new PrintPageEventHandler(PrintPage);

                printDoc.Print();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Print-ERROR: " + ex.Message);
            }
        }


        #endregion




        public void Document_SendByEmail_V2(string reportPath, vw_AX_CustomerInvoice invTransaction, string emailAddress, string mailType, string xGUID)
        {

            try
            {
                //programar el envio del mail
                IMPORTSUITE_DAODataContext _db = new IMPORTSUITE_DAODataContext();

                //Bypass if the invoice is already in the email list
                if (_db.T_DocEmail_Sents.Any(f => f.DocNumber == invTransaction.INVOICEID && f.SentDate != null && f.SentDate == DateTime.Today))
                    return;

                //JM : 2017/JUL/14 Bypass if the invoice is already in the email list
                if (_db.T_MailPools.Any(f => f.RefNo == invTransaction.INVOICEID && f.CreatedOn.Date == DateTime.Today))
                    return;

                string template = "";
                try { template = _db.S_PARAMETERs.Where(f => f.CODE == "MAILTPL").First().VALUE; }
                catch { }
                //string sendCopyTo = _db.M_Parameters.Where(f => f.Code == "BILLING.INVCOPYMAIL").First().Value;

                string message = "Your Invoice  # " + invTransaction.INVOICEID + " is attached.";
                string subject = "Invoice # " + invTransaction.INVOICEID;

                //message = MailSender.ReplaceTemplate(template, errorMsg);

                _db.T_DocEmail_Sents.InsertOnSubmit(new T_DocEmail_Sent
                {
                    DocNumber = invTransaction.INVOICEID,
                    DocType = "INVOICE",
                    AccountNumber = invTransaction.INVOICEACCOUNT,
                    Email = (invTransaction.EMAIL == null ? emailAddress : invTransaction.EMAIL),
                    SentDate = DateTime.Now,
                    xGUID = xGUID
                });

                //JM : 2017/JUL/14  Save at once
                _db.SubmitChanges();


                T_MailPool enviomail = new T_MailPool
                {

                    MTo = emailAddress,
                    Message = message,
                    CreatedBy = "admin",
                    Subject = subject,
                    UseBCC = 0,
                    CreatedOn = DateTime.Now,
                    Sent = 0,
                    Attachment = reportPath,
                    MailType = mailType,
                    RefNo = invTransaction.INVOICEID,
                    xGUID = xGUID
                };

                _db.T_MailPools.InsertOnSubmit(enviomail);

                _db.SubmitChanges();

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

        }


        // CAA
        public void Document_to_PDF(string reportPath, string responsePath, string invoiceId)
        {
            Document_to_PDF(reportPath, responsePath, invoiceId, "");
        }

        // Parametro "fileName", se concatena al nombre del file cuando se genera el PDF.
        public void Document_to_PDF(string reportPath, string responsePath, string invoiceId, string fileName)
        {

            try
            {
                if (!File.Exists(reportPath))
                    throw new Exception("Report file does not exists: " + reportPath);

                //Rendering Report

                createdReportPath = responsePath;

                LocalReport lr = new LocalReport();

                lr.ReportPath = reportPath;

                IMPORTSUITE_DAODataContext _xdb = new IMPORTSUITE_DAODataContext();

                DataSet cm = SQLBase.ReturnDataSet("EXEC spGetInvoiceData  @Invoice_ID='" + invoiceId + "'", new SqlConnection(_xdb.Connection.ConnectionString) );

                for (int i = 0; i < cm.Tables.Count; i++)
                    lr.DataSources.Add(new ReportDataSource("DataSet" + (i + 1).ToString(), cm.Tables[i]));


                string deviceInfo = "<DeviceInfo>" +
                "  <OutputFormat>EMF</OutputFormat><ColorDepth>8</ColorDepth>" +
                "  <PageWidth>8.5in</PageWidth>" +
                "  <PageHeight>11in</PageHeight>" +
                "  <MarginTop>0.5cm</MarginTop>" +
                "  <MarginLeft>0.0in</MarginLeft>" +
                "  <MarginRight>0.0in</MarginRight>" +
                "  <MarginBottom>0.5cm</MarginBottom>" +
                "</DeviceInfo>";

                string mimeType;
                string encoding;
                string fileNameExtension;
                Warning[] warnings;
                string[] streams;
                byte[] renderedBytes;

                //Render the report
                renderedBytes = lr.Render(
                    "PDF",
                    deviceInfo,
                    // null,
                    out mimeType,
                    out encoding,
                    out fileNameExtension,
                    out streams,
                    out warnings);

                //
                string path = Path.Combine(responsePath + @"\consolPDF", fileName + invoiceId + ".PDF");
                BinaryWriter bn = new BinaryWriter(File.Open(path, FileMode.Create));
                bn.Write(renderedBytes);
                bn.Flush();
                bn.Close();

            }

            catch
            {
                throw;
            }
        }



    }
}