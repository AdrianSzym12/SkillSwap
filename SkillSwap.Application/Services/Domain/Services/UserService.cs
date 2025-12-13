using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<Result<UserDTO>> AddAsync(UserDTO userDTO)
        {
            try
            {
                var user = _mapper.Map<User>(userDTO);
                user.PasswordHash = HashPassword(userDTO.Password);
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userRepository.AddAsync(user);
                var mapped = _mapper.Map<UserDTO>(result);
                mapped.Password = null;

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "User created successfully"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error creating user: {ex.Message}"
                };
            }
        }

        public async Task<Result<UserDTO>> GetAsync(int id)
        {
            try
            {
                var user = await _userRepository.GetAsync(id);
                if (user is null)
                    return new()
                    {
                        IsSuccess = false,
                        Message = "User not found"
                    };

                var mapped = _mapper.Map<UserDTO>(user);
                mapped.Password = null;

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "User retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error retrieving user: {ex.Message}"
                };
            }
        }

        public async Task<Result<UserDTO>> UpdateAsync(UserDTO userDTO)
        {
            try
            {
                var user = await _userRepository.GetAsync(userDTO.Id);
                if (user is null) 
                    return new()
                    {
                        IsSuccess = false,
                        Message = "User not found"
                    };

                user.Email = userDTO.Email;
                if (!string.IsNullOrEmpty(userDTO.Password))
                    user.PasswordHash = HashPassword(userDTO.Password);

                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userRepository.UpdateAsync(user);
                var mapped = _mapper.Map<UserDTO>(result);
                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "User updated successfully"
                };
            }
            catch(Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error updating user: {ex.Message}"
                };
            }
        }
        public async Task<Result<string>> DeleteAsync(int id, int currentUserId)
        {
            try
            {
                if (id != currentUserId)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "You can delete only your own account"
                    };
                }

                var user = await _userRepository.GetAsync(id);
                if (user is null || user.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "User not found"
                    };
                }

                await _userRepository.DeleteAsync(user);

                return new()
                {
                    IsSuccess = true,
                    Data = "User deleted successfully",
                    Message = "Account soft-deleted"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error deleting user: {ex.Message}"
                };
            }
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
