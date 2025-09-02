using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChatApp.Models.DTOs;
using ChatApp.Services;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        
        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }
        
        [HttpPost("send")]
        public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageDto messageDto)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (senderId == 0)
                return Unauthorized();
                
            try
            {
                var message = await _messageService.SendMessageAsync(senderId, messageDto);
                return Ok(message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpGet("conversation/{otherUserId}")]
        public async Task<ActionResult<List<MessageDto>>> GetConversation(int otherUserId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized();
                
            var messages = await _messageService.GetConversationAsync(currentUserId, otherUserId);
            return Ok(messages);
        }
        
        [HttpPost("read/{messageId}")]
        public async Task<ActionResult> MarkMessageAsRead(int messageId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized();
                
            var success = await _messageService.MarkMessageAsReadAsync(messageId, currentUserId);
            if (!success)
                return NotFound();
                
            return Ok(new { message = "Message marked as read" });
        }
        
        [HttpGet("unread-count/{senderId}")]
        public async Task<ActionResult<object>> GetUnreadCount(int senderId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized();
                
            var count = await _messageService.GetUnreadMessageCountAsync(currentUserId, senderId);
            return Ok(new { unreadCount = count });
        }
        
        [HttpGet("conversations")]
        public async Task<ActionResult<List<ConversationDto>>> GetConversations()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized();
                
            var conversations = await _messageService.GetUserConversationsAsync(currentUserId);
            return Ok(conversations);
        }
    }
}
