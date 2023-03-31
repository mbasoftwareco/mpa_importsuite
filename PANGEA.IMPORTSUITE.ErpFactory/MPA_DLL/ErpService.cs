using Microsoft.Dynamics.BusinessConnectorNet;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PANGEA.IMPORTSUITE.ErpFactory.ConnectFactory;

namespace PANGEA.IMPORTSUITE.ErpFactory.MPA_DLL
{
    partial class ErpService : IProcessService
    {
        Axapta ax;
        object response;

        public ErpService()
        {
 
            string[] serverObject = null;

            try
            {
                serverObject = FactoryConnection.cnnString.Split('|');

                if (serverObject.Length < 4)
                    throw new Exception("");
            }
            catch
            {
                throw new Exception("Please setup the MPA_DLL connection parameter correctly.");
            }

            try
            {
                // Login to Microsoft Dynamics Ax.
                ax = new Axapta();

                string strUserName = serverObject[0];
                string passwd = serverObject[1];
                string domain = serverObject[2];

                System.Net.NetworkCredential nc = new System.Net.NetworkCredential(strUserName, passwd, domain);

                ax.LogonAs(strUserName, domain, nc, FactoryConnection.company, "en-us", serverObject[3], null);

            }
            catch (Exception e)
            {
                string exMsg = String.Format("An error occurred in object creation or AX logon: {0}", e.Message);

                Console.WriteLine(exMsg);

                throw new Exception(exMsg);
                //return;
            }


        }


        private string process(string module, DataSet request)
        {
            try
            {
                response = ax.CallStaticClassMethod("PGANetBusinessConnector", "run", module, request);
            }
            catch (Exception e)
            {
                string exMsg = String.Format("An error has been encountered during CallStaticClassMethod {0}: {1}", module, e.Message);

                Console.WriteLine(exMsg);

                throw new Exception(exMsg);
            }
            finally
            {
                // Log off from Microsoft Dynamics AX.
                ax.Logoff();
            }

            string result = "";

            if (!string.IsNullOrEmpty(response.ToString()))
            {
                try
                {
                    DataSet dsResult = UtilTool.GetDataSetFromString(response.ToString());
                    result = dsResult.Tables[0].Rows[0]["Message"].ToString();
                }
                catch
                {

                }
            }

            return result;
        }



        public string ImportData(string filePath, string fileName, string separator, string gUID, string module, string entity)
        {
            //GUID, UrlFileCSV, FileName, Separator, Template, Company, Journal, UserId, AutoPost 

            //ValidatePortalSuite
            //ProcessPortalSuite

            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            dt.Columns.Add("UrlFileCSV");
            dt.Columns.Add("FileName");
            dt.Columns.Add("Separator");
            dt.Columns.Add("GUID");
            dt.Columns.Add("Template");
            dt.Columns.Add("Company");

            DataRow dr = dt.NewRow();
            dr["UrlFileCSV"] = filePath;
            dr["FileName"] = fileName;
            dr["Separator"] = separator;
            dr["GUID"] = gUID;
            dr["Template"] = module;
            dr["Company"] = entity;

            dt.Rows.Add(dr);
            ds.Tables.Add(dt);


            return process("ImportPortalSuite", ds);
        }


        public string ValidateData(string gUID, string module, string entity) {

            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            dt.Columns.Add("GUID");
            dt.Columns.Add("Template");
            dt.Columns.Add("Company");

            DataRow dr = dt.NewRow();
            dr["GUID"] = gUID;
            dr["Template"] = module;
            dr["Company"] = entity;

            dt.Rows.Add(dr);
            ds.Tables.Add(dt);


            return process("ValidatePortalSuite", ds);
        }



        public string ProcessData(string gUID, string module, string entity, string journalName, string userLogin, string autoPost)
        {

            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            dt.Columns.Add("GUID");
            dt.Columns.Add("Template");
            dt.Columns.Add("Company");
            dt.Columns.Add("UserId");

            if (!string.IsNullOrEmpty(journalName))
                dt.Columns.Add("Journal");

            if (!string.IsNullOrEmpty(autoPost) && autoPost != "0")
                dt.Columns.Add("AutoPost");

            DataRow dr = dt.NewRow();
            dr["GUID"] = gUID;
            dr["Template"] = module;
            dr["Company"] = entity;
            dr["UserId"] = userLogin;

            if (!string.IsNullOrEmpty(journalName))
                dr["Journal"] = journalName;

            if (!string.IsNullOrEmpty(autoPost) && autoPost != "0")
                dr["AutoPost"] = autoPost;

            dt.Rows.Add(dr);
            ds.Tables.Add(dt);


            return process("ProcessPortalSuite", ds);
        }

    }
}

 