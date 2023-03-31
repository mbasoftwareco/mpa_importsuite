using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PANGEA.IMPORTSUITE.DataModel
{
    public class _WEB
    {


        public IMPORTSUITE_DAODataContext _dbx { get; set; }

        ImportSuiteConnection curConnection { get; set; }

        public SqlConnection _sqlCnn { get; set; }


        public _WEB(ImportSuiteConnection _curConnection)
        {
            this.curConnection = _curConnection;

            _dbx = new IMPORTSUITE_DAODataContext(curConnection.imporSuiteConnectionString);

            _sqlCnn = new SqlConnection(curConnection.imporSuiteConnectionString);
        }


        public DataSet sp_IMPORT_DATA(string OPCION, WH_Obj WHTrx_Obj)
        {
            return SQLBase.ReturnDataSet("EXEC sp_IMPORT_DATA @OPTION ='" + OPCION + "', @USERNAME ='" + WHTrx_Obj.USUARIO.USERNAME + "', @GUID ='" + WHTrx_Obj.GUID + "', @ROWID ='" + WHTrx_Obj.ROWID + "', @EXTRA_DATA ='" + WHTrx_Obj.FIELD + "', @EXTRA_DATA_2 ='" + WHTrx_Obj.VALUE + "' ",
               _sqlCnn);
        }



        public DataSet sp_IMPORT_DIRECTAX(string OPCION, WH_Obj WHTrx_Obj)
        {
            return SQLBase.ReturnDataSet("EXEC sp_IMPORT_DIRECTAX @OPTION ='" + OPCION + "', @USERNAME ='" + WHTrx_Obj.USUARIO.USERNAME + "', @GUID ='" + WHTrx_Obj.GUID + "', @ROWID ='" + WHTrx_Obj.ROWID + "', @EXTRA_DATA ='" + WHTrx_Obj.EXTRA_DATA + "', @EXTRA_DATA_2 ='" + WHTrx_Obj.VALUE + "', @EXTRA_DATA_3 ='" + WHTrx_Obj.EXTRA_DATA3 + "', @EXTRA_DATA_4 ='" + WHTrx_Obj.EXTRA_DATA4 + "', @EXTRA_DATA_5 ='" + WHTrx_Obj.EXTRA_DATA5 + "' ",
               _sqlCnn);
        }



        public DataSet sp_IMPORT_DATA_CUSTOM_VALIDATIONS(string customProcedure, WH_Obj WHTrx_Obj)
        {
            return SQLBase.ReturnDataSet("EXEC " + customProcedure + " @USERNAME ='" + WHTrx_Obj.USUARIO.USERNAME + "', @GUID ='" + WHTrx_Obj.GUID + "'",
               _sqlCnn);

        }


        public List<M_METAMASTER> GET_MMASTER(string mmCode)
        {
            try
            {
                return _dbx.M_METAMASTERs.Where(f => f.CLASS_CODE == mmCode).ToList();
            }
            catch
            {
                return new List<M_METAMASTER>();
            }

        }


        public DataSet sp_MPA_RECONCIL(string OPCION, WH_Obj WHTrx_Obj)
        {

            return SQLBase.ReturnDataSet("EXEC sp_MPA_RECONCIL @OPTION ='" + OPCION + "', @USERNAME ='" + WHTrx_Obj.USUARIO.USERNAME + "', @GUID ='" + WHTrx_Obj.GUID + "', @ROWID ='" + WHTrx_Obj.ROWID + "', @EXTRA_DATA ='" + WHTrx_Obj.EXTRA_DATA + "', @EXTRA_DATA_2 ='" + WHTrx_Obj.VALUE + "', @EXTRA_DATA_3 ='" + WHTrx_Obj.EXTRA_DATA3 + "', @EXTRA_DATA_4 ='" + WHTrx_Obj.EXTRA_DATA4 + "', @EXTRA_DATA_5 ='" + WHTrx_Obj.EXTRA_DATA5 + "' ",
               _sqlCnn);
        }


        public DataSet sp_MPA_CONFIG(string OPCION, File_Info fileInfo)
        {

            return SQLBase.ReturnDataSet("EXEC sp_MPA_CONFIG @OPTION ='" + OPCION + "', @ID =" + fileInfo.id + " , @sourceFile ='" + fileInfo.sourceFile + "', @name ='" + fileInfo.Name + "', @path ='" + fileInfo.path + "', @columnsQty=" + fileInfo.columnsQty + " , @rowsFrom =" + fileInfo.rowsFrom + ", @runConciliation =" + fileInfo.runConciliation + " , @ConciliateWith = '" + fileInfo.ConciliateWith + "' , @facilityCode = '" + fileInfo.facilityCode + "', @filetype  = '" + fileInfo.filetype + "' , @inactiveDate = ' " + fileInfo.inactiveDate + "',@merchantId='" + fileInfo.merchantId + "'",
                _sqlCnn);
        }

        public DataSet spBilling_Delivery(string OPCION, WH_Obj WHTrx_Obj)
        {
            return SQLBase.ReturnDataSet("EXEC spBilling_Delivery @OPTION ='" + OPCION + "', @USERNAME ='" + WHTrx_Obj.USUARIO.USERNAME + "', @EXTRA_DATA ='" + WHTrx_Obj.EXTRA_DATA + "', @START_DATE ='" + WHTrx_Obj.EXTRA_DATA2 + "', @END_DATE ='" + WHTrx_Obj.EXTRA_DATA3 + "'",
               _sqlCnn);
        }


        public DataSet sp_Views(string OPCION, WH_Obj WHTrx_Obj)
        {
            return SQLBase.ReturnDataSet("EXEC sp_Views @OPTION ='" + OPCION + "', @USERNAME ='" + WHTrx_Obj.USUARIO.USERNAME + "', @GUID ='" + WHTrx_Obj.GUID + "', @ROWID ='" + WHTrx_Obj.ROWID + "', @EXTRA_DATA ='" + WHTrx_Obj.EXTRA_DATA + "', @EXTRA_DATA_2 ='" + WHTrx_Obj.VALUE + "', @EXTRA_DATA_3 ='" + WHTrx_Obj.EXTRA_DATA3 + "', @EXTRA_DATA_4 ='" + WHTrx_Obj.EXTRA_DATA4 + "', @EXTRA_DATA_5 ='" + WHTrx_Obj.EXTRA_DATA5 + "' ",
               _sqlCnn);
        }

    }
}