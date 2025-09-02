using Microsoft.EntityFrameworkCore;
using ChatApp.Data;
using ChatApp.Models;
using ChatApp.Models.DTOs;

namespace ChatApp.Services
{
    public class MessageService : IMessageService
    {
        private readonly ChatDbContext _context;
        
        public MessageService(ChatDbContext context)
        {
            _context = context;
        }
        
        public async Task<MessageDto> SendMessageAsync(int senderId, SendMessageDto messageDto)
        {
            var message = new Message
            {
                Content = messageDto.Content,
                SenderId = senderId,
                ReceiverId = messageDto.ReceiverId,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            
            // Get sender and receiver info
            var sender = await _context.Users.FindAsync(senderId);
            var receiver = await _context.Users.FindAsync(messageDto.ReceiverId);
            
            return new MessageDto
            {
                Id = message.Id,
                Content = message.Content,
                SentAt = message.SentAt,
                IsRead = message.IsRead,
                ReadAt = message.ReadAt,
                Sender = new UserDto
                {
                    Id = sender!.Id,
                    Username = sender.Username,
                    Email = sender.Email,
                    IsOnline = sender.IsOnline,
                    LastSeen = sender.LastSeen,
                    CreatedAt = sender.CreatedAt
                },
                Receiver = new UserDto
                {
                    Id = receiver!.Id,
                    Username = receiver.Username,
                    Email = receiver.Email,
                    IsOnline = receiver.IsOnline,
                    LastSeen = receiver.LastSeen,
                    CreatedAt = receiver.CreatedAt
                }
            };
        }
        
        public async Task<List<MessageDto>> GetConversationAsync(int userId1, int userId2)
        {
            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                           (m.SenderId == userId2 && m.ReceiverId == userId1))
                .OrderBy(m => m.SentAt)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .ToListAsync();
                
            return messages.Select(m => new MessageDto
            {
                Id = m.Id,
                Content = m.Content,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                ReadAt = m.ReadAt,
                Sender = new UserDto
                {
                    Id = m.Sender.Id,
                    Username = m.Sender.Username,
                    Email = m.Sender.Email,
                    IsOnline = m.Sender.IsOnline,
                    LastSeen = m.Sender.LastSeen,
                    CreatedAt = m.Sender.CreatedAt
                },
                Receiver = new UserDto
                {
                    Id = m.Receiver.Id,
                    Username = m.Receiver.Username,
                    Email = m.Receiver.Email,
                    IsOnline = m.Receiver.IsOnline,
                    LastSeen = m.Receiver.LastSeen,
                    CreatedAt = m.Receiver.CreatedAt
                }
            }).ToList();
        }
        
        public async Task<bool> MarkMessageAsReadAsync(int messageId, int userId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId);
                
            if (message == null) return false;
            
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        public async Task<int> GetUnreadMessageCountAsync(int userId, int senderId)
        {
            return await _context.Messages
                .Where(m => m.ReceiverId == userId && m.SenderId == senderId && !m.IsRead)
                .CountAsync();
        }
        
        public async Task<List<ConversationDto>> GetUserConversationsAsync(int userId)
        {
            var conversations = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.SentAt).First(),
                    UnreadCount = g.Where(m => m.ReceiverId == userId && !m.IsRead).Count()
                })
                .ToListAsync();
                
            var result = new List<ConversationDto>();
            
            foreach (var conv in conversations)
            {
                var otherUser = await _context.Users.FindAsync(conv.OtherUserId);
                if (otherUser != null)
                {
                    var messages = await GetConversationAsync(userId, conv.OtherUserId);
                    
                    result.Add(new ConversationDto
                    {
                        OtherUser = new UserDto
                        {
                            Id = otherUser.Id,
                            Username = otherUser.Username,
                            Email = otherUser.Email,
                            IsOnline = otherUser.IsOnline,
                            LastSeen = otherUser.LastSeen,
                            CreatedAt = otherUser.CreatedAt
                        },
                        Messages = messages,
                        UnreadCount = conv.UnreadCount
                    });
                }
            }
            
            return result.OrderByDescending(c => c.Messages.LastOrDefault()?.SentAt ?? DateTime.MinValue).ToList();
        }
    }
}
