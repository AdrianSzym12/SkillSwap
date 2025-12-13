using System.ComponentModel.DataAnnotations;

namespace SkillSwap.Application.DTO
{
    public class RegisterDTO
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; }

        [Required, MaxLength(100)]
        public string LastName { get; set; }

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; }

        [Required, MinLength(6), MaxLength(100)]
        public string Password { get; set; }
    }
}
