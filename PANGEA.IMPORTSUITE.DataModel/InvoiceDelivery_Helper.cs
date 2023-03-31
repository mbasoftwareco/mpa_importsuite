using Microsoft.Reporting.WebForms;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PANGEA.IMPORTSUITE.DataModel
{
    public class InvoiceDelivery_Helper
    {
        IMPORTSUITE_DAODataContext _db { get; set; }


        public InvoiceDelivery_Helper()
        {
            _db = new IMPORTSUITE_DAODataContext();
        }



        public void Print_Pending_Invoices()
        {

            //string rdlPath = @"D:\AX_INTEGRATION_SITE\Forms\VB_INVOICE.rdl";

            //string responsePath = @"D:\AX_INTEGRATION_SITE\PrintReport";

            // CAA
            string printer = ""; 

            string rdlFormDir = "";
            try { rdlFormDir = _db.S_PARAMETERs.Where(f => f.CODE == "INVOICE.FORM.DIR").First().VALUE; }
            catch { throw new Exception("Error: parameter INVOICE.FORM.DIR not defined!"); }


            string responsePath = "";
            try { responsePath = _db.S_PARAMETERs.Where(f => f.CODE == "INVOICE.DIR").First().VALUE; }
            catch { throw new Exception("Error: parameter INVOICE.DIR not defined!"); }

            // CAA. For the PDF individual printings
            string temporaryPathPDF = "";
            try { temporaryPathPDF = _db.S_PARAMETERs.Where(f => f.CODE == "INVOICE_PDF.DIR").First().VALUE; }
            catch { throw new Exception("Error: parameter INVOICE_PDF.DIR not defined!"); }

            string finalPathPDF = "";
            try { finalPathPDF = _db.S_PARAMETERs.Where(f => f.CODE == "INVOICE_PDF_FINAL.DIR").First().VALUE; }
            catch { throw new Exception("Error: parameter INVOICE_PDF_FINAL.DIR not defined!"); }


            int numberToPrint = int.Parse(_db.S_PARAMETERs.Where(f => f.CODE == "TAKETOPRINT").First().VALUE);


            bool printThread = false;
            try { printThread = _db.S_PARAMETERs.Where(f => f.CODE == "PRINT.THREAD").First().VALUE.Equals("T"); }
            catch { }


            //Pone todas las invoices pendientes en Locked 
            // [2016/05/31]
            // Ordena por orden de registro. (se registran segun orden especificado durante la impresion)
            List<T_PrinterPool> invoiceList = _db.T_PrinterPools
                .Where(f => f.DocType == "INV" && f.ProcessDate == null && f.LockSession == null).OrderBy(o => o.RowID)
                .Take(numberToPrint).ToList();

            string lockSession = Guid.NewGuid().ToString();

            foreach (T_PrinterPool curInv in invoiceList)
                curInv.LockSession = DateTime.Now;

            _db.SubmitChanges(); //Save the lock session to prevent print more than once

            ReportFormHelper ph = null;
            BatchPrintProcess threadP = null;

            Console.WriteLine("Starts at " + DateTime.Now.ToString() + ", with " + invoiceList.Count.ToString());
            int caaCont = 0, fileCont = 0;

            foreach (T_PrinterPool invoice in invoiceList)
            {
                // CAA [2016/05/31]
                // Consecutivo para ser enviado como parte del fileName, con el objetivo que los PDFs queden 
                // con el nombre sequencial y ser unidos en orden por el proceso de Merge
                fileCont++;

                if (string.IsNullOrEmpty(invoice.DocNumber))
                    continue;

                string rdlPath = rdlFormDir + "\\" + UtilTool.getRDLFromInvoice(invoice.DocNumber);

                try
                {


                    //THREAD
                    if (printThread)
                    {

                        //Create el therat desde este punto.
                        threadP = new BatchPrintProcess
                        {
                            rdlPath = rdlPath,
                            responsePath = responsePath,
                            Printer = invoice.Printer,
                            DocNumber = invoice.DocNumber,
                            RPFH = new ReportFormHelper(),
                            fileName = fileCont.ToString().PadLeft(3, '0')
                        };

                        // CAA
                        Thread th = new Thread(new ParameterizedThreadStart(CreatePDFInBatchThread)); // PrintDocumentsInBatchThread
                        th.Start(threadP);
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        //NO THREAD
                        ph = new ReportFormHelper();
                        ph.Document_to_PDF(rdlPath, responsePath, invoice.DocNumber, fileCont.ToString().PadLeft(3, '0'));
                        // ph.Document_PrintNoThread(rdlPath, responsePath, invoice.Printer, invoice.DocNumber);
                    }


                    invoice.ProcessDate = DateTime.Now;

                    Console.WriteLine(invoice.DocNumber + " => OK");
                    caaCont++;

                }
                catch (Exception ex)
                {
                    invoice.ErrorMsg = ex.Message;

                    invoice.LockSession = null;

                    Console.WriteLine(invoice.DocNumber + " => " + ex.Message);
                }

                _db.SubmitChanges();

                printer = invoice.Printer;
            }
     
            // CAA
            // Une todos los PDFs generados en 1 solo y luego imprime ese consolidado

            MergePDF(responsePath, printer, temporaryPathPDF, finalPathPDF);
        }


        // CAA
        private static void CreatePDFInBatchThread(Object threatP)
        {
            BatchPrintProcess batch = (BatchPrintProcess)threatP;
            batch.RPFH.Document_to_PDF(batch.rdlPath, batch.responsePath, batch.DocNumber, batch.fileName);
        }


        private void MergePDF(string path, string printer, string tmpPdfPath, string finalPath)
        {
            try
            {
                Thread.Sleep(10000);

                // valida si generó algún PDF para imprimir
                string[] files = System.IO.Directory.GetFiles(path + @"\consolPDF", "*.pdf");

                if (files == null || files.Length == 0)
                    return;

                Console.WriteLine("Merge SP " + DateTime.Now.ToString());
                Console.WriteLine("Merge Starting with " + files.Length.ToString());

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                startInfo.FileName = "cmd.exe";
                startInfo.WorkingDirectory = path + @"\consolPDF";

                // path + @"\consolPDF\print\fullPDF_" 
                //string newDPF = tmpPdfPath + @"\fullPDF_" + DateTime.Now.ToString().Replace("/", "").Replace(" ", "_").Replace(":", "_") + ".pdf";
                string singleFile = "fullPDF_" + DateTime.Now.ToString().Replace("/", "").Replace(" ", "_").Replace(":", "_") + ".pdf";

                string newDPF = tmpPdfPath + "\\" + singleFile;

                startInfo.Arguments = "/C " + path + @"\consolPDF\pdftk.exe *.pdf cat output " + newDPF;

                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                Console.WriteLine("Merge ends at: " + DateTime.Now.ToString());

                // delete tmp PDF
                //
                // string[] files = System.IO.Directory.GetFiles(path + @"\consolPDF", "*.pdf");
                //DateTime creationDate;

                //JM MOVE PDF to Final Folder
                File.Move(newDPF, finalPath + "\\" + singleFile);

                Console.WriteLine("\t" + singleFile + " moved.");

                foreach (string f in files)
                {
                    //creationDate = System.IO.File.GetCreationTime(f);
                    //if (creationDate < System.DateTime.Today)
                    System.IO.File.Delete(f);
                }


                // [2015/09/22] Print using a Third app
                // Print(newDPF, printer); 
            }
            catch (Exception ex)
            {
                string mess = UtilTool.GetTechMessage(ex);
                UtilTool.WriteEventLog(mess, "Merging");
            }
        }


        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }


        public void UpdateLastRun(string paramCode)
        {
            try
            {

                S_PARAMETER lastRun = _db.S_PARAMETERs.Where(f => f.CODE == paramCode).First();

                lastRun.VALUE = DateTime.Now.ToString();

                _db.SubmitChanges();
            }
            catch { }

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



        public void Document_SendByEmail(string reportPath, string responsePath, vw_AX_CustomerInvoice invTransaction,
            string emailAddress, string mailType)
        {

            try
            {
                //programar el envio del mail
                IMPORTSUITE_DAODataContext _db = new IMPORTSUITE_DAODataContext();

                string cnnStr = _db.Connection.ConnectionString;

                //Bypass if the invoice is already in the email list
                if (_db.T_DocEmail_Sents.Any(f => f.DocNumber == invTransaction.INVOICEID))
                    return;

                //JM : 2017/JUL/14 Bypass if the invoice is already in the email list
                if (_db.T_MailPools.Any(f => f.RefNo == invTransaction.INVOICEID && f.CreatedOn.Date == DateTime.Today))
                    return;


                if (!File.Exists(reportPath))
                    throw new Exception("Report file does not exists: " + reportPath);

                //Rendering Report

                createdReportPath = responsePath;

                LocalReport lr = new LocalReport();

                lr.ReportPath = reportPath;
                

                DataSet cm = SQLBase.ReturnDataSet("EXEC spGetInvoiceData  @Invoice_ID='" + invTransaction.INVOICEID + "'", new SqlConnection(cnnStr));

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

                string path = Path.Combine(responsePath, invTransaction.INVOICEID + ".PDF");
                BinaryWriter bn = new BinaryWriter(File.Open(path, FileMode.Create));
                bn.Write(renderedBytes);
                bn.Flush();
                bn.Close();



                string template = _db.S_PARAMETERs.Where(f => f.CODE == "MAILTPL").First().VALUE;
                //string sendCopyTo = _db.S_PARAMETERs.Where(f => f.CODE == "BILLING.INVCOPYMAIL").First().VALUE;

                string message = "Your Invoice  # " + invTransaction.INVOICEID + " is attached.";
                string subject = "Miami Parking Authority Invoice # " + invTransaction.INVOICEID;

                //message = MailSender.ReplaceTemplate(template, errorMsg);

                _db.T_DocEmail_Sents.InsertOnSubmit(new T_DocEmail_Sent
                {
                    DocNumber = invTransaction.INVOICEID,
                    DocType = "INVOICE",
                    AccountNumber = invTransaction.INVOICEACCOUNT,
                    Email = (invTransaction.EMAIL == null ? emailAddress : invTransaction.EMAIL),
                    SentDate = DateTime.Now
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
                    Attachment = path,
                    MailType = mailType,
                    RefNo = invTransaction.INVOICEID
                };

                _db.T_MailPools.InsertOnSubmit(enviomail);

                _db.SubmitChanges();

            }

            catch (Exception ex)
            {
                throw;
            }

        }



        public void Document_SendByEmail_V2(string reportPath, vw_AX_CustomerInvoice invTransaction, string emailAddress, string mailType)
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

                string template = _db.S_PARAMETERs.Where(f => f.CODE == "MAILTPL").First().VALUE;
                //string sendCopyTo = _db.S_PARAMETERs.Where(f => f.CODE == "BILLING.INVCOPYMAIL").First().VALUE;

                string message = "Your Invoice  # " + invTransaction.INVOICEID + " is attached.";
                string subject = "Vertical Bridge Invoice # " + invTransaction.INVOICEID;

                //message = MailSender.ReplaceTemplate(template, errorMsg);

                _db.T_DocEmail_Sents.InsertOnSubmit(new T_DocEmail_Sent
                {
                    DocNumber = invTransaction.INVOICEID,
                    DocType = "INVOICE",
                    AccountNumber = invTransaction.INVOICEACCOUNT,
                    Email = (invTransaction.EMAIL == null ? emailAddress : invTransaction.EMAIL),
                    SentDate = DateTime.Now
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
                    RefNo = invTransaction.INVOICEID
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

                DataSet cm = SQLBase.ReturnDataSet("EXEC spGetInvoiceData  @Invoice_ID='" + invoiceId + "'", new SqlConnection((new IMPORTSUITE_DAODataContext()).Connection.ConnectionString));

                for (int i = 0; i < cm.Tables.Count; i++)
                    lr.DataSources.Add(new ReportDataSource("DataSet" + (i + 1).ToString(), cm.Tables[i]));


                string deviceInfo = "<DeviceInfo>" +
                "  <OutputFormat>EMF</OutputFormat><ColorDepth>8</ColorDepth>" +
                "  <PageWidth>8.5in</PageWidth>" +
                "  <PageHeight>11in</PageHeight>" +
                "  <MarginTop>0.5cm</MarginTop>" +
                "  <MarginLeft>0in</MarginLeft>" +
                "  <MarginRight>0in</MarginRight>" +
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

            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
