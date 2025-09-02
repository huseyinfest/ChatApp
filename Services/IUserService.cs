using ChatApp.Models;
using ChatApp.Models.DTOs;

namespace ChatApp.Services
{
    public interface IUserService
    {
        Task<UserDto> RegisterAsync(UserRegistrationDto registrationDto);
        Task<string> LoginAsync(UserLoginDto loginDto);
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto> GetUserByEmailAsync(string email); // Bu satır eklendi
        Task<List<UserWithUnreadCountDto>> GetAllUsersAsync(int currentUserId);
        Task<bool> UpdateUserStatusAsync(int userId, bool isOnline);
        Task<bool> UpdateLastSeenAsync(int userId);
        Task<bool> IsUsernameUniqueAsync(string username);
        Task<bool> IsEmailUniqueAsync(string email);
    }
}