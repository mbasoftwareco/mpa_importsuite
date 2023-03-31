using PANGEA.IMPORTSUITE.DataModel.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PANGEA.IMPORTSUITE.ErpFactory
{
    // Abstract class Connect Factory
    public abstract class ConnectFactory
    {

        // List of Erp types supported by the factory

        //Methods to implement.
        public abstract IProcessService ErpService();


        public static ErpConnection FactoryConnection;
 
        /// <summary>
        /// Factory que retorna la conexion al ERP Especifico
        /// </summary>
        /// <param name="whichFactory"></param>
        /// <returns></returns>
        public static ConnectFactory getConnectFactory(ErpConnection curConnection)
        {
            FactoryConnection = curConnection;
     
            switch (curConnection.cnnType)
            {

                case "MPA.DLL":
                    return new MPA_DLL.MPA_DLL();

                case "MPA.WS":
                    return new MPA_WS.MPA_WS();


                default:
                    return null;
            }
        }


        public interface IProcessService
        {
            string ImportData(string filePath, string fileName, string separator, string gUID, string module, string entity);

            string ValidateData(string gUID, string module, string entity);

            string ProcessData(string gUID, string module, string entity, string journalName, string userLogin, string autoPost);
        }

    }
}
