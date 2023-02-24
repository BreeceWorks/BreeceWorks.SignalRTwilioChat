namespace BreeceWorks.TwilioSignalRApp.Services
{
    public interface ISMSService
    {
        Task SendSMS(String message, String toNmber, String fromNumber);
    }
}
