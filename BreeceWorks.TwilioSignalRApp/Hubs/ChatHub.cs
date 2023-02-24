using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BreeceWorks.TwilioSignalRApp.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," +
CookieAuthenticationDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private static List<ConnectedUser> connectedUsers = new List<ConnectedUser>();

        public async Task SendMessage(string user, string message, string from)
        {
            var userIdentifier = (from _connectedUser in connectedUsers
                                  where _connectedUser.Name.ToLower() == user.ToLower()
                                  select _connectedUser.UserIdentifier).FirstOrDefault();



            if (string.IsNullOrEmpty(user) || user == "anonymous" || userIdentifier == null)
            {
                await Clients.All.SendAsync("ReceiveMessage", Context.User.Identity.Name ?? "anonymous", message);
            }
            else
            {
                String userName = Context.User.Identity.Name;
                if (string.IsNullOrEmpty(userName))
                {
                    var claim = Context.User.Claims.FirstOrDefault(c => c.Type == "name");
                    if (claim != null)
                    {
                        userName = claim.Value;
                    }
                }
                if (String.IsNullOrEmpty(from) && !String.IsNullOrEmpty(userName)) 
                { 
                    from= userName;
                }
                await Clients.User(userIdentifier).SendAsync("ReceivePrivateMessage",
                                       from, message);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {

            var user = connectedUsers.Where(cu => cu.UserIdentifier == Context.UserIdentifier).FirstOrDefault();

            var connection = user.Connections.Where(c => c.ConnectionID == Context.ConnectionId).FirstOrDefault();
            var count = user.Connections.Count;

            if (count == 1) // A single connection: remove user
            {
                connectedUsers.Remove(user);

            }
            if (count > 1) // Multiple connection: Remove current connection
            {
                user.Connections.Remove(connection);
            }

            var list = (from _user in connectedUsers
                        select new { _user.Name }).ToList();

        }


        public override async Task OnConnectedAsync()
        {
            var user = connectedUsers.Where(cu => cu.UserIdentifier == Context.UserIdentifier).FirstOrDefault();

            if (user == null) // User does not exist
            {
                String userName = Context.User.Identity.Name;
                if (string.IsNullOrEmpty(userName))
                {
                    var claim = Context.User.Claims.FirstOrDefault(c => c.Type == "name");
                    if (claim != null)
                    {
                        userName = claim.Value;
                    }
                }
                ConnectedUser connectedUser = new ConnectedUser
                {
                    UserIdentifier = Context.UserIdentifier,
                    Name = userName == null ? String.Empty : userName,
                    Connections = new List<Connection> { new Connection { ConnectionID = Context.ConnectionId } }
                };

                connectedUsers.Add(connectedUser);
            }
            else
            {
                user.Connections.Add(new Connection { ConnectionID = Context.ConnectionId });
            }

            // connectedUsers.Add(new )


        }

    }
}
