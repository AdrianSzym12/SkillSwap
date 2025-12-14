using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces
{
    public interface IMatchSuggestionService
    {
        Task<Result<List<MatchSuggestionDTO>>> GetSuggestionsAsync(int currentUserId, int limit = 20);
    }
}
