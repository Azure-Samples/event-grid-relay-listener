
//   
//   Copyright © Microsoft Corporation, All Rights Reserved
// 
//   Licensed under the Apache License, Version 2.0 (the "License"); 
//   you may not use this file except in compliance with the License. 
//   You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0 
// 
//   THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
//   OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION
//   ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A
//   PARTICULAR PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
// 
//   See the Apache License, Version 2.0 for the specific language
//   governing permissions and limitations under the License. 

namespace GridRelayListener
{
    using System;
    using System.IO;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using Microsoft.ServiceBus;
    using Newtonsoft.Json.Linq;

    [ServiceContract]
    class GridServiceListener
    {
        // Replace the following three values with your Serice Bus
        // namespace, key name, and key value. You only need to
        // change the key name if you changed it.
        const string sbNamespace = "<yourServiceBusNamespace>";
        const string sbKeyName = "RootManageSharedAccessKey";
        const string sbKeyValue = "<yourServiceBusKeyValue>";

        const string path = "gridservicelistener";

        static void Main()
        {

            GridServiceListener svc = new GridServiceListener();
            svc.Run(sbNamespace, path, sbKeyName, sbKeyValue);
        }

        public void Run(string sbNamespace, string path, string keyName, string keyValue)
        {
            string httpsAddress = new UriBuilder("https", sbNamespace, -1, path).ToString();
            using (var host = new WebServiceHost(GetType()))
            {
                // Add an endpoint to receive requests over HTTPS without requiring Auth at the Relay Service
                var webBinding = new WebHttpRelayBinding(EndToEndWebHttpSecurityMode.Transport, RelayClientAuthenticationType.None);
                var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, keyValue);
                var transportBehavior = new TransportClientEndpointBehavior(tokenProvider);
                var endpoint = host.AddServiceEndpoint(typeof(GridServiceListener), webBinding, httpsAddress);
                endpoint.EndpointBehaviors.Add(transportBehavior);

                host.Open();
                Console.WriteLine($"Listening at: {httpsAddress}?code={sbKeyValue}");

                Console.WriteLine("Press [Enter] to exit");
                Console.ReadLine();

                host.Close();
            }
        }

        [OperationContract, WebInvoke(Method = "POST", UriTemplate = "")]
        Stream GridEventHandler(Stream input)
        {
            // Check Auth and return immediately if no valid code is provided
            if (!AuthorizedRequest(WebOperationContext.Current))
            {
                Console.WriteLine("Received unauthorized request");
                return UnauthorizedStream(WebOperationContext.Current);
            }

            string body = new StreamReader(input).ReadToEnd();
            PrintRequest(WebOperationContext.Current, body);

            AzureEventGridEvent[] events = AzureEventGridEvent.DeserializeAzureEventGridEvents(body);

            // If event received is a SubscriptionValidationEvent, handle it appropriately
            if (events.Length == 1 && events[0].EventType == "Microsoft.EventGrid/SubscriptionValidationEvent")
            {
                // For a validation event, return the cookie embedded in the data.validationCode field.
                return ValidationResponseStream(WebOperationContext.Current, events[0]);
            }
            else
            {
                // For any event which is not a SubscriptionValidateEvent, just return 200 - Got It
                return GotItStream(WebOperationContext.Current);
            }
        }

        void PrintRequest(WebOperationContext ctx, string body)
        {
            // Print headers and body of request to console
            string curTime = DateTime.Now.ToString("hh.mm.ss.fff");
            Console.WriteLine($"Received a HTTP POST from EventGrid at time:{curTime}");
            foreach (var header in ctx.IncomingRequest.Headers.AllKeys)
            {
                Console.WriteLine($"{header}: {ctx.IncomingRequest.Headers[header]}");
            }
            Console.WriteLine(body);
        }

        bool AuthorizedRequest(WebOperationContext ctx)
        {
            // Incoming requests need to have a code query parameter with same Key as the Relay Key to be authorized
            string code = ctx.IncomingRequest.UriTemplateMatch.QueryParameters["code"];
            return code == sbKeyValue;
        }

        Stream ValidationResponseStream(WebOperationContext ctx, AzureEventGridEvent validationEvent)
        {
            string validationCode = (string)((JObject)validationEvent.Data)["validationCode"];
            ctx.OutgoingResponse.ContentType = "application/json";
            string responseBody = "{\"validationResponse\": \"" + validationCode + "\"}";
            Stream output = new MemoryStream(System.Text.UTF8Encoding.UTF8.GetBytes(responseBody));
            output.Position = 0;
            return output;
        }

        Stream GotItStream(WebOperationContext ctx)
        {
            ctx.OutgoingResponse.ContentType = "text/plain";
            Stream output = new MemoryStream(System.Text.UTF8Encoding.UTF8.GetBytes("Got it"));
            output.Position = 0;
            return output;
        }

        Stream UnauthorizedStream(WebOperationContext ctx)
        {
            ctx.OutgoingResponse.ContentType = "text/plain";
            Stream output = new MemoryStream(System.Text.UTF8Encoding.UTF8.GetBytes("Unauthorized, please include a 'code' query parameter with the appropriate value"));
            output.Position = 0;
            ctx.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            return output;
        }
    }
}