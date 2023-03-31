using Microsoft.IdentityModel.Clients.ActiveDirectory;
using PANGEA.IMPORTSUITE.DataModel.AX365_DIRECT_IMPORT;
using System;
using System.Data;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;


namespace PANGEA.IMPORTSUITE.DataModel.Util
{

    public class AX365
    {
        public const string OAuthHeader = "Authorization";
        public static string oAuthHeaderClient;

        /// <summary>
        ///     Format SOAP Service string
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <param name="aosUriString">D365O connection string</param>
        /// <returns></returns>
        static string GetSoapServiceUriString(string serviceName, string aosUriString)
        {
            string soapServiceUriStringTemplate = "{0}/soap/services/{1}";
            string soapServiceUriString = string.Format(soapServiceUriStringTemplate, aosUriString.TrimEnd('/'), serviceName);
            return soapServiceUriString;
        }

        /// <summary>
        ///     Get service binding
        /// </summary>
        /// <returns>Initalized <c>Binding</c></returns>
        static Binding GetBinding()
        {
            BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);

            // Set binding timeout and other configuration settings
            binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
            binding.ReaderQuotas.MaxArrayLength = int.MaxValue;
            binding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;

            binding.ReceiveTimeout = TimeSpan.MaxValue;
            binding.SendTimeout = TimeSpan.MaxValue;
            binding.MaxReceivedMessageSize = int.MaxValue;

            HttpsTransportBindingElement httpsTransportBindingElement = binding.CreateBindingElements().OfType<HttpsTransportBindingElement>().FirstOrDefault();
            if (httpsTransportBindingElement != null)
            {
                httpsTransportBindingElement.MaxPendingAccepts = 10000; // Largest posible is 100000, otherwise throws
            }

            HttpTransportBindingElement httpTransportBindingElement = binding.CreateBindingElements().OfType<HttpTransportBindingElement>().FirstOrDefault();
            if (httpTransportBindingElement != null)
            {
                httpTransportBindingElement.MaxPendingAccepts = 10000; // Largest posible is 100000, otherwise throws
            }

            return binding;
        }


        static ImportSuiteDataClient Get365Client(string _ENV, ImportSuiteConnection curConnection)
        {
            #region CONNECTION OLD VERSION
            /*
            //LIVE
            string aosUri = "https://mtp-prod.operations.dynamics.com/";
            string activeDirectoryTenant = "https://login.windows.net/mtpsites.com";
            string activeDirectoryClientAppId = "19a5c822-44bc-4cd6-8196-27f5afc45d3a";
            string activeDirectoryClientAppSecret = "JqRzudXxaYa0JrIi/5E67kp/7/UDkAkuIb4aHuLDOlg=";
            string activeDirectoryResource = "https://mtp-prod.operations.dynamics.com";
 

            if (_ENV == "UAT")
            {

                //UAT
                aosUri = "https://mtp-uat.sandbox.operations.dynamics.com/";
                activeDirectoryTenant = "https://login.windows.net/mtpsites.com";
                activeDirectoryClientAppId = "1eb33215-9cae-4efa-b931-a0caafbc7156";
                activeDirectoryClientAppSecret = "fl8W+4lKudTI8Ikxo71ZBFxl2YduYqhjiiROpkNWyw8=";
                activeDirectoryResource = "https://mtp-uat.sandbox.operations.dynamics.com";

            }
            else if (_ENV == "DEV")
            {
                //DEV
                aosUri = "https://mtpsites-devdevaos.sandbox.ax.dynamics.com/";
                activeDirectoryTenant = "https://login.windows.net/mtpsites.com";
                activeDirectoryClientAppId = "5686178d-3a6a-42c6-8460-d3d40cadb159";
                activeDirectoryClientAppSecret = "r6JAExEY1Bn0yxxepJgXqu/tcHhXQZu3siOxW+/cFjc=";
                activeDirectoryResource = "https://mtpsites-devdevaos.sandbox.ax.dynamics.com";

            }
            */
            #endregion


            //SOME SAMPLES 
            //https://mtpsites-devdevaos.sandbox.ax.dynamics.com/|https://login.windows.net/mtpsites.com|5686178d-3a6a-42c6-8460-d3d40cadb159|r6JAExEY1Bn0yxxepJgXqu/tcHhXQZu3siOxW+/cFjc=| 
            //https://mtp-uat.sandbox.operations.dynamics.com/|https://login.windows.net/mtpsites.com|1eb33215-9cae-4efa-b931-a0caafbc7156|fl8W+4lKudTI8Ikxo71ZBFxl2YduYqhjiiROpkNWyw8=|


            string aosUri = "";
            string activeDirectoryTenant = "";
            string activeDirectoryClientAppId = "";
            string activeDirectoryClientAppSecret = "";
            string activeDirectoryResource = "";

            string[] activeConnection = null;
            try
            {
                activeConnection = UtilTool.ObtenerParametro(_ENV, curConnection).Split('|');
            }
            catch
            {
                throw new Exception("Parameter 365 CONNECTION Not defined.");
            }

             aosUri = activeConnection[0];
             activeDirectoryTenant = activeConnection[1];
             activeDirectoryClientAppId = activeConnection[2];
             activeDirectoryClientAppSecret = activeConnection[3].Trim();
             activeDirectoryResource = activeConnection[0].Remove(activeConnection[0].Length - 1); 


            AuthenticationContext authenticationContext = new AuthenticationContext(activeDirectoryTenant);

            string aadClientAppSecret = activeDirectoryClientAppSecret;
            ClientCredential creadential = new ClientCredential(activeDirectoryClientAppId, aadClientAppSecret);
            AuthenticationResult authenticationResult = authenticationContext.AcquireTokenAsync(activeDirectoryResource, creadential).Result;
            oAuthHeaderClient = authenticationResult.CreateAuthorizationHeader();

            string serviceName = "pangeagrpsvc";
            string soapServiceUriString = GetSoapServiceUriString(serviceName, aosUri);

            //manejo de los protocolos de seguridad
            if (System.Net.ServicePointManager.SecurityProtocol == (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls))
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            EndpointAddress endpointAddress = new EndpointAddress(soapServiceUriString);
            Binding binding = GetBinding();

            ImportSuiteDataClient importSuiteClient = new ImportSuiteDataClient(binding, endpointAddress);

            return importSuiteClient;
        }


        public static string AX365_import(AX365_Params ax365p)
        {

            ImportSuiteDataClient importSuiteClient = Get365Client(ax365p._ENV, ax365p.curConnection);

            IClientChannel importSuiteChannel = importSuiteClient.InnerChannel;

            using (OperationContextScope dimServiceOperContext = new OperationContextScope(importSuiteChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();

                requestMessage.Headers[OAuthHeader] = oAuthHeaderClient;

                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                //CompressData 
                byte[] zipData = CompressData.ZipData(ax365p._parmURLFileCSV);
                //Pass bytes to base64
                string base64file = Convert.ToBase64String(zipData);

                //string base64file = UtilTool.Base64Encode(_parmURLFileCSV);

                //Call method of teh service
                PANGEA_import_v2 request = new PANGEA_import_v2
                {
                    CallContext = new CallContext { Company = ax365p.entity },
                    _parmCompany = ax365p.entity,
                    _parmFileName = ax365p._parmFileName,
                    _parmGUID = ax365p._parmGUID,
                    _parmModule = ax365p._parmModule,
                    _parmSeparator = ax365p._parmSeparator,
                    _parmURLFileCSV = base64file,
                    _parmUserId = (ax365p._ENV == "DEV") ? "pangea1" : ax365p._parmUserId

                };


                string result = ((ImportSuiteData)importSuiteChannel).PANGEA_import_v2(request).result;

                return result;
            }

        }


        public static string AX365_validate(AX365_Params ax365p, string executeValidation)
        {

            ImportSuiteDataClient importSuiteClient = Get365Client(ax365p._ENV, ax365p.curConnection);

            IClientChannel importSuiteChannel = importSuiteClient.InnerChannel;

            using (OperationContextScope dimServiceOperContext = new OperationContextScope(importSuiteChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();

                requestMessage.Headers[OAuthHeader] = oAuthHeaderClient;

                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                //Call method of teh service
                PANGEA_validate_v2 request = new PANGEA_validate_v2
                {
                    CallContext = new CallContext { Company = ax365p.entity },
                    _parmCompany = ax365p.entity,
                    _parmGUID = ax365p._parmGUID,
                    _parmModule = ax365p._parmModule,
                    _parmValidate = executeValidation
                };

                string result = "";

                try { result = ((ImportSuiteData)importSuiteChannel).PANGEA_validate_v2(request).result; }

                catch (Exception ex)
                {
                    result = ex.Message;
                }



                return result;
            }


        }


        public static string AX365_process(AX365_Params ax365p)
        {

            ImportSuiteDataClient importSuiteClient = Get365Client(ax365p._ENV, ax365p.curConnection);

            IClientChannel importSuiteChannel = importSuiteClient.InnerChannel;

            using (OperationContextScope dimServiceOperContext = new OperationContextScope(importSuiteChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();

                requestMessage.Headers[OAuthHeader] = oAuthHeaderClient;

                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                //Call method of teh service
                PANGEA_process_v2 request = new PANGEA_process_v2
                {
                    CallContext = new CallContext { Company = ax365p.entity },
                    _parmCompany = ax365p.entity,
                    _parmGUID = ax365p._parmGUID,
                    _parmModule = ax365p._parmModule,
                    _parmJournal = ax365p._parmJournal,
                    _parmUserId = ax365p._parmUserId
                };

                string result = ((ImportSuiteData)importSuiteChannel).PANGEA_process_v2(request).result;

                return result;
            }

        }


        public static DataSet AX365_showrecords(AX365_Params ax365p)
        {

            ImportSuiteDataClient importSuiteClient = Get365Client(ax365p._ENV, ax365p.curConnection);

            IClientChannel importSuiteChannel = importSuiteClient.InnerChannel;

            using (OperationContextScope dimServiceOperContext = new OperationContextScope(importSuiteChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();

                requestMessage.Headers[OAuthHeader] = oAuthHeaderClient;

                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                //Call method of teh service
                PANGEA_showrecords_v2 request = new PANGEA_showrecords_v2
                {
                    CallContext = new CallContext { Company = ax365p.entity },
                    _parmCompany = ax365p.entity,
                    _parmGUID = ax365p._parmGUID,
                    _parmModule = ax365p._parmModule,
                    _parmRegType = ax365p._parmRegType 
                };

                string result = ((ImportSuiteData)importSuiteChannel).PANGEA_showrecords_v2(request).result;

                //From base 64 to bytes
                //byte[] unZipData = Convert.FromBase64String(result);
                //result = CompressData.UnzipData(unZipData);

                DataSet dsResult = UtilTool.GetDataSetFromString(result);

                return dsResult;
            }


        }


    }


    public class AX365_Params
    {

        public string entity { get; set; }
        public string _parmURLFileCSV { get; set; }
        public string _parmFileName { get; set; }
        public string _parmSeparator { get; set; }
        public string _parmGUID { get; set; }
        public string _parmModule { get; set; }
        public string _parmJournal { get; set; }
        public string _parmRegType { get; set; }
        public string _ENV { get; set; }
        public string _parmUserId { get; set; }
        public ImportSuiteConnection curConnection { get; set; }
    }

}
