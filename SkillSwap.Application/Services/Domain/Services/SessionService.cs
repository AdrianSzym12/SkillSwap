using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Config;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;
using SkillSwap.Domain.Services;
using SkillSwap.Persistence;
using SkillSwap.Persistence.Repositories;
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
        private readonly PersistenceContext _context;
        private readonly Configuration _config;

        public SessionService(
            IUserRepository userRepository,
            IProfileRepository profileRepository,
            ISessionRepository sessionRepository,
            PersistenceContext context,
            Configuration config)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _profileRepository = profileRepository;
            _config = config;
            _context = context;
        }

        public async Task<Result<LoginResultDTO>> LoginAsync(LoginDTO dto)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(dto.Email);
                if (user is null)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password"
                    };
                }

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password"
                    };
                }

                var secretKey = _config.Api.SecretKey;
                var keyBytes = Encoding.UTF8.GetBytes(secretKey);
                var securityKey = new SymmetricSecurityKey(keyBytes);

                var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddHours(1);

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
                var tokenString = tokenHandler.WriteToken(token);

                var session = new Session
                {
                    UserId = user.Id,
                    JwtToken = tokenString,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expires,
                    IsRevoked = false
                };

                await _sessionRepository.AddAsync(session);

                var loginResult = new LoginResultDTO
                {
                    Token = tokenString,
                    ExpiresAt = expires,
                    UserId = user.Id
                };
                if (user.IsDeleted)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Account is deleted"
                    };
                }

                return new()
                {
                    IsSuccess = true,
                    Data = loginResult,
                    Message = "Login successful"
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error during login: {ex.Message}"
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
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error during logout: {ex.Message}"
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
                        Message = "Session not found"
                    };
                }

                if (session.ExpiresAt <= DateTime.UtcNow)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Session expired"
                    };
                }

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
                    }, out SecurityToken validatedToken);

                }
                catch (SecurityTokenExpiredException)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Token expired"
                    };
                }
                catch (Exception)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Invalid token"
                    };
                }

                var user = await _userRepository.GetAsync(session.UserId);
                if (user is null)
                {
                    return new()
                    {
                        IsSuccess = false,
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
            catch (Exception ex)
            {
                return new()
                {
                    IsSuccess = false,
                    Message = $"Error getting current session: {ex.Message}"
                };
            }
        }


        public async Task<Result<RegisterResponseDTO>> RegisterAsync(RegisterDTO dto)
        {
            // tracking etapów do message
            var userCreated = false;
            var profileCreated = false;
            var sessionCreated = false;

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var email = dto.Email.Trim().ToLowerInvariant();

                var existing = await _userRepository.GetByEmailAsync(email);
                if (existing is not null)
                {
                    return new()
                    {
                        IsSuccess = false,
                        Message = "Registration failed: email is already taken."
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
                userCreated = true;
                var defaultName = BuildDefaultUserName(user);
                var uniqueUserName = await GenerateUniqueUserNameAsync(defaultName);

                var profile = new Profile
                {
                    User = user,
                    UserName = uniqueUserName,

                    // jeśli DB wymaga NOT NULL:
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
                profileCreated = true;

                var expires = DateTime.UtcNow.AddHours(1);
                var tokenString = GenerateJwtToken(user, profile.Id, expires);

                var session = new Session
                {
                    UserId = user.Id,
                    JwtToken = tokenString,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expires,
                    IsRevoked = false
                };

                await _sessionRepository.AddAsync(session);
                sessionCreated = true;

                // jeden zapis na końcu (jeśli Twoje repo AddAsync nie robi SaveChanges)
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                var data = new RegisterResponseDTO
                {
                    UserId = user.Id,
                    ProfileId = profile.Id,
                    AccessToken = tokenString,
                    RequiresOnboarding = !isComplete,
                    ProfileCompletion = completion,
                    MissingFields = missingFields
                };

                return new()
                {
                    IsSuccess = true,
                    Data = data,
                    Message = !isComplete
                        ? "Account created. Profile needs onboarding."
                        : "Account created."
                };
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();

                var inner = ex.InnerException?.Message ?? ex.Message;

                // komunikat etapowy (po fixie idealnie zawsze "nothing was created" bo rollback)
                var stageMsg = GetRegistrationStageMessage(userCreated, profileCreated, sessionCreated);

                return new()
                {
                    IsSuccess = false,
                    Message = $"Registration failed: {stageMsg} Database error: {inner}"
                };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                var stageMsg = GetRegistrationStageMessage(userCreated, profileCreated, sessionCreated);

                return new()
                {
                    IsSuccess = false,
                    Message = $"Registration failed: {stageMsg} Error: {ex.Message}"
                };
            }
        }

        private static string GetRegistrationStageMessage(bool userCreated, bool profileCreated, bool sessionCreated)
        {
            // przy rollback realnie nic nie powinno zostać, ale message jest informacyjny
            if (!userCreated) return "user was not created.";
            if (userCreated && !profileCreated) return "user was created but profile was not created.";
            if (userCreated && profileCreated && !sessionCreated) return "user and profile were created but session was not created.";
            return "unknown stage.";
        }
        private string GenerateJwtToken(User user, int profileId, DateTime expires)
        {
            var secretKey = _config.Api.SecretKey;
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);

            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", $"{user.FirstName} {user.LastName}")
            };

            if (profileId > 0)
                claims.Add(new Claim("profileId", profileId.ToString()));

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
            // Prosty i przewidywalny default (możesz później zrobić unikalność)
            var candidate = $"{user.FirstName}.{user.LastName}".Replace(" ", "");
            return string.IsNullOrWhiteSpace(candidate) ? user.Email.Split('@')[0] : candidate;
        }
        private async Task<string> GenerateUniqueUserNameAsync(string baseName)
        {
            baseName = NormalizeUserName(baseName);

            // 1) bez suffixu
            if (!await _profileRepository.ExistsByUserNameAsync(baseName))
                return baseName;

            // 2) dopinanie liczby
            for (int i = 1; i <= 9999; i++)
            {
                var candidate = $"{baseName}{i}";
                if (!await _profileRepository.ExistsByUserNameAsync(candidate))
                    return candidate;
            }

            // 3) awaryjnie timestamp
            return $"{baseName}{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        private static string NormalizeUserName(string userName)
        {
            userName = userName.Trim();

            // zamień spacje na nic / kropkę — jak wolisz
            userName = userName.Replace(" ", "");

            // limit długości (masz MaxLength(50) w DTO)
            if (userName.Length > 50)
                userName = userName.Substring(0, 50);

            // fallback
            if (string.IsNullOrWhiteSpace(userName))
                userName = "user";

            return userName;
        }

    }
}
