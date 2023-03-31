using Microsoft.IdentityModel.Clients.ActiveDirectory;
using PANGEA.IMPORTSUITE.DataModel.UAT;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

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


        static ImportSuiteServiceClient Get365Client()
        {

           
            string aosUri = "https://mtp-uat.sandbox.operations.dynamics.com/";
            string activeDirectoryTenant = "https://login.windows.net/mtpsites.com";
            string activeDirectoryClientAppId = "1eb33215-9cae-4efa-b931-a0caafbc7156";
            string activeDirectoryClientAppSecret = "fl8W+4lKudTI8Ikxo71ZBFxl2YduYqhjiiROpkNWyw8=";
            string activeDirectoryResource = "https://mtp-uat.sandbox.operations.dynamics.com";
      

            /*
            string aosUri = "https://mtpsites-devdevaos.sandbox.ax.dynamics.com/";
            string activeDirectoryTenant = "https://login.windows.net/mtpsites.com";
            string activeDirectoryClientAppId = "5686178d-3a6a-42c6-8460-d3d40cadb159";
            string activeDirectoryClientAppSecret = "r6JAExEY1Bn0yxxepJgXqu/tcHhXQZu3siOxW+/cFjc=";
            string activeDirectoryResource = "https://mtpsites-devdevaos.sandbox.ax.dynamics.com";
            */


            AuthenticationContext authenticationContext = new AuthenticationContext(activeDirectoryTenant);

            string aadClientAppSecret = activeDirectoryClientAppSecret;
            ClientCredential creadential = new ClientCredential(activeDirectoryClientAppId, aadClientAppSecret);
            AuthenticationResult authenticationResult = authenticationContext.AcquireTokenAsync(activeDirectoryResource, creadential).Result;
            oAuthHeaderClient = authenticationResult.CreateAuthorizationHeader();

            string serviceName = "PangeaServiceGroup";
            string soapServiceUriString = GetSoapServiceUriString(serviceName, aosUri);

            //manejo de los protocolos de seguridad
            if (System.Net.ServicePointManager.SecurityProtocol == (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls))
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            EndpointAddress endpointAddress = new EndpointAddress(soapServiceUriString);

            Binding binding = GetBinding();

            ImportSuiteServiceClient importSuiteClient = new ImportSuiteServiceClient(binding, endpointAddress);

            return importSuiteClient;
        }


        public static string AX365_import(string entity, string _parmURLFileCSV, string _parmFileName, string _parmSeparator, string _parmGUID, string _parmModule)
        {

            ImportSuiteServiceClient importSuiteClient = Get365Client();

            IClientChannel importSuiteChannel = importSuiteClient.InnerChannel;

            using (OperationContextScope dimServiceOperContext = new OperationContextScope(importSuiteChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();

                requestMessage.Headers[OAuthHeader] = oAuthHeaderClient;

                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                //Call method of teh service
                PANGEA_import request = new PANGEA_import
                {
                    CallContext = new CallContext { Company = entity },
                    _parmCompany = entity,
                    _parmFileName = _parmFileName,
                    _parmGUID = _parmGUID,
                    _parmModule = _parmModule,
                    _parmSeparator = _parmSeparator,
                    _parmURLFileCSV = _parmURLFileCSV

                };

                string result = ((ImportSuiteService)importSuiteChannel).PANGEA_import(request).result;

                return result;
            }

        }


        public static string AX365_validate(string entity, string _parmGUID, string _parmModule)
        {

            ImportSuiteServiceClient importSuiteClient = Get365Client();

            IClientChannel importSuiteChannel = importSuiteClient.InnerChannel;

            using (OperationContextScope dimServiceOperContext = new OperationContextScope(importSuiteChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();

                requestMessage.Headers[OAuthHeader] = oAuthHeaderClient;

                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                //Call method of teh service
                PANGEA_validate request = new PANGEA_validate
                {
                    CallContext = new CallContext { Company = entity },
                    _parmCompany = entity,
                    _parmGUID = _parmGUID,
                    _parmModule = _parmModule
                  
                };

                string result = ((ImportSuiteService)importSuiteChannel).PANGEA_validate(request).result;

                return result;
            }


        }


        public static string AX365_process(string entity, string _parmGUID, string _parmJournal, string _parmModule, string _parmUserId)
        {

            ImportSuiteServiceClient importSuiteClient = Get365Client();

            IClientChannel importSuiteChannel = importSuiteClient.InnerChannel;

            using (OperationContextScope dimServiceOperContext = new OperationContextScope(importSuiteChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();

                requestMessage.Headers[OAuthHeader] = oAuthHeaderClient;

                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                //Call method of teh service
                PANGEA_process request = new PANGEA_process
                {
                    CallContext = new CallContext { Company = entity },
                    _parmCompany = entity,
                    _parmGUID = _parmGUID,
                    _parmModule = _parmModule,
                    _parmJournal = _parmJournal,
                    _parmUserId = _parmUserId


                };

                string result = ((ImportSuiteService)importSuiteChannel).PANGEA_process(request).result;

                return result;
            }

        }


        public static DataSet AX365_showrecords(string entity, string _parmGUID, string _parmModule, string _parmRegType)
        {

            ImportSuiteServiceClient importSuiteClient = Get365Client();

            IClientChannel importSuiteChannel = importSuiteClient.InnerChannel;

            using (OperationContextScope dimServiceOperContext = new OperationContextScope(importSuiteChannel))
            {
                HttpRequestMessageProperty requestMessage = new HttpRequestMessageProperty();

                requestMessage.Headers[OAuthHeader] = oAuthHeaderClient;

                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                //Call method of teh service
                PANGEA_showrecords request = new PANGEA_showrecords
                {
                    CallContext = new CallContext { Company = entity },
                    _parmCompany = entity,
                    _parmGUID = _parmGUID,
                    _parmModule = _parmModule,
                    _parmRegType = _parmRegType 
                };

                string result = ((ImportSuiteService)importSuiteChannel).PANGEA_showrecords(request).result;

                DataSet dsResult = UtilTool.GetDataSetFromString(result);

                return dsResult;
            }


        }


    }
}
