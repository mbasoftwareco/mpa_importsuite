using PANGEA.IMPORTSUITE.DataModel.MPA_SERVICE;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PANGEA.IMPORTSUITE.ErpFactory.ConnectFactory;

namespace PANGEA.IMPORTSUITE.ErpFactory.MPA_WS
{
    partial class ErpService : IProcessService
    {

        MPAPortalSuiteClient svc { get; set; }
        CallContext myContext { get; set; }

        public ErpService()
        {
            svc = new MPAPortalSuiteClient();


            string[] serverObject = null;

            try
            {
                serverObject = FactoryConnection.cnnString.Split('|');

                if (serverObject.Length < 4)
                    throw new Exception("");
            }
            catch
            {
                throw new Exception("Please setup the MPA_WS connection parameter correctly.");
            }

            myContext = new CallContext { Company = FactoryConnection.company  };

            //svc.ClientCredentials = new System.Net.NetworkCredential( serverObject[0],  serverObject[1],  serverObject[2]);
        }

        public string  ImportData(string filePath, string fileName, string separator, string gUID, string module, string entity)
        {
 
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

            string[] sigleFile = fileName.Split("\\".ToCharArray());
            sigleFile = sigleFile[sigleFile.Length-1].Split("/".ToCharArray());

            dr["FileName"] = sigleFile[0];

            dr["Separator"] = separator;
            dr["GUID"] = gUID;
            dr["Template"] = module;
            dr["Company"] = entity;

            dt.Rows.Add(dr);
            ds.Tables.Add(dt);

            string response = svc.importPortal(myContext, ds);
            return formatResponse(response);

        }

        public string ValidateData(string gUID, string module, string entity)
        {

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

            string response = svc.validate(myContext, ds);
            return formatResponse(response);

        }

        public string  ProcessData(string gUID, string module, string entity, string journalName, string userLogin, string autoPost)
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

            string response = svc.process(myContext, ds);
            return formatResponse(response);
        }


        string  formatResponse (string response)
        {
            string status = "";

            DataSet dsResult = null;

            if (!string.IsNullOrEmpty(response.ToString()))
            {
                try
                {
                    dsResult = UtilTool.GetDataSetFromString(response.ToString());

                    status = dsResult.Tables[0].Rows[0]["Status"].ToString();

                }
                catch
                {
                  
                }

                if (status.Equals("Error"))
                    throw new Exception(dsResult.Tables[0].Rows[0]["Message"].ToString());

                return dsResult.Tables[0].Rows[0]["Message"].ToString();

            }

            if (response == null)
                response = "";

            throw new Exception("No se obtuvo una respueesta valida [" + response + "].");
        }

    }
}


 