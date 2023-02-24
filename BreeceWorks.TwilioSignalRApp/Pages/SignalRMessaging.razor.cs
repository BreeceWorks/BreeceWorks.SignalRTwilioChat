using BreeceWorks.TwilioSignalRApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BreeceWorks.TwilioSignalRApp.Pages
{
    public partial class SignalRMessaging
    {
        [Inject]
        public TokenManager _tokenManager { get; set; }

        [Inject]
        IConfiguration _configuration { get; set; }

        [Inject]
        private ISMSService _smsService { get; set; }

        public string AccessToken { get; set; }

        private HubConnection? hubConnection;
        private List<string> messages = new List<string>();
        private List<string> privateMessages = new List<string>();
        private string? userInput;
        private string? messageInput;
        protected override async Task OnInitializedAsync()
        {
            try
            {
                AccessToken = await _tokenManager.RetrieveAccessTokenAsync();
            }
            catch (Exception ex)
            {
            }

            hubConnection = new HubConnectionBuilder()
                        .WithUrl(Navigation.ToAbsoluteUri("/chathub"),
                            o => o.AccessTokenProvider = () => Task.FromResult(AccessToken))
                        .Build();

            //hubConnection = new HubConnectionBuilder().WithUrl(Navigation.ToAbsoluteUri("/chathub")).Build();
            hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                var encodedMsg = $"{user}: {message}";
                messages.Add(encodedMsg);
                InvokeAsync(StateHasChanged);
            });

            hubConnection.On<string, string>("ReceivePrivateMessage", (user, message) =>
            {
                var encodedMsg = $"{user}: {message}";
                privateMessages.Add(encodedMsg);

                InvokeAsync(() => StateHasChanged());
            });

            try
            {
                await hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
            }
        }

        private async Task Send()
        {
            String fromPhoneNumber = GetFromPhoneNumber();
            String toPhoneNumber = GetToPhoneNumber(userInput);
            await _smsService.SendSMS(messageInput != null ? messageInput : String.Empty, toPhoneNumber, fromPhoneNumber);
            //uswed for sending message through Signal R
            //if (hubConnection != null)
            //{
            //    await hubConnection.SendAsync("SendMessage", userInput, messageInput);
            //}
        }

        private String GetToPhoneNumber(string? userInput)
        {
            //TODO: create logic for determining to phone number
            return _configuration["TestPhoneNumber"];
        }

        private string GetFromPhoneNumber()
        {
            //TODO: create logic for determining from phone number
            return "6154377795";
        }

        public bool IsConnected => hubConnection?.State == HubConnectionState.Connected;
        public async ValueTask DisposeAsync()
        {
            if (hubConnection != null)
            {
                await hubConnection.DisposeAsync();
            }
        }

    }
}