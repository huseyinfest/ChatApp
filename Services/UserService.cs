using ChatApp.Data;
using ChatApp.Models;
using ChatApp.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace ChatApp.Services
{
    public class UserService : IUserService
    {
        private readonly ChatDbContext _context;
        private readonly IConfiguration _configuration;

        public UserService(ChatDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        
        public async Task<UserDto> RegisterAsync(UserRegistrationDto registrationDto)
        {
            if (await IsEmailUniqueAsync(registrationDto.Email) == false)
            {
                throw new InvalidOperationException("Email already taken.");
            }
            
            var user = new User
            {
                Username = registrationDto.Username,
                Email = registrationDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registrationDto.Password),
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };
        }
        
        public async Task<string> LoginAsync(UserLoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new InvalidOperationException("Invalid email or password.");
            }
            
            await UpdateUserStatusAsync(user.Id, true);
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        
        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
                return null;
            
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen
            };
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
                return null;
            
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen
            };
        }
        
        public async Task<List<UserWithUnreadCountDto>> GetAllUsersAsync(int currentUserId)
        {
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id != currentUserId)
                .Select(u => new UserWithUnreadCountDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsOnline = u.IsOnline,
                    LastSeen = u.LastSeen,
                    UnreadMessageCount = _context.Messages
                        .Count(m => m.ReceiverId == currentUserId && m.SenderId == u.Id && m.IsRead == false)
                })
                .ToListAsync();
            
            return users;
        }
        
        public async Task<bool> UpdateUserStatusAsync(int userId, bool isOnline)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;
                
            user.IsOnline = isOnline;
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> UpdateLastSeenAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;
                
            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUsernameUniqueAsync(string username)
        {
            return await _context.Users.AsNoTracking().AllAsync(u => u.Username != username);
        }
        
        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            return await _context.Users.AsNoTracking().AllAsync(u => u.Email != email);
        }
    }
}