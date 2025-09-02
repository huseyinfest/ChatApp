using ChatApp.Models.DTOs;

namespace ChatApp.Services
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessageAsync(int senderId, SendMessageDto messageDto);
        Task<List<MessageDto>> GetConversationAsync(int userId1, int userId2);
        Task<bool> MarkMessageAsReadAsync(int messageId, int userId);
        Task<int> GetUnreadMessageCountAsync(int userId, int senderId);
        Task<List<ConversationDto>> GetUserConversationsAsync(int userId);
    }
}
