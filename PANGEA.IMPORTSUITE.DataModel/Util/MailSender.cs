using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net;
using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System.IO;
using System.Data;
using Microsoft.Reporting.WebForms;
using System.Data.SqlClient;

namespace PANGEA.IMPORTSUITE.DataModel.Util
{
    public static class MailSender
    {
        private static String Server { get; set; }
        private static String User { get; set; }
        private static String Password { get; set; }
        private static String Domain { get; set; }
        private static String HtmlEnvelope { get; set; }
        private static String Port { get; set; }
        private static bool EnableSSL { get; set; }
        private static String MailFrom { get; set; }
        private static bool SendMailEnabled { get; set; }
        private static String TestEmailAddress { get; set; }

        private static IMPORTSUITE_DAODataContext _db;

        //PRINT REPORTS

        private static int m_currentPageIndex;

        private static IList<Stream> m_streams = null;

        private static string createdReportPath = null;

        private static void SetMailOptions()
        {

            if (_db == null)
                _db = new IMPORTSUITE_DAODataContext();

            if (string.IsNullOrEmpty(Server) || string.IsNullOrEmpty(User))
            {

                List<S_PARAMETER> listaParametros = _db.S_PARAMETERs.ToList();

                Server = listaParametros.Where(f => f.CODE == "MAILSERVER").First().VALUE;

                User = listaParametros.Where(f => f.CODE == "MAILUSER").First().VALUE;

                Password = listaParametros.Where(f => f.CODE == "MAILPASSWD").First().VALUE;

                try
                {
                    Domain = listaParametros.Where(f => f.CODE == "MAILDOMAIN").First().VALUE;
                }
                catch { }

                try
                {
                    HtmlEnvelope = listaParametros.Where(f => f.CODE == "HTMLENVELOPE").First().VALUE;
                }
                catch { HtmlEnvelope = ""; }

                try
                {
                    EnableSSL = listaParametros.Where(f => f.CODE == "MAILSSL").First().VALUE.Equals("T");
                }
                catch { EnableSSL = false; }

                try
                {
                    Port = listaParametros.Where(f => f.CODE == "MAILPORT").First().VALUE;
                }
                catch { }


                try
                {
                    MailFrom = listaParametros.Where(f => f.CODE == "MAILFROM").First().VALUE;
                }
                catch { }

                try
                {
                    SendMailEnabled = true;
                    SendMailEnabled = bool.Parse(listaParametros.Where(f => f.CODE == "SEND.MAIL").First().VALUE);
                }
                catch { }

                try
                {
                  
                    TestEmailAddress  = listaParametros.Where(f => f.CODE == "MAIL_TESTADDRESS").First().VALUE;
                }
                catch { }

            }

        }



        public static void SendEmail(T_MailPool message)
        {

            IList<string> toAddress = new List<string>();

            try
            {

                if (string.IsNullOrEmpty(message.MTo))
                    return;

                SetMailOptions();

                if (!string.IsNullOrEmpty(HtmlEnvelope))
                    message.Message = HtmlEnvelope.Replace("__CONTENT", message.Message);

                MailMessage objMessage = new MailMessage();

                objMessage.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

                objMessage.From = new MailAddress(MailFrom); //new MailAddress(message.mfrom);

            
                //Testing Purposes
                if (!string.IsNullOrEmpty(TestEmailAddress))
                {
                    objMessage.To.Add(TestEmailAddress);
                }
                else  //Real production
                {

                    foreach (String address in message.MTo.Replace(" ", ";").Split(';'))
                    {
                        if (string.IsNullOrEmpty(address.Trim()))
                            continue;

                        if (toAddress.Contains(address.Trim()))
                            continue;

                        try
                        {
                            objMessage.To.Add(address.Trim());
                            toAddress.Add(address.Trim());
                        }
                        catch { }
                    }

                    //BCC
                    objMessage.Bcc.Add("jamurco@hotmail.com");

                    //Copy to Mail Sender  -- ONLY COPY TO VBL WHEN EMAIL HAS SUBJECT
                    if (!string.IsNullOrEmpty(message.Subject) && message.Subject.Contains("Invoice"))
                        objMessage.CC.Add(MailFrom);
                }

                //ATTACHMENTS
                try
                {
                    foreach (string adj in message.Attachment.Split(';'))
                    {
                        if (string.IsNullOrEmpty(adj))
                            continue;

                        objMessage.Attachments.Add(new Attachment(adj));
                    }
                }
                catch { }

                objMessage.Subject = message.Subject;
                objMessage.Body = message.Message;


                objMessage.IsBodyHtml = true;

                SmtpClient smtpClient = new SmtpClient(Server);

                if (string.IsNullOrEmpty(User))
                    smtpClient.UseDefaultCredentials = true;

                else
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(User, Password, Domain);
                }

                if (!string.IsNullOrEmpty(Port))
                    smtpClient.Port = int.Parse(Port);

                if (EnableSSL)
                    smtpClient.EnableSsl = true;

                smtpClient.Send(objMessage);
            }
            catch
            {
                throw;
            }

        }

        internal static void Mail_SendError_DataTable(DataTable dtNotas, string subject, string sendTo, string template)
        {

            if (_db == null)
                _db = new IMPORTSUITE_DAODataContext();


            string message = template;

            int pkseq = 0, seq = 0;   //Controlar la columna PK que no se muestre en el mail

            string body = "<table border=1 cellpadding=2 cellspacing=2><thead><tr bgcolor=\"#cecece\">";

            foreach (DataColumn colm in dtNotas.Columns)
            {
                seq++;
                body += "<th>" + colm.Caption + "</th>";
            }

            body += "</tr></thead><tbody>";


            foreach (DataRow row in dtNotas.Rows)
            {
                body += "<tr>";

                for (int i = 0; i < row.ItemArray.Count(); i++)
                {

                    try
                    {
                        body += "<td>" + row[i].ToString() + "&nbsp;</td>";
                    }
                    catch
                    {
                        body += "<td>&nbsp;</td>";
                    }
                }
                body += "</tr>";
            }

            body += "</tbody></table>";



            message += body;



            T_MailPool enviomail = new T_MailPool
            {

                MTo = sendTo,
                Message = message,
                CreatedBy = "admin",
                Subject = subject,
                UseBCC = 0,
                CreatedOn = DateTime.Now,
                Sent = 0,
                MailType = ""

            };

            _db.T_MailPools.InsertOnSubmit(enviomail);

            _db.SubmitChanges();

            SendPendingMessages();
        }


        public static void Mail_SendError(List<string> errorMsg)
        {
            return;

            if (errorMsg == null || errorMsg.Count == 0)
                return;

            if (_db == null)
                _db = new IMPORTSUITE_DAODataContext();


            string template = _db.S_PARAMETERs.Where(f => f.CODE == "MAILTPL").First().VALUE;
            string sendTo = _db.S_PARAMETERs.Where(f => f.CODE == "BILLING.MAIL").First().VALUE;

            string message = "", subject = "";

            message = ReplaceTemplate(template, errorMsg);

            T_MailPool enviomail = new T_MailPool
            {

                MTo = sendTo,
                Message = message,
                CreatedBy = "admin",
                Subject = subject,
                UseBCC = 0,
                CreatedOn = DateTime.Now,
                Sent = 0,
                MailType = ""

            };

            _db.T_MailPools.InsertOnSubmit(enviomail);
            _db.SubmitChanges();


            SendPendingMessages();

        }


        public static string ReplaceTemplate(string tpl, List<string> msgList)
        {
            string result = "";

            foreach (string m in msgList)
                result += m + "<br>";

            return result;
        }



        public static void SendPendingMessages()
        {
            if (_db == null)
                _db = new IMPORTSUITE_DAODataContext();


            Console.WriteLine("Sending Pending Mesasges");

            //START RUNNING
            INVOICES_prepareMessages_V3(_db);  //Pack Invoices of 40  - 6 JUN 2018

            _db.CommandTimeout = 900;


            IList<T_MailPool> msgList = _db.T_MailPools
                .Where(f => f.MailType != "INVOICE")
                .Where(f => f.Sent == 0).ToList();


            Console.WriteLine("Invoices to Consolidate: " + msgList.Count);



            if (msgList == null || msgList.Count == 0)
            {
                //END RUNNING
                return;

            }

            Console.WriteLine("Pass List.");



            foreach (T_MailPool msg in msgList)
            {
                try
                {
                    MailSender.SendEmail(msg);
                    msg.Sent = 1;
                    msg.SentDate = DateTime.Now;
                    Console.WriteLine("\t" + msg.Subject);

                }
                catch (Exception ex)
                {
                    msg.Sent = 2;
                    msg.Notes = ex.Message;
                    Console.WriteLine(ex.Message);
                }

                _db.SubmitChanges();
            }

        }


        //Send Emails until of 15 MB each mail
        private static void INVOICES_prepareMessages_V3(IMPORTSUITE_DAODataContext _zdb)
        {

            if (_db == null)
                _db = _zdb;

            _db.CommandTimeout = 900;

            //1. get all Invoiced grouped by Customer-Email
            IList<vw_TMailPool_Invoice> msgList = _db.vw_TMailPool_Invoices
                .Where(f => f.Sent == 0)
                .ToList();

            if (msgList == null || msgList.Count == 0)
                return;

            Console.Write("Invoices to Send: " + msgList.Count);

            //2. get the email Template
            string invoiceMailTemplate = _db.S_PARAMETERs.Where(f => f.CODE == "INV.MAIL.TPL").First().VALUE;

            string responsePath = "";
            try { responsePath = _db.S_PARAMETERs.Where(f => f.CODE == "INVOICE.DIR").First().VALUE; }
            catch { throw new Exception("Error: parameter INVOICE.DIR not defined!"); }


            string trInv = "<table cellpadding=3 cellspacing=1 bgcolor=#cecece>"
                + "<tr><th>Invoice Number</th><th>Facility</th><th>Invoice Date</th><th>Invoice Description</th><th>Invoice Amount</th></tr>\n";

            foreach (string customer in msgList.Select(f => f.INVOICEACCOUNT).Distinct())
            {
                string invDetails = trInv;

                //3. Prepare the mail by Customer
                //Lista de Facturas de ese cliente
                List<vw_TMailPool_Invoice> listOfCustomerInvoices = msgList.Where(f => f.INVOICEACCOUNT == customer)
                    .OrderBy(f => f.INVOICEID).ToList();


                //INVOICES EACH 40
                int tmpCount = 0; int regsToTake = 40; int pageNum = 1;

                try { regsToTake = int.Parse(_db.S_PARAMETERs.Where(f => f.CODE == "INVOICE.NUMREG").First().VALUE); }
                catch { regsToTake = 40; }

                int pageTotal = (int)(listOfCustomerInvoices.Count / regsToTake);

                if ((listOfCustomerInvoices.Count % regsToTake) > 0)
                    pageTotal++;

                while (tmpCount < listOfCustomerInvoices.Count)
                {

                    CreateMail_InvoicePackageForCustomer(listOfCustomerInvoices.Skip(tmpCount).Take(regsToTake).ToList(),
                        trInv, pageNum, pageTotal, responsePath, invoiceMailTemplate);

                    tmpCount += regsToTake;

                    pageNum++;
                }


                //put record as sent
                foreach (vw_TMailPool_Invoice curMsg in listOfCustomerInvoices)
                {
                    T_MailPool msg = _db.T_MailPools.Where(f => f.RowID == curMsg.RowID).FirstOrDefault();
                    msg.Sent = 1;
                    msg.Notes = "PREPARE";
                    msg.SentDate = DateTime.Now;
                    _db.SubmitChanges();
                }

            }



        }

        private static void CreateMail_InvoicePackageForCustomer(List<vw_TMailPool_Invoice> recordList, string htmlTableHeader,
                            int pageNum, int pageTotal, string responsePath, string mailTemplate)
        {
            string attachnments = "";
            string invDetails = htmlTableHeader;

            //4. Send the email
            decimal zTotal = 0;
            List<string> alreadyProcessed = new List<string>();

            foreach (vw_TMailPool_Invoice regInvCust in recordList)
            {

                //JM JUL 28/2017
                //Prevent to process the Invoice more than once
                if (alreadyProcessed.Contains(regInvCust.INVOICEID))
                    continue;

                invDetails += "<tr bgcolor=#fafafa><td>" + regInvCust.INVOICEID + "</td>"
                    + "<td>" + regInvCust.Site + "</td>"
                    + "<td>" + regInvCust.INVOICEDATE.ToString("MM/dd/yyyy") + "</td>"
                    + "<td>" + regInvCust.InvDetail + "</td>"
                    + "<td align=right>" + regInvCust.amount.Value.ToString("N2") + "</td></tr>\n";


                attachnments += generate_PDF_FiletoAttach(regInvCust.Attachment, regInvCust.INVOICEID, responsePath) + ";";

                zTotal += regInvCust.amount.Value;

                alreadyProcessed.Add(regInvCust.INVOICEID);

            }

            invDetails += "<tr bgcolor=#fafafa><td colspan=4 align=right><b>TOTAL&nbsp;</b></td>"
                    + "<td align=right><b>" + zTotal.ToString("N2") + "</b></td></tr>\n";

            invDetails += "</table>";

            invDetails = mailTemplate.Replace("__DETAILS", invDetails);
            invDetails = invDetails.Replace("__CUSTOMER", recordList.First().Customer);
            invDetails = invDetails.Replace("__DATE", DateTime.Today.ToString("MM/dd/yyyy"));


            string pages_str = (pageTotal > 1) ? (" - " + pageNum.ToString() + " of " + pageTotal.ToString()) : "";

            T_MailPool enviomail = new T_MailPool
            {

                MTo = recordList.First().MTo, //"jairo.murillo@grouppangea.com;carolina.anzola@grouppangea.com",
                Message = invDetails,
                CreatedBy = "admin",
                Subject = "Miami Parking Authority Invoice Notification - " + recordList.First().INVOICEACCOUNT + pages_str, // DO NOT REPLY - "Vertical Bridge Billing " + DateTime.Today.ToString("MM/dd/yyyy"),
                UseBCC = 0,
                CreatedOn = DateTime.Now,
                Sent = 0,
                Attachment = attachnments,
                MailType = ""
            };

            Console.WriteLine("Attachment: " + attachnments);

            _db.T_MailPools.InsertOnSubmit(enviomail);
            _db.SubmitChanges();
        }


        private static string generate_PDF_FiletoAttach(string reportPath, string invoiceID, string responsePath)
        {

            try
            {
                //programar el envio del mail
                _db = new IMPORTSUITE_DAODataContext();
                string cnnStr = _db.Connection.ConnectionString;

                /*
                //Bypass if the invoice is already in the email list
                if (_db.T_DocEmail_Sents.Any(f => f.DocNumber == invoiceID))
                    return "";

                //JM : 2017/JUL/14 Bypass if the invoice is already in the email list
                if (_db.T_MailPools.Any(f => f.RefNo == invoiceID && f.CreatedOn.Date == DateTime.Today && f.Sent == 1))
                    return "";
                */


                if (!File.Exists(reportPath))
                    throw new Exception("Report file does not exists: " + reportPath);

                //Rendering Report


                createdReportPath = responsePath;

                LocalReport lr = new LocalReport();

                lr.ReportPath = reportPath;
                 

                DataSet cm = SQLBase.ReturnDataSet("EXEC spGetInvoiceData  @Invoice_ID='" + invoiceID + "'",  new SqlConnection(cnnStr));

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

                string path = Path.Combine(responsePath, invoiceID + ".PDF");
                BinaryWriter bn = new BinaryWriter(File.Open(path, FileMode.Create));
                bn.Write(renderedBytes);
                bn.Flush();
                bn.Close();

                return path;

            }

            catch (Exception ex)
            {
                Console.Write("\t" + ex.Message);
                throw;
            }
        }


        /*

        public static void SendEmail(Z_MAIL_SEND message)
        {

            IList<string> toAddressTO = new List<string>();

            try
            {

                if (string.IsNullOrEmpty(message.MTO))
                    return;

                SetMailOptions();

                if (!SendMailEnabled)
                    return;

                if (!string.IsNullOrEmpty(HtmlEnvelope))
                    message.MESSAGE = HtmlEnvelope.Replace("__CONTENT", message.MESSAGE);

                MailMessage objMessage = new MailMessage();

                objMessage.From = new MailAddress(MailFrom); //new MailAddress(message.mfrom);


                //TO
                foreach (String address in message.MTO.Replace(" ", ";").Split(';'))
                {
                    if (string.IsNullOrEmpty(address.Trim()))
                        continue;

                    if (toAddressTO.Contains(address.Trim()))
                        continue;

                    try
                    {
                        objMessage.To.Add(address.Trim());
                        toAddressTO.Add(address.Trim());
                    }
                    catch { }
                }

                //CC
                //foreach (String address in message.mcc.Replace(" ", ";").Split(';'))
                //{
                //    if (string.IsNullOrEmpty(address.Trim()))
                //        continue;

                //    if (toAddressTO.Contains(address.Trim()))
                //        continue;

                //    try
                //    {
                //        objMessage.CC.Add(address.Trim());
                //        toAddressTO.Add(address.Trim());
                //    }
                //    catch { }
                //}


                //ATTACHMENTS
                try
                {
                    foreach (string adj in message.ATTACHMENT.Split(';'))
                    {
                        if (string.IsNullOrEmpty(adj))
                            continue;

                        objMessage.Attachments.Add(new Attachment(adj));
                    }
                }
                catch { }


                objMessage.Subject = message.SUBJECT;
                objMessage.Body = message.MESSAGE;


                objMessage.IsBodyHtml = true;

                SmtpClient smtpClient = new SmtpClient(Server);

                if (string.IsNullOrEmpty(User))
                    smtpClient.UseDefaultCredentials = true;

                else
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(User, Password, Domain);
                }

                if (!string.IsNullOrEmpty(Port))
                    smtpClient.Port = int.Parse(Port);

                if (EnableSSL)
                    smtpClient.EnableSsl = true;

                smtpClient.Send(objMessage);
            }
            catch
            {
                throw;
            }

        }


        public static void EnviarMail(string mto, string subject, string mensaje, string attachmentPath, S_USER cur_user)
        {
            if (_db == null)
                _db = new IMPORTSUITE_DAODataContext();


            //Validar si esta activa la opcion de enviar mail.
            try
            {
                if (_db.S_PARAMETERs.Where(f => f.CODE == "SEND.MAIL").First().VALUE.StartsWith("F"))
                    return; //No envia el mail.
            }
            catch { }

            string separador = ";";




            Z_MAIL_SEND enviomail = new Z_MAIL_SEND
            {
                MTO = mto,
                MESSAGE = mensaje,
                CREATEDBY = cur_user.USERNAME,
                SUBJECT = subject,
                USE_BCC = 0,
                CREATEDON = DateTime.Now,
                SENT = 0,
                //mcc = mcc
            };


            if (!string.IsNullOrEmpty(attachmentPath))
            {
                try
                {
                    //Crear el attachment con el file creado
                    //Carpeta Documentos - Adjuntos
                    StreamWriter file = new StreamWriter(attachmentPath);
                    file.WriteLine(mensaje);
                    file.Close();

                    enviomail.ATTACHMENT = attachmentPath;
                }
                catch { }
            }

            _db.Z_MAIL_SENDs.InsertOnSubmit(enviomail);

            _db.SubmitChanges();

            SendPendingMessages();
        }

        
        public static void SendPendingMessages()
        {
            if (_db == null)
                _db = new IMPORTSUITE_DAODataContext();

            IList<Z_MAIL_SEND> msgList = _db.Z_MAIL_SENDs.Where(f => f.SENT == 0).ToList();

            if (msgList == null || msgList.Count == 0)
                return;

            foreach (Z_MAIL_SEND msg in msgList)
            {
                try
                {
                    MailSender.SendEmail(msg);
                    msg.SENT = 1;
                    //msg.intentos++;
                    msg.SENTDATE = DateTime.Now;
                }
                catch (Exception ex)
                {
                  
                    //msg.intentos++;

                    //Permite hasta 3 intentos
                    //if (msg.intentos >= 3)
                    //    msg.sent = 2;                    

                    msg.NOTE = ex.Message;

                    UtilTool.SetLogEventos("MAIL", "ERROR", "Envio Mail: " + ex.Message, msg.CREATEDBY);

                }
            }

            _db.SubmitChanges();

        }

        */
    }
}
