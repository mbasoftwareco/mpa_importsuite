using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.MPA_SERVICE;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            string entity = "MPA";
            string separator = ",";
            string GUID = Guid.NewGuid().ToString().Remove(8);
            string module = "BR";
            string journalName = "GenJour";

            WH_Obj whObj = new WH_Obj { USUARIO = new S_USER { }, EXTRA_DATA = "", GUID = "0" };


            _WEB _web = new _WEB(null);

            DataSet dtJN = _web.sp_MPA_RECONCIL("ERPJOURNAL", whObj); //Obtiene data del Journal segun el curid to send to ERP

            string csvFile = Create_CSV_File(GUID + ".csv", dtJN.Tables[0]);

            csvFile = csvFile.Replace("/","\\");

            /*
            //Call th ax Service to ImportData
            PANGEA_MPA_SERVICEClient svc = new PANGEA_MPA_SERVICEClient();

            CallContext myContext = new CallContext { Company = entity };

            //Import
            string result = svc.PANGEA_import(myContext, csvFile, GUID, separator, GUID, module, myContext.Company);

            //Validate
            result = svc.PANGEA_validate(myContext, GUID, module, myContext.Company);

            //Process
            string autoPost = "1";
            result = svc.PANGEA_process(myContext, GUID, module, entity, journalName, "JM", autoPost); //Session["LOGONUSER"].ToString()
            */

        }


        private static string Create_CSV_File(string filename, DataTable dt)
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
            string uploadsFilesPath = UtilTool.ObtenerParametro("FILEUPLOAD", null);

            uploadsFilesPath += DateTime.Today.Year.ToString() + "/RECONCILE";

            if (!Directory.Exists(uploadsFilesPath))
                Directory.CreateDirectory(uploadsFilesPath);

            uploadsFilesPath = Path.Combine(uploadsFilesPath, filename);

            System.IO.File.WriteAllText(uploadsFilesPath, sb.ToString());

            return uploadsFilesPath;

        }


    }
}
