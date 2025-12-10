using System;

namespace MasterServicePlatform.Web.Models.ViewModels
{
    public class ChatConversationViewModel
    {
        public string UserId { get; set; } = "";      // other user id
        public string UserName { get; set; } = "";    // other user name
        public string? LastMessage { get; set; }      // last message text
        public DateTime? LastMessageTime { get; set; } // last message time
        public bool IsLastFromCurrent { get; set; }   // last message from current user
    }
}
