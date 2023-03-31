using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PANGEA.IMPORTSUITE.DataModel.Util
{
    public class UtilTool
    {

        public static string CryptPasswd(string data, string cryptKey)
        {
            if (string.IsNullOrEmpty(data))
                return "";


            Crypto cpt = new Crypto(Crypto.SymmProvEnum.DES);
            return cpt.Encrypting(data, cryptKey);

        }


        public static string DeCryptPasswd(string value, string key)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            Crypto cpt = new Crypto(Crypto.SymmProvEnum.DES);
            return cpt.Decrypting(value, key);

        }


        public static string ObtenerParametro(string CODE, ImportSuiteConnection _dbi)
        {
            if (!_dbi.useSML_Settings)
                return ObtenerParametro_Base(CODE, _dbi);

            try
            {
                return _dbi.settingsList["IS365_" + CODE];
            }
            catch (Exception ex)
            {
                return ObtenerParametro_Base(CODE, _dbi);
            }
        }


        public static string ObtenerParametro_Base(string CODE, ImportSuiteConnection _dbi)
        {
            try
            {
                return _dbi._xdb.S_PARAMETERs.Where(f => f.CODE == CODE).FirstOrDefault().VALUE;
            }
            catch (Exception ex)
            {

            }

            return "";

        }



        public static bool CheckPermit(string permit, string user, IMPORTSUITE_DAODataContext _dbx)
        {
            return _dbx.CFG_07_AX_USERPROFILEs.Any(f => f.PROFILEID == permit && f.USERID == user);
        }

        public static void ActualizarParametro(string CODE, string VALUE, ImportSuiteConnection _dbi)
        {
            try
            {
                VALUE = string.IsNullOrEmpty(VALUE) ? "" : VALUE;

                S_PARAMETER PARM = _dbi._xdb.S_PARAMETERs.Where(f => f.CODE == CODE).First();
                PARM.VALUE = VALUE;
                _dbi._xdb.SubmitChanges();
            }
            catch (Exception ex)
            {

            }

        }


        public static List<M_METAMASTER> GetMMASTER(string CLASS_CODE, ImportSuiteConnection _dbi)
        {
            return _dbi._xdb.M_METAMASTERs.Where(f => f.CLASS_CODE == CLASS_CODE).OrderBy(f => f.NAME).ToList();
        }


        public static string ReturnGUID()
        {
            return Guid.NewGuid().ToString().ToUpper().Remove(8);
        }


        public static string GetTechMessage(Exception ex)
        {
            if (ex == null)
                return "";

            string msg = ex.Message;
            Exception tmpEx = ex.InnerException;

            while (tmpEx != null)
            {
                msg += "\n" + tmpEx.Message;
                tmpEx = tmpEx.InnerException;
            }

            return msg;
        }


        public static void WriteEventLog(string techError, string section)
        {
            string sSource;
            string sLog;
            string sEvent;

            sSource = "Pangea Runtime Console";
            sLog = "Application";
            sEvent = "Pangea Runtime " + section + " failure: " + techError;

            if (!EventLog.SourceExists(sSource))
                EventLog.CreateEventSource(sSource, sLog);

            EventLog.WriteEntry(sSource, sEvent, EventLogEntryType.Error, 200);
        }


        private static string DepureXmlData(string data)
        {
            return data.Replace('"', ' ').Replace('&', ' ').Replace("'", " ");
        }


        public static void Validate_Date(string dateValue, string column, string line)
        {

            DateTime curDate;

            if (!DateTime.TryParseExact(dateValue, "m/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out curDate))
                throw new Exception("ERROR IN CSV FILE: Date value [" + dateValue + "] in line " + line + " column " + column + " is not valid. Please check all dates.");

        }


        public static string Validate_Date_Jqgrid(DateTime? dateValue)
        {

            try
            {
                return dateValue.ToString();
            }
            catch
            {
                return "";
            }
        }


        public static string Validate_Bool_Jqgrid(bool dateValue)
        {
            if (dateValue == true)
                return "1";
            else
                return "0";
        }


        public static double ParseDouble(string paramKey)
        {

            CultureInfo CultureInfo = new CultureInfo("es-CO");

            NumberFormatInfo numberInfo = CultureInfo.NumberFormat;

            try
            {
                return double.Parse(paramKey, numberInfo);
            }
            catch { }

            return 0;

        }




        public static void SetLogEventos(string ENTITY, string TYPE, string MESSAGE, string CREATE_BY, ImportSuiteConnection _dbi)
        {
            try
            {
                _dbi._xdb.Z_LOGs.InsertOnSubmit(new Z_LOG
                {
                    ENTITY = ENTITY,
                    TYPE = TYPE,
                    MESSAGE = MESSAGE,
                    CREATEDBY = CREATE_BY,
                    CREATEDON = DateTime.Now
                });

                _dbi._xdb.SubmitChanges();
            }
            catch { }
        }



        public string EvaluateNull(string data)
        {
            if (data == null)
                return "";

            return data.Trim();
        }

        public static string Base64Encode(string plainText)
        {
            //System.Text.Encoding.GetEncoding("iso-8859-8")

            var plainTextBytes = System.Text.Encoding.Default.GetBytes(plainText);  //ASCII

            return System.Convert.ToBase64String(plainTextBytes);
        }


        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);

            return System.Text.Encoding.Default.GetString(base64EncodedBytes);
        }


        public static DataSet GetDataSetFromString(string xmlData)
        {
            StringReader theReader = new StringReader(xmlData);

            DataSet theDataSet = new DataSet();

            theDataSet.ReadXml(theReader);

            return theDataSet;
        }


        public static string getRDLFromInvoice(string invoice_id)
        {
            return "MPA_INVOICE_A.rdl";
        }

        public static string splitString(string data, int pos, string separator)
        {
            if (data.Length <= pos)
                return data;

            string pos_one = data.Substring(0, pos);
            string pos_two = data.Substring(pos);


            return pos_one + separator + pos_two;
        }


    }


    public static class Helper
    {
        /// <summary>
        /// Converts a DataTable to a list with generic objects
        /// </summary>
        /// <typeparam name="T">Generic object</typeparam>
        /// <param name="table">DataTable</param>
        /// <returns>List with generic objects</returns>
        public static List<T> DataTableToList<T>(this DataTable table) where T : class, new()
        {
            try
            {
                List<T> list = new List<T>();

                foreach (var row in table.AsEnumerable())
                {
                    T obj = new T();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                            propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                return null;
            }
        }
    }


    public class WH_Obj
    {


        public S_USER USUARIO { get; set; }

        public string OPCION { get; set; }

        public string GUID { get; set; }

        public int ROWID { get; set; }

        public string EXTRA_DATA { get; set; }

        public string EXTRA_DATA2 { get; set; }

        public string EXTRA_DATA3 { get; set; }

        public string EXTRA_DATA4 { get; set; }

        public string EXTRA_DATA5 { get; set; }

        public string FIELD { get; set; }


        public string VALUE { get; set; }

    }

    public class File_Info
    {


        public int id { get; set; }

        public string sourceFile { get; set; }

        public string Name { get; set; }

        public string path { get; set; }

        public int columnsQty { get; set; }

        public int rowsFrom { get; set; }

        public int runConciliation { get; set; }

        public string ConciliateWith { get; set; }

        public string facilityCode { get; set; }

        public string filetype { get; set; }

        public string regDate { get; set; }

        public string inactiveDate { get; set; }

        public string merchantId { get; set; }

    }


    public static class Constantes
    {

        public const string CryptString = "IMPORTSUITE";


        //ENTIDADES
        public const string ENTIDAD_SITES = "SITES";

        public const string ENTIDAD_PRINTER = "PRINTER";


        //DIRECTORIOS
        public const string PRINT_GENERATE_DIR = "PRINT";


        //NOTIFICACIONES
        public const string EMAIL_NOTIFICACION_NOVEDAD = "EMAIL.NOTIFICACION.NOVEDAD";


        //PARAMETROS
        public const string PARM_FOLDER_FILE = "FOLDER_FILE";

        public const string PARM_FOLDER_DEST_FILE = "FOLDER_DEST_FILE";

        public const string PARM_MSG_COMPLETED = "MSG_COMPLETED";


        //STATUS
        public const int STATUS_READY_TO_PROCESS = 0;
        public const int STATUS_VALIDATED = 100;
        public const int STATUS_WITH_ERROR = 2;
        public const int STATUS_SENT_TO_ERP = 1;
        public const int STATUS_POSTED = 10;


    }


    public class LIST_SELECTION
    {

        public string VALUE { get; set; }

        public string TEXT { get; set; }

    }


    public class Handsontable
    {

        public string data { get; set; }

        public string type { get; set; }

        public string width { get; set; }

        public string dateFormat { get; set; }

        public string format { get; set; }

        public string validator { get; set; }

        public bool allowInvalid { get; set; }

    }

    public static class Roles
    {

        public const string ADMIN = "ADMIN";

    }

    public class CSVFileSetup
    {
        public string columnList { get; set; }
        public int columnNum { get; set; }
        public string separator { get; set; }
    }


    public class ErpConnection
    {
        public string company { get; set; }
        public string cnnType { get; set; }
        public string cnnString { get; set; }

    } 

    #region :ARBOL
    public class Arbol
    {
        public string id { get; set; }

        public string text { get; set; }

        public string parent { get; set; }

        public string icon { get; set; }
        public state state { get; set; }

    }


    public class state //state Arbol
    {
        public Boolean opened { get; set; }  // is the node open
        public Boolean disabled { get; set; }  // is the node disabled
        public Boolean selected { get; set; } // is the node selected
    }
    #endregion


    #region :INQUIRY
    public class INQUIRYTOOL
    {
        public String field { get; set; }
        public String title { get; set; }
        public String width { get; set; }

        public String format { get; set; }

        public bool filterable { get; set; }
        public bool groupable { get; set; }

        public String[] aggregates { get; set; } //     aggregates: [ "count", "min", "max" ],
        public String groupFooterTemplate { get; set; } //groupFooterTemplate: "age total: #: count #, min: #: min #, max: #: max #"

        /*
        attributes: {
            "class": "table-cell",
            style: "text-align: right; font-size: 14px"
        }*/


    }

    public class INQUIRYTOOL_ROWS
    {
        public String Columns { get; set; }
        public String Value { get; set; }
    }
    #endregion


    #region :CHART
    public class CHART_BAR
    {
        public String label { get; set; }
        public List<object> data; //{ get; set; }
        public BAR_CHART_LINE bars;// { get; set; }
    }

    public class BAR_CHART_LINE
    {
        public Boolean show;//{ get; set; }
        public double barWidth;//{ get; set; }
        public int order;// { get; set; }
    }

    public class BAR_CHART_DATA
    {
        public int num1;// { get; set; }
        public int[,] num2;// { get; set; }
    }

    public class fullCalendar
    {
        public string title { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string backgroundColor { get; set; }
        public string borderColor { get; set; }
        public bool allDay { get; set; }
        public string url { get; set; }
    }

    public class pieChart
    {
        public double value { get; set; }
        public string color { get; set; }
        public string highlight { get; set; }
        public string label { get; set; }
    }

    #region areaChart
    public class areaChart
    {
        public List<string> labels { get; set; }
        public List<areaChartDatasets> datasets { get; set; }
    }

    public class areaChartDatasets
    {

        public string label { get; set; }
        public string fillColor { get; set; }
        public string strokeColor { get; set; }
        public string pointColor { get; set; }
        public string pointStrokeColor { get; set; }
        public string pointHighlightFill { get; set; }
        public string pointHighlightStroke { get; set; }
        public object data { get; set; }
    }
    #endregion

    #endregion


    #region :KENDO UI
    public class GridFieldFilter
    {
        public string Field { get; set; }
        public string Type { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }

    public class VirtualFilter
    {
        public string tipo_campo { get; set; }
        public string id_java_script { get; set; }
        public string display { get; set; }
        public string nombre { get; set; }
        public int num_order { get; set; }
        public List<VirtualFilterCbo> ListCbo;//{ get; set; }
    }

    public class VirtualFilterCbo
    {
        public string rowid { get; set; }
        public string display { get; set; }
    }

    public class GridFieldShow
    {
        public String Field { get; set; }
        public String Visible { get; set; }
    }
    #endregion

}
