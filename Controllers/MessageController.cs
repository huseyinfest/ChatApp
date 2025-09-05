using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChatApp.Models.DTOs;
using ChatApp.Services;
using ChatApp.Models;

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
        
        [HttpPost("upload-image")]
        public async Task<ActionResult<object>> UploadImage([FromForm] IFormFile image, [FromForm] int receiverId)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (senderId == 0)
                return Unauthorized();
                
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "No image file provided" });
                
            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
                return BadRequest(new { message = "Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed." });
                
            // Validate file size (max 5MB)
            if (image.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File size too large. Maximum 5MB allowed." });
                
            try
            {
                // Generate unique filename
                var fileExtension = Path.GetExtension(image.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var uploadsPath = Path.Combine("wwwroot", "uploads", "images");
                var filePath = Path.Combine(uploadsPath, fileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(uploadsPath);
                
                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                
                // Create image URL
                var imageUrl = $"/uploads/images/{fileName}";
                
                // Create message with image
                var messageDto = new SendMessageDto
                {
                    ReceiverId = receiverId,
                    Content = "", // Empty content for image messages
                    MessageType = MessageType.Image,
                    ImageUrl = imageUrl,
                    ImageFileName = image.FileName
                };
                
                var message = await _messageService.SendMessageAsync(senderId, messageDto);
                return Ok(new { message, imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error uploading image: {ex.Message}" });
            }
        }
    }
}
