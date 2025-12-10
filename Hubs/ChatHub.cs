using Microsoft.AspNetCore.SignalR;
using MasterServicePlatform.Web.Models;
using System.Threading.Tasks;

namespace MasterServicePlatform.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string receiverId, string message)
        {
            var senderId = Context.UserIdentifier;

            // save message to DB
            var msg = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = message
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            // now msg.Id is generated
            int messageId = msg.Id;

            // send to receiver
            await Clients.User(receiverId)
                .SendAsync("ReceiveMessage", senderId, message, messageId);

            // send back to sender
            await Clients.User(senderId)
                .SendAsync("ReceiveMessage", senderId, message, messageId);
            await Clients.User(receiverId)
                .SendAsync("NewMessageNotification", senderId);

            await RefreshDialogs(senderId);
            await RefreshDialogs(receiverId);
        }

        public async Task RefreshDialogs(string userId)
        {
            await Clients.User(userId).SendAsync("RefreshDialogs");
        }

    }
}
