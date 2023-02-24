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

            var sentMessage = MessageResource.Create(
                body: message,
                from: new Twilio.Types.PhoneNumber(fromNumber),
                to: new Twilio.Types.PhoneNumber(toNmber)
            );
        }

        private const string SavePath = @"\App_Data\";

        [HttpPost(Name = "Incoming")]
        [ValidateTwilioRequest]
        public async Task<TwiMLResult> Incoming([FromForm] SmsRequest request, [FromForm] int numMedia)
        {
            await SaveImages(numMedia);

            var response = new MessagingResponse();

            RelayMessageToChat(request);

            return TwiML(response);
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
