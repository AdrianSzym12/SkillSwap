using Microsoft.IdentityModel.Tokens;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Config;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;
using SkillSwap.Domain.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class SessionService : ISessionService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly Configuration _config;

        public SessionService(
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            ISessionRepository sessionRepository,
            Configuration config)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _profileRepository = profileRepository;
            _config = config;
        }

        public async Task<Result<LoginResultDTO>> LoginAsync(LoginDTO dto)
        {
            try
            {
                var email = dto.Email.Trim().ToLowerInvariant();
                var user = await _userRepository.GetByEmailAsync(email);

                if (user is null || user.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = 401,
                        Message = "Invalid email or password"
                    };
                }

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                {
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = 401,
                        Message = "Invalid email or password"
                    };
                }

                var expires = DateTime.UtcNow.AddHours(1);
                var tokenString = GenerateJwtToken(user, expires);

                var session = new Session
                {
                    UserId = user.Id,
                    JwtToken = tokenString,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expires,
                    IsRevoked = false
                };

                await _sessionRepository.AddAsync(session);

                return new()
                {
                    IsSuccess = true,
                    Data = new LoginResultDTO
                    {
                        Token = tokenString,
                        ExpiresAt = expires,
                        UserId = user.Id
                    },
                    Message = "Login successful"
                };
            }
            catch
            {
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = "Error during login"
                };
            }
        }

        public async Task<Result<string>> LogoutAsync(string token)
        {
            try
            {
                var session = await _sessionRepository.GetByTokenAsync(token);
                if (session is null)
                {
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Message = "Session not found"
                    };
                }

                session.IsRevoked = true;
                await _sessionRepository.UpdateAsync(session);

                return new()
                {
                    IsSuccess = true,
                    Data = "Logged out",
                    Message = "Logged out"
                };
            }
            catch
            {
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = "Error during logout"
                };
            }
        }

        public async Task<Result<CurrentSessionDTO>> GetCurrentAsync(string token)
        {
            try
            {
                var session = await _sessionRepository.GetByTokenAsync(token);
                if (session is null || session.IsRevoked)
                {
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Message = "Session not found"
                    };
                }

                if (session.ExpiresAt <= DateTime.UtcNow)
                {
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = 401,
                        Message = "Session expired"
                    };
                }

                if (!ValidateJwtToken(token))
                {
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = 401,
                        Message = "Invalid token"
                    };
                }

                var user = await _userRepository.GetAsync(session.UserId);
                if (user is null)
                {
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Message = "User not found"
                    };
                }

                var dto = new CurrentSessionDTO
                {
                    SessionId = session.Id,
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = session.CreatedAt,
                    ExpiresAt = session.ExpiresAt,
                    IsRevoked = session.IsRevoked
                };

                return new()
                {
                    IsSuccess = true,
                    Data = dto,
                    Message = "Current session"
                };
            }
            catch
            {
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = "Error getting current session"
                };
            }
        }

        public async Task<Result<RegisterResponseDTO>> RegisterAsync(RegisterDTO dto)
        {
            try
            {
                var email = dto.Email.Trim().ToLowerInvariant();

                var existing = await _userRepository.GetByEmailAsync(email);
                if (existing is not null)
                {
                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = 409,
                        Message = "Email is already taken."
                    };
                }

                var now = DateTime.UtcNow;

                var user = new User
                {
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _userRepository.AddAsync(user);

                var defaultName = BuildDefaultUserName(user);
                var uniqueUserName = await GenerateUniqueUserNameAsync(defaultName);

                var profile = new Profile
                {
                    User = user,
                    UserName = uniqueUserName,
                    Avatar = Array.Empty<byte>(),
                    Bio = "null",
                    Country = "null",
                    Language = "null",
                    PreferredMeetingType = SkillSwap.Domain.Enums.MeetingType.None,
                    PreferredLearningStyle = SkillSwap.Domain.Enums.LearningStyle.None,
                    Availability = SkillSwap.Domain.Enums.AvailabilitySlot.None,
                    IsDeleted = false
                };

                var (completion, missingFields, isComplete) = ProfileStartDataPolicy.Evaluate(profile);
                profile.ProfileCompletion = completion;
                profile.IsOnboardingComplete = isComplete;

                await _profileRepository.AddAsync(profile);

                var expires = DateTime.UtcNow.AddHours(1);
                var tokenString = GenerateJwtToken(user, expires);

                var session = new Session
                {
                    UserId = user.Id,
                    JwtToken = tokenString,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expires,
                    IsRevoked = false
                };

                await _sessionRepository.AddAsync(session);

                return new()
                {
                    IsSuccess = true,
                    Data = new RegisterResponseDTO
                    {
                        UserId = user.Id,
                        ProfileId = profile.Id,
                        AccessToken = tokenString,
                        RequiresOnboarding = !isComplete,
                        ProfileCompletion = completion,
                        MissingFields = missingFields
                    },
                    Message = !isComplete
                        ? "Account created. Profile needs onboarding."
                        : "Account created."
                };
            }
            catch
            {
                return new()
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = "Registration failed"
                };
            }
        }

        private bool ValidateJwtToken(string token)
        {
            var secretKey = _config.Api.SecretKey;
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateJwtToken(User user, DateTime expires)
        {
            var secretKey = _config.Api.SecretKey;
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);

            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", $"{user.FirstName} {user.LastName}")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string BuildDefaultUserName(User user)
        {
            var candidate = $"{user.FirstName}.{user.LastName}".Replace(" ", "");
            return string.IsNullOrWhiteSpace(candidate) ? user.Email.Split('@')[0] : candidate;
        }

        private async Task<string> GenerateUniqueUserNameAsync(string baseName)
        {
            baseName = NormalizeUserName(baseName);

            if (!await _profileRepository.ExistsByUserNameAsync(baseName))
                return baseName;

            for (int i = 1; i <= 9999; i++)
            {
                var candidate = $"{baseName}{i}";
                if (!await _profileRepository.ExistsByUserNameAsync(candidate))
                    return candidate;
            }

            return $"{baseName}{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        private static string NormalizeUserName(string userName)
        {
            userName = userName.Trim();
            userName = userName.Replace(" ", "");

            if (userName.Length > 50)
                userName = userName.Substring(0, 50);

            if (string.IsNullOrWhiteSpace(userName))
                userName = "user";

            return userName;
        }
    }
}
