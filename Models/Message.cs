using System.ComponentModel.DataAnnotations;

namespace ChatApp.Models
{
    public enum MessageType
    {
        Text = 0,
        Image = 1
    }

    public class Message
    {
        public int Id { get; set; }
        
        public string Content { get; set; } = string.Empty;
        
        public MessageType MessageType { get; set; } = MessageType.Text;
        
        public string? ImageUrl { get; set; }
        
        public string? ImageFileName { get; set; }
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        public bool IsRead { get; set; } = false;
        
        public DateTime? ReadAt { get; set; }
        
        // Foreign keys
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        
        // Navigation properties
        public virtual User Sender { get; set; } = null!;
        public virtual User Receiver { get; set; } = null!;
    }
}
