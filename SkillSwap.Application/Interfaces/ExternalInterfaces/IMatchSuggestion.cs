using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.Application.Interfaces.ExternalInterfaces
{
    public interface IMatchSuggestion
    {
        Task<Result<List<MatchSuggestionDTO>>> GetSuggestionsAsync(int currentUserId, int limit = 20);
    }
}
