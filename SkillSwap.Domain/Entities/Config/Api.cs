namespace SkillSwap.Domain.Entities.Config
{
    public class Api
    {
        public string? Server { get; set; }
        public string? Database { get; set; }
        public int Port { get; set; }
        public string? User { get; set; }
        public string? Password { get; set; }
        public string? SecretKey { get; set; }
    }
}