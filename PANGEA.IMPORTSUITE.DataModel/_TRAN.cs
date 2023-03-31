using PANGEA.IMPORTSUITE.DataModel.AX_IMPORT;
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
    public class _TRAN
    {


        public IMPORTSUITE_DAODataContext _db { get; set; }
        public string _db_cnn { get; set; }

        public _WEB _web { get; set; }


        public _TRAN()
        {
            _db = new IMPORTSUITE_DAODataContext();
            _db_cnn = _db.Connection.ConnectionString;
        }

        public void ERP_PROCESS_DATA_TEMPLATE(string uploadFilePath, T_TEMPLATE_RUNTIME tplRuntime)
        {

            PANGEA_IMPORT_SUITEClient svcCliente = new PANGEA_IMPORT_SUITEClient();

            string ERP_result = svcCliente.dataUploadByTemplate(new CallContext { Company = "MPA" }, uploadFilePath, "TESTING " + tplRuntime.GUID, "GL");


            //El template me indicas si se maneja en BATCH or One by One.
            int regROWID = 0;

            if (ERP_result.Contains("ERROR"))
                UpdateRecordAfterProcess(tplRuntime, regROWID,  Constant.STATUS_WITH_ERROR, ERP_result);

            else if (ERP_result == "REG")
                UpdateRecordAfterProcess(tplRuntime, regROWID, Constant.STATUS_SENT_TO_ERP, "SENT TO ERP, POSTING PENDING");  //SENT

            else
                UpdateRecordAfterProcess(tplRuntime, regROWID, Constant.STATUS_POSTED, ERP_result);  //POSTED

        }


        private void UpdateRecordAfterProcess(T_TEMPLATE_RUNTIME curTplRuntime, int curRecord, int status, string ERP_result)
        {
            // MARCAR LOS REGISTROS PROCESADOS. INDIVIDUAL OR TOTAL

            WH_Obj tranObj = null;

            if (curRecord == 0)  // 0 => afects all recods with the same GUID
            {
                tranObj = new WH_Obj
                {
                    USUARIO = new S_USER { USERNAME = curTplRuntime.CREATEDBY },

                    ROWID = status,  //STATUS IN ROWID

                    GUID = curTplRuntime.GUID,

                    EXTRA_DATA = ERP_result 
                };

                _web.sp_IMPORT_DATA("UPDATE_AFTER_ERP_BATCH", tranObj);

            }
            else if (curRecord > 0)  // afects only a specific rowid
            {
                tranObj = new WH_Obj
                {
                    USUARIO = new S_USER { USERNAME = curTplRuntime.CREATEDBY },

                    ROWID = curRecord,

                    GUID = curTplRuntime.GUID,

                    EXTRA_DATA = ERP_result,

                    VALUE = status.ToString()
                };

                _web.sp_IMPORT_DATA("UPDATE_AFTER_ERP_ROWID", tranObj);

            }

        }

    }
}
