using Microsoft.VisualBasic.FileIO;
using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class BaseController : Controller
    {

        public _WEB _web;
        public S_USER curUser;
        public bool? use365;
        public string formato_fecha = "dd/MM/yyyy";
        public CultureInfo CultureInfo = new CultureInfo("en-US");

        public CultureInfo CultureInfoEs = new CultureInfo("es-CO");
        public CultureInfo CultureInfoUs = new CultureInfo("en-US");

        //DBL SMLS - Setup
        public static ImportSuiteConnection curConnection { get; set; }
        public IMPORTSUITE_DAODataContext _db { get; set; }
        public SqlConnection _sqlCnn { get; set; }


        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {

            Response.BufferOutput = true;

            try
            {
                bool useSLMS = false;

                try { useSLMS = System.Configuration.ConfigurationManager.AppSettings["UseSMSL_Settings"].Equals("T"); }
                catch { }

                curConnection = ConnectionHelper.getContext(useSLMS);

                _db = curConnection._xdb;

                _sqlCnn = curConnection._xsqlCnn;

                _web = new _WEB(curConnection);

                curUser = (Session["curUser"] as S_USER);

                use365 = (Session["USE365"] as bool?);

            }
            catch (Exception ex)
            {
                curConnection.ConnectionError = ex.Message;

                RedirectToAction("Login", "Account");
            }

            base.OnActionExecuting(filterContext);
        }


        public string WriteExcelDocumentV3_OXML(DataSet ds, string nombreArchivo)
        {

            string name = nombreArchivo;
            string finalPath = Server.MapPath("/Download/");

            if (!Directory.Exists(finalPath))
                Directory.CreateDirectory(finalPath);

            ExportToExcelOXML.CreateExcelDocument(ds, finalPath + name);

            return "/Download/" + name;
        }


        public static string FILE_Save(string data_file, string additionalPath, string GUID, HttpPostedFileBase FILE, string uploadsFilesPath, bool nameOnlyWithGUID)
        {
            string[] nombre = data_file.Replace(" ", "").Split("\\".ToCharArray());

            string oriName = nombre[nombre.Length - 1];

            string nombreFile = (nameOnlyWithGUID) ? 
                //(GUID + "." + FILE.FileName.Split('.')[1]) 
                GUID + Path.GetExtension(FILE.FileName)
                : (GUID + "_" + nombre[nombre.Length - 1]);

            var uploadedFile = FILE; // Request.Files[0];

            if(string.IsNullOrEmpty(uploadsFilesPath))
                uploadsFilesPath = UtilTool.ObtenerParametro("FILEUPLOAD", curConnection); //"\\\\svraxlatamuat\\FILEUPLOAD\\"; //Server.MapPath("/FILE_LOADED/");

            uploadsFilesPath += additionalPath;

            if (!Directory.Exists(uploadsFilesPath))
                Directory.CreateDirectory(uploadsFilesPath);

            //YEAR

            var filePath = Path.Combine(uploadsFilesPath, nombreFile);

            //uploadedFile.SaveAs(filePath);

            UtilSaveFile(filePath, uploadedFile);

            return filePath;
        }



        public static void UtilSaveFile(string filePath, HttpPostedFileBase uploadedFile)
        {

            IntPtr token;

            string[] curUser = UtilTool.ObtenerParametro("IMPERSONATE", curConnection).Split('|');

            if (!NativeMethods.LogonUser(curUser[0], curUser[2], curUser[1],
                /* this.userName,
                this.domain,
                this.password,*/
                NativeMethods.LogonType.NewCredentials,
                NativeMethods.LogonProvider.Default,
                out token))
            {
                throw new Win32Exception();
            }

            try
            {
                IntPtr tokenDuplicate;

                if (!NativeMethods.DuplicateToken(
                    token,
                    NativeMethods.SecurityImpersonationLevel.Impersonation,
                    out tokenDuplicate))
                {
                    throw new Win32Exception();
                }

                try
                {
                    using (WindowsImpersonationContext impersonationContext =
                        new WindowsIdentity(tokenDuplicate).Impersonate())
                    {
                        // Do stuff with your share here.

                        uploadedFile.SaveAs(filePath);

                        impersonationContext.Undo();
                        return;
                    }
                }
                finally
                {
                    if (tokenDuplicate != IntPtr.Zero)
                    {
                        if (!NativeMethods.CloseHandle(tokenDuplicate))
                        {
                            // Uncomment if you need to know this case.
                            ////throw new Win32Exception();
                        }
                    }
                }
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    if (!NativeMethods.CloseHandle(token))
                    {
                        // Uncomment if you need to know this case.
                        ////throw new Win32Exception();
                    }
                }
            }
        }



        public string FILE_GetShortName(string fileName)
        {
            try
            {
                string[] shortName = fileName.Trim().Split("\\".ToCharArray());

                shortName = shortName[shortName.Length - 1].Trim().Split('/');

                return shortName[shortName.Length];
            }
            catch
            {
                return fileName;
            }


        }


        public CSVFileSetup FILE_GetColumns(string fileName, string separator)
        {
            string result = "";
            int i = 0;

            using (var textFieldParser = new TextFieldParser(fileName))
            {

                textFieldParser.TextFieldType = FieldType.Delimited;
                textFieldParser.Delimiters = new[] { separator };
                textFieldParser.HasFieldsEnclosedInQuotes = true;

                //Column List
                List<string> columnList = textFieldParser.ReadFields().ToList();


                foreach (var field in columnList)
                {
                    result += field + "|";
                    i++;
                }

            }

            return new CSVFileSetup { columnList = result, columnNum = i, separator = separator };

        }


 

    }
}