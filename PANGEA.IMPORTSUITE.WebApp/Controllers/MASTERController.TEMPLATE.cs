using Excel;
using PANGEA.IMPORTSUITE.DataModel;
using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PANGEA.IMPORTSUITE.WebApp.Controllers
{
    public partial class MASTERController : BaseController
    {

        #region :: TEMPLATE_MASTER

        // GET: PROFILE
        public ActionResult TEMPLATE_MASTER()
        {
            return View(_web._dbx.T_TEMPLATEs.OrderByDescending(f => f.CUSTOM_VALIDATIONS).ToList());
        }

        [NoCache]
        public JsonResult SYS_TEMPLATE_JOURNALNAME(int ROWID, string JOURNALNAME)
        {

            try
            {

                T_TEMPLATE T_TEMPLATE = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == ROWID).First();
                T_TEMPLATE.JOURNALNAME = JOURNALNAME;
                T_TEMPLATE.MODIFIEDBY = curUser.USERNAME;
                T_TEMPLATE.MODIFIEDON = DateTime.Now;

                _web._dbx.SubmitChanges();


                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }
        #endregion

        #region :: TEMPLATE_01_BASE

        // GET: PROFILE
        public ActionResult TEMPLATE_01_BASE()
        {
            return View(_web._dbx.T_TEMPLATEs.OrderBy(f => f.NAME).ToList());
        }


        [NoCache]
        public JsonResult SYS_TEMPLATE_ACTIVE(int ROWID)
        {

            try
            {
                T_TEMPLATE T_TEMPLATE = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == ROWID).First();


                if (T_TEMPLATE.AUTOPOST == true)
                    T_TEMPLATE.AUTOPOST = false;
                else
                    T_TEMPLATE.AUTOPOST = true;

                T_TEMPLATE.MODIFIEDBY = curUser.USERNAME;
                T_TEMPLATE.MODIFIEDON = DateTime.Now;


                _web._dbx.SubmitChanges();


                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }

        #endregion



        #region :: TEMPLATE_02_CONFIG


        public ActionResult TEMPLATE_02_CONFIG(int? ROWID_TEMPLATE)
        {
            T_TEMPLATE TEMPLATE = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == ROWID_TEMPLATE).FirstOrDefault();

            if (TEMPLATE == null)
                TEMPLATE = new T_TEMPLATE();

            return View(TEMPLATE);
        }


        [HttpPost]
        public ActionResult TEMPLATE_02_CONFIG(int ROWID)
        {
            try
            {

                T_TEMPLATE TEMPLATE = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == ROWID).FirstOrDefault();
                bool nuevo = false;

                if (TEMPLATE == null)
                {
                    TEMPLATE = new T_TEMPLATE();
                    nuevo = true;
                    TEMPLATE.CREATEDBY = curUser.USERNAME;
                    TEMPLATE.CREATEDON = DateTime.Now;
                }
                else
                {
                    TEMPLATE.MODIFIEDBY = curUser.USERNAME;
                    TEMPLATE.MODIFIEDON = DateTime.Now;
                }

                TEMPLATE.NAME = Request["NAME"];
                TEMPLATE.FOLDER = Request.Params["FOLDER"];
                TEMPLATE.ACTIVE = (Request["ACTIVE"] == "False") ? false : true;
                TEMPLATE.PANGEA_TEMPLATE = (Request["PANGEA_TEMPLATE"] == "False") ? false : true;
                TEMPLATE.HEADER_FIRSTROW = (Request["HEADER_FIRSTROW"] == "False") ? false : true;

                try
                {
                    TEMPLATE.NUMBER_FIELDS = int.Parse(Request["NUMBER_FIELDS"]);
                }
                catch { TEMPLATE.NUMBER_FIELDS = 0; }

                if (nuevo)
                    _web._dbx.T_TEMPLATEs.InsertOnSubmit(TEMPLATE);

                _web._dbx.SubmitChanges();

                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }


        #endregion



        #region :: TEMPLATE_02_CONFIG_FIELD

        public ActionResult TEMPLATE_02_CONFIG_FIELD(int ROWID_TEMPLATE)
        {

            T_TEMPLATE TEMPLATE = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == ROWID_TEMPLATE).First();

            ViewBag.TEMPLATE = TEMPLATE;

            ViewBag.VALIDATIONS = UtilTool.GetMMASTER("VALIDATION.LINE", curConnection);

            ViewBag.EQUIVALENCE = _web._dbx.T_EQUIVALENCEs.ToList();

            return PartialView(_web._dbx.T_TEMPLATE_FIELDs.Where(f => f.ROWID_TEMPLATE == ROWID_TEMPLATE).OrderBy(f => f.SEQUENCE).ToList());
        }


        [HttpPost]
        public JsonResult TEMPLATE_02_CONFIG_FIELD_VALUE(int ROWID)
        {
            try
            {
                T_TEMPLATE_FIELD TEMPLATE_FIELD = _web._dbx.T_TEMPLATE_FIELDs.Where(f => f.ROWID == ROWID).First();

                TEMPLATE_FIELD.FIELD_DEST_NAME = Request.Params["FIELD_DEST_NAME"];

                TEMPLATE_FIELD.VALIDATIONS = Request.Params["VALIDATIONS"];

                try
                {
                    TEMPLATE_FIELD.ROWID_EQ = int.Parse(Request["ROWID_EQ"]);
                }
                catch
                {
                    TEMPLATE_FIELD.ROWID_EQ = null;
                }

                //TEMPLATE_FIELD.REQUIRED = (Request.Params["REQUIRED"] == "False") ? false : true;

                TEMPLATE_FIELD.ACTIVE = (Request.Params["ACTIVE"] == "False") ? false : true;

                TEMPLATE_FIELD.MODIFIEDBY = curUser.USERNAME;
                TEMPLATE_FIELD.MODIFIEDON = DateTime.Now;

                _web._dbx.SubmitChanges();

                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }


        [HttpPost]
        public JsonResult TEMPLATE_02_CONFIG_DELETE_FIELD(int ROWID_TEMPLATE, string ROWID_TEMPLATE_FIELD)
        {
            try
            {

                if (ROWID_TEMPLATE_FIELD.Equals("ALL"))
                {
                    var TEMPLATE_FIELD = _web._dbx.T_TEMPLATE_FIELDs.Where(f => f.ROWID_TEMPLATE == ROWID_TEMPLATE).ToList();
                    _web._dbx.T_TEMPLATE_FIELDs.DeleteAllOnSubmit(TEMPLATE_FIELD);
                    _web._dbx.SubmitChanges();
                }
                else
                {
                    var TEMPLATE_FIELD = _web._dbx.T_TEMPLATE_FIELDs.Where(f => f.ROWID.ToString() == ROWID_TEMPLATE_FIELD).First();
                    TEMPLATE_FIELD.ACTIVE = (TEMPLATE_FIELD.ACTIVE == true) ? true : false;
                    //_web._dbx.T_TEMPLATE_FIELDs.DeleteOnSubmit(TEMPLATE_FIELD);
                    _web._dbx.SubmitChanges();
                }

                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }
        }


        public ActionResult LOAD_TEMPLATE_FIELD(HttpPostedFileBase FILE, int ROWID_TEMPLATE)
        {
            try
            {

                T_TEMPLATE TEMPLATE = _web._dbx.T_TEMPLATEs.Where(f => f.ROWID == ROWID_TEMPLATE).First();

                if (FILE == null)
                    throw new Exception("File not found.");

                LOAD_TEMPLATE_FIELD_FILE(TEMPLATE, FILE);

                return Json("OK");
            }
            catch (Exception e)
            {
                Response.StatusCode = 500;
                Response.StatusDescription = e.Message;
                return Json(e.Message);
            }

        }


        public void LOAD_TEMPLATE_FIELD_FILE(T_TEMPLATE TEMPLATE, HttpPostedFileBase data_file)
        {

            string ext = Path.GetExtension(data_file.FileName).ToLower();

            if (ext == null || (ext != ".xlsx")) // && ext != ".xls"
                throw new Exception("Extensión no válida para el archivo. Debe ser (xlsx)."); // XLS o 

            string filePath = Save_File(data_file.FileName);

            System.Data.DataRow objDBRow;

            DataSet dsx = new DataSet("DSX");


            #region TABLA TEMPORAL XLSX

            if (ext == ".xlsx")
            {

                SqlDataAdapter objAdapter = new SqlDataAdapter("EXEC TEMPLATE_CONFIG @OPTION ='TEMPLATE_CONFIG' ", _web._sqlCnn);

                objAdapter.Fill(dsx, "DSXT");

                int countWs = 0, countCell = 0, countRow = 0;

                foreach (var worksheet in Workbook.Worksheets(filePath))
                {
                    countWs++;

                    if (countWs > 1)
                        continue;

                    if (worksheet == null)
                        continue;


                    foreach (var row in worksheet.Rows)
                    {
                        countRow++;

                        if (row == null)
                            continue;

                        if (countRow == 1)
                            continue;


                        objDBRow = dsx.Tables[0].NewRow();

                        countCell = 0;

                        foreach (var cell in row.Cells)
                        {
                            countCell++;

                            if (cell == null)
                                continue;

                            if (string.IsNullOrEmpty(cell.Text))
                                continue;

                            string TextCell = cell.Text.Replace(",", "").Replace(".", "");


                            try
                            {
                                objDBRow[countCell] = TextCell;
                            }
                            catch
                            {
                                objDBRow[countCell] = int.Parse(TextCell.ToString());
                            }

                        }

                        dsx.Tables[0].Rows.Add(objDBRow);
                    }
                }

            }

            #endregion

            int SEQUENCE = 1;
            foreach (System.Data.DataRow row in dsx.Tables[0].Rows)
            {

                T_TEMPLATE_FIELD T_TEMPLATE_FIELD = new T_TEMPLATE_FIELD();

                T_TEMPLATE_FIELD.ROWID_TEMPLATE = TEMPLATE.ROWID;

                if (TEMPLATE.HEADER_FIRSTROW)
                    T_TEMPLATE_FIELD.FIELD_SOURCE_NAME = row[0].ToString();
                else
                    T_TEMPLATE_FIELD.FIELD_SOURCE_NAME = "C" + SEQUENCE;


                T_TEMPLATE_FIELD.SEQUENCE = SEQUENCE;

                _web._dbx.T_TEMPLATE_FIELDs.InsertOnSubmit(T_TEMPLATE_FIELD);
                _web._dbx.SubmitChanges();

                SEQUENCE++;

                if (!TEMPLATE.HEADER_FIRSTROW && SEQUENCE >= TEMPLATE.NUMBER_FIELDS)
                    break;

            }
        }


        private string Save_File(string data_file)
        {
            string[] nombre = data_file.Replace(" ", "").Split("\\".ToCharArray());

            string oriName = nombre[nombre.Length - 1];

            string nombreFile = Guid.NewGuid().ToString().Substring(0, 8) + "_" + nombre[nombre.Length - 1];

            var uploadedFile = Request.Files[0];


            string uploadsFilesPath = Server.MapPath("/FILE_LOADED/");

            if (!Directory.Exists(uploadsFilesPath))
                Directory.CreateDirectory(uploadsFilesPath);

            var filePath = Path.Combine(uploadsFilesPath, nombreFile);

            uploadedFile.SaveAs(filePath);

            return filePath;
        }


        #endregion


    }
}