using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ChatApp.Models.DTOs;
using ChatApp.Services;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
        
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] UserRegistrationDto registrationDto)
        {
            try
            {
                var user = await _userService.RegisterAsync(registrationDto);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpPost("login")]
        public async Task<ActionResult<object>> Login([FromBody] UserLoginDto loginDto)
        {
            try
            {
                var token = await _userService.LoginAsync(loginDto);
                // Kullanıcıyı e-posta ile bul ve ID'sini al
                var user = await _userService.GetUserByEmailAsync(loginDto.Email);
                
                // Dönen yanıta ID'yi ekle
                return Ok(new { token, id = user.Id, message = "Login successful" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();
                
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();
                
            return Ok(user);
        }
        
        [HttpGet("users")]
        [Authorize]
        public async Task<ActionResult<List<UserWithUnreadCountDto>>> GetAllUsers()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
                return Unauthorized();
                
            var users = await _userService.GetAllUsersAsync(currentUserId);
            return Ok(users);
        }
        
        [HttpGet("check-username/{username}")]
        public async Task<ActionResult<object>> CheckUsername(string username)
        {
            var isUnique = await _userService.IsUsernameUniqueAsync(username);
            return Ok(new { isUnique, message = isUnique ? "Username available" : "Username already taken" });
        }
        
        [HttpGet("check-email/{email}")]
        public async Task<ActionResult<object>> CheckEmail(string email)
        {
            var isUnique = await _userService.IsEmailUniqueAsync(email);
            return Ok(new { isUnique, message = isUnique ? "Email available" : "Email already taken" });
        }
        
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();
                
            await _userService.UpdateUserStatusAsync(userId, false);
            return Ok(new { message = "Logout successful" });
        }
    }
}