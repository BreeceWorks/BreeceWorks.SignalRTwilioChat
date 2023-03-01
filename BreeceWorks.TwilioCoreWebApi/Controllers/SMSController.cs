using BreeceWorks.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Win32;
using System.Diagnostics;
using Twilio;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;
using Task = System.Threading.Tasks.Task;
using IdentityModel.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using NuGet.Protocol.Plugins;
using static Twilio.Rest.Api.V2010.Account.MessageResource;
using System.Collections.Generic;
using Twilio.TwiML.Messaging;
using Twilio.TwiML.Voice;
using Twilio.Types;

namespace BreeceWorks.TwilioCoreWebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SmsController : TwilioController
    {
        private IWebHostEnvironment _env;
        private ITempDataDictionary? _tempData;
        private ViewDataDictionary? _viewData;
        private IConfiguration _configuration;

        public SmsController(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        [HttpPost(Name = "Outgoing")]
        public void Outgoing(String message, String toNmber, String fromNumber)
        {
            String authToken = _configuration["Twilio:AuthToken"];
            string accountSid = _configuration["Twilio:Client:AccountSid"];

            TwilioClient.Init(accountSid, authToken);

            MessageResource sentMessage = MessageResource.Create(
                body: message,
            from: new Twilio.Types.PhoneNumber(fromNumber),
            to: new Twilio.Types.PhoneNumber(toNmber),
            statusCallback: new Uri(_configuration["Twilio:StatusCallbackUrl"]),
            pathAccountSid : null, 
            messagingServiceSid : null, 
            mediaUrl : null, 
            applicationSid : null, 
            maxPrice : null, 
            provideFeedback : null, 
            attempt : null, 
            validityPeriod : null, 
            forceDelivery : null, 
            contentRetention : null, 
            addressRetention : null, 
            smartEncoded : null, 
            persistentAction : null, 
            shortenUrls : null, 
            scheduleType : null, 
            sendAt : null, 
            sendAsMms : null, 
            contentSid : null, 
            contentVariables : null
            );
            LogMessageResource("Outgoing", sentMessage);
        }
        private const string SavePath = @"\App_Data\";
        [HttpPost(Name = "Incoming")]
        [ValidateTwilioRequest]
        public async Task<TwiMLResult> Incoming([FromForm] SmsRequest request, [FromForm] int numMedia)
        {
            LogSMSRequest("SMS Incoming", request);
            await SaveImages(numMedia);
            var response = new MessagingResponse();
            RelayMessageToChat(request);
            return TwiML(response);
        }
        [HttpPost(Name = "sms_status_callback")]
        [ValidateTwilioRequest]
        public TwiMLResult sms_status_callback([FromForm] SmsRequest request)
        {
            LogSMSRequest("SMS Status Callback", request);
            var response = new MessagingResponse();
            return TwiML(response);
        }

        private static void LogSMSRequest(String description, SmsRequest request)
        {
            Debug.WriteLine(description);
            Debug.WriteLine("SMS SID: " + request.SmsSid);
            Debug.WriteLine("Message Status: " + request.MessageStatus);
            Debug.WriteLine("Account SID: " + request.AccountSid);
            Debug.WriteLine("From: " + request.From);
            Debug.WriteLine("TO: " + request.To);
            Debug.WriteLine("Body: " + request.Body);
            Debug.WriteLine("Opt Out Type: " + request.OptOutType);
            Debug.WriteLine("Messaging Service SID: " + request.MessagingServiceSid);
            Debug.WriteLine("From City: " + request.FromCity);
            Debug.WriteLine("From State: " + request.FromState);
            Debug.WriteLine("From Zip: " + request.FromZip);
            Debug.WriteLine("From Country: " + request.FromCountry);
            Debug.WriteLine("To City: " + request.ToCity);
            Debug.WriteLine("To State: " + request.ToState);
            Debug.WriteLine("To Zip: " + request.ToZip);
            Debug.WriteLine("To Country: " + request.ToCountry);
        }

        private void LogMessageResource(String description, MessageResource sentMessage)
        {
            Debug.WriteLine(description);
            Debug.WriteLine("SID: " + sentMessage.Sid);
            Debug.WriteLine("Account SID: " + sentMessage.AccountSid);
            Debug.WriteLine("API Version: " + sentMessage.ApiVersion);
            Debug.WriteLine("Body: " + sentMessage.Body);
            Debug.WriteLine("Date Created: " + sentMessage.DateCreated);
            Debug.WriteLine("Date Sent: " + sentMessage.DateSent);
            Debug.WriteLine("Date Updated: " + sentMessage.DateUpdated);
            Debug.WriteLine("Direction: " + sentMessage.Direction);
            Debug.WriteLine("Error Code: " + sentMessage.ErrorCode);
            Debug.WriteLine("Num Media: " + sentMessage.NumMedia);
            Debug.WriteLine("Error Message: " + sentMessage.ErrorMessage);
            Debug.WriteLine("From: " + sentMessage.From);
            Debug.WriteLine("Messaging Service Sid: " + sentMessage.MessagingServiceSid);
            Debug.WriteLine("Num Segments: " + sentMessage.NumSegments);
            Debug.WriteLine("Price: " + sentMessage.Price);
            Debug.WriteLine("PriceUnit: " + sentMessage.PriceUnit);
            Debug.WriteLine("Status: " + sentMessage.Status);
            for (int x = 0; x < sentMessage.SubresourceUris.Count; x++)
            {
                Debug.WriteLine("Subresource Uri key: {0} value: {1}", sentMessage.SubresourceUris.Keys.ElementAt(x),
                                     sentMessage.SubresourceUris[sentMessage.SubresourceUris.Keys.ElementAt(x)]);
            }
            Debug.WriteLine("To: " + sentMessage.To);
            Debug.WriteLine("Uri: " + sentMessage.Uri);
        }

        private async void RelayMessageToChat(SmsRequest request)
        {
            using var tokenClient = new HttpClient();

            var tokenResponse = await tokenClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = "https://demo.duendesoftware.com/connect/token",

                ClientId = "m2m",
                ClientSecret = "secret",
                Scope = "api"
            });
            var accessToken = tokenResponse.AccessToken;

            var connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7044/chathub",
                    o => o.AccessTokenProvider = () => Task.FromResult(accessToken))
                .Build();

            await connection.StartAsync();
            await connection.SendAsync("SendMessage", GetChatUserID(request), request.Body, GetChatFrom(request));
            await connection.StopAsync();

        }

        private String GetChatFrom(SmsRequest request)
        {
            //TODO: create logic to determine who is chatting on this sms thread
            return "Ben";
        }

        private String GetChatUserID(SmsRequest request)
        {
            //TODO: create logic to determine who is chatting on this sms thread
            return "bob smith";
        }

        //Save images sent by SMS
        private async Task SaveImages(int numMedia)
        {
            for (var i = 0; i < numMedia; i++)
            {
                var mediaUrl = Request.Form[$"MediaUrl{i}"];
                Trace.WriteLine(mediaUrl);
                var contentType = Request.Form[$"MediaContentType{i}"];

                var filePath = GetMediaFileName(mediaUrl, contentType);
                await DownloadUrlToFileAsync(mediaUrl, filePath);
            }
        }

        private string GetMediaFileName(string mediaUrl,
            string contentType)
        {

            return _env.WebRootPath +
                // e.g. ~/App_Data/MExxxx.jpg
                SavePath +
                Path.GetFileName(mediaUrl) +
                GetDefaultExtension(contentType);
        }

        private static async Task DownloadUrlToFileAsync(string mediaUrl,
            string filePath)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(mediaUrl);
                var httpStream = await response.Content.ReadAsStreamAsync();
                using (var fileStream = System.IO.File.Create(filePath))
                {
                    await httpStream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
        }

        public static string GetDefaultExtension(string mimeType)
        {
            // NOTE: This implementation is Windows specific (uses Registry)
            // Platform independent way might be to download a known list of
            // mime type mappings like: http://bit.ly/2gJYKO0
            var key = Registry.ClassesRoot.OpenSubKey(
                @"MIME\Database\Content Type\" + mimeType, false);
            var ext = key?.GetValue("Extension", null)?.ToString();
            return ext ?? "application/octet-stream";
        }

    }
}
