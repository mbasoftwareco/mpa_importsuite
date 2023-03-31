using DBL.SLMS.GlobalEnvironment;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PANGEA.IMPORTSUITE.DataModel
{
    public class ConnectionHelper
    {
        public static ImportSuiteConnection getContext(bool useSLMS_Settings)
        {
            ImportSuiteConnection curConnection = new ImportSuiteConnection { success = false };

            if (useSLMS_Settings)
            {

                curConnection.globalSetting = DBL.SLMS.GlobalEnvironment.GlobalSettings.GetHostSettings(true, System.Configuration.ConfigurationManager.AppSettings.Get("hostname"));

                curConnection.settingsList = DBL.SLMS.Environment.Configurations.GetApplicationSetup().AppSettings;

                curConnection.imporSuiteConnectionString = curConnection.settingsList["ImportSuite365CnnStr"];

                curConnection._xdb = new IMPORTSUITE_DAODataContext(curConnection.imporSuiteConnectionString);

                curConnection.useSML_Settings = true;

            }
            else
            {
                //Standar Connection
                curConnection._xdb = new IMPORTSUITE_DAODataContext();

                curConnection.imporSuiteConnectionString = curConnection._xdb.Connection.ConnectionString;

            }


            curConnection._xsqlCnn = new SqlConnection(curConnection.imporSuiteConnectionString);

            curConnection.success = true;

            return curConnection;
        }
    }


    public class ImportSuiteConnection
    {
        public HostSettings globalSetting { get; set; }

        public Dictionary<string, string> settingsList { get; set; }

        public string imporSuiteConnectionString { get; set; }

        public string ConnectionError { get; set; }

        public bool success { get; set; }

        public bool useSML_Settings { get; set; }

        public IMPORTSUITE_DAODataContext _xdb { get; set; }

        public SqlConnection _xsqlCnn { get; set; }
    }
}
