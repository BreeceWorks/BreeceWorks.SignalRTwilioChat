namespace BreeceWorks.TwilioSignalRApp.Hubs
{
    public class ConnectedUser
    {
        public string Name { get; set; }
        public string UserIdentifier { get; set; }

        public List<Connection> Connections { get; set; }
    }
    public class Connection
    {
        public string ConnectionID { get; set; }

    }
}
