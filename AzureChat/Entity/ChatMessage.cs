using System;

namespace AzureChat.Entity
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SenderId { get; set; }
        public string? ReceiverId { get; set; }

        public User Sender { get; set; }  
        public User Receiver { get; set; }  

        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
