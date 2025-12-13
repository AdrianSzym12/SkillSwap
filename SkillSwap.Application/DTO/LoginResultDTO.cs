
namespace SkillSwap.Application.DTO
{
    public class LoginResultDTO
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int UserId { get; set; }
    }
}
