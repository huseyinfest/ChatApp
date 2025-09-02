namespace ChatApp.Models.DTOs
{
    public class SendMessageDto
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
    
    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public UserDto Sender { get; set; } = null!;
        public UserDto Receiver { get; set; } = null!;
    }
    
    public class ConversationDto
    {
        public UserDto OtherUser { get; set; } = null!;
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public int UnreadCount { get; set; }
    }
}
