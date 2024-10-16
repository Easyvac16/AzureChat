using System.ComponentModel.DataAnnotations;

namespace AzureChat.Entity
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required] 
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public ICollection<ChatMessage> SentMessages { get; set; } 
        public ICollection<ChatMessage> ReceivedMessages { get; set; } 
    }
}
