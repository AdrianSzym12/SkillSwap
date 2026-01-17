namespace SkillSwap.Domain.Entities.Commons
{
    public class Result<T> where T : class
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public int? StatusCode { get; set; }

    }
}
