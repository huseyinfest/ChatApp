using Microsoft.AspNetCore.SignalR;
using ChatApp.Models.DTOs;

namespace ChatApp.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly Dictionary<string, int> _userConnections = new();

        public async Task JoinChat(int userId)
        {
            var connectionId = Context.ConnectionId;
            _userConnections[connectionId] = userId;

            // Add user to a group for personal messages
            await Groups.AddToGroupAsync(connectionId, $"user_{userId}");

            // Notify other users that this user is online
            await Clients.Others.SendAsync("UserOnline", userId);
        }

        public async Task SendMessage(SendMessageDto messageDto)
        {
            if (_userConnections.TryGetValue(Context.ConnectionId, out var senderId))
            {
                // Send to specific user
                await Clients.Group($"user_{messageDto.ReceiverId}").SendAsync("ReceiveMessage", new MessageDto
                {
                    Content = messageDto.Content,
                    MessageType = messageDto.MessageType,
                    ImageUrl = messageDto.ImageUrl,
                    ImageFileName = messageDto.ImageFileName,
                    SentAt = DateTime.UtcNow,
                    Sender = new UserDto { Id = senderId },
                    Receiver = new UserDto { Id = messageDto.ReceiverId }
                });

                // Send back to sender for confirmation
                await Clients.Caller.SendAsync("MessageSent", messageDto);
            }
        }

        public async Task MarkMessageAsRead(int messageId, int senderId)
        {
            if (_userConnections.TryGetValue(Context.ConnectionId, out var currentUserId))
            {
                // Notify sender that message was read
                await Clients.Group($"user_{senderId}").SendAsync("MessageRead", messageId, currentUserId);
            }
        }

        public async Task Typing(int receiverId)
        {
            if (_userConnections.TryGetValue(Context.ConnectionId, out var senderId))
            {
                await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", senderId);
            }
        }

        public async Task StopTyping(int receiverId)
        {
            if (_userConnections.TryGetValue(Context.ConnectionId, out var senderId))
            {
                await Clients.Group($"user_{receiverId}").SendAsync("UserStoppedTyping", senderId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            if (_userConnections.TryGetValue(connectionId, out var userId))
            {
                _userConnections.Remove(connectionId);

                // Notify other users that this user is offline
                await Clients.Others.SendAsync("UserOffline", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}