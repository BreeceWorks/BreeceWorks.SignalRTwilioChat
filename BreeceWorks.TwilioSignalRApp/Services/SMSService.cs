namespace BreeceWorks.TwilioSignalRApp.Services
{
    public class SMSService:ISMSService
    {
        private readonly HttpClient _httpClient;
        private IConfiguration _configuration;

        public SMSService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task SendSMS(string message, string toNmber, string fromNumber)
        {
             await _httpClient.PostAsync(String.Format(_configuration["SMSOutgoingUrl"], message, toNmber, fromNumber), null);
        }
    }
}
