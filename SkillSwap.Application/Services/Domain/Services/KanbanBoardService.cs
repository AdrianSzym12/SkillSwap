using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class KanbanBoardService : IKanbanBoardService
    {
        private readonly IKanbanBoardRepository _boardRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IMapper _mapper;

        public KanbanBoardService(
            IKanbanBoardRepository boardRepository,
            IMatchRepository matchRepository,
            IProfileRepository profileRepository,
            IMapper mapper)
        {
            _boardRepository = boardRepository;
            _matchRepository = matchRepository;
            _profileRepository = profileRepository;
            _mapper = mapper;
        }

        public async Task<Result<KanbanBoardDTO>> GetAsync(int id, CancellationToken ct)
        {
            try
            {
                var board = await _boardRepository.GetAsync(id, ct);
                if (board is null)
                    return new() { IsSuccess = false, Message = "KanbanBoard not found" };

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanBoardDTO>(board),
                    Message = "KanbanBoard retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanBoard: {ex.Message}" };
            }
        }

       

        public async Task<Result<List<KanbanBoardDTO>>> GetByMatchAsync(int matchId, int currentUserId, CancellationToken ct)
        {
            try
            {
                var match = await _matchRepository.GetAsync(matchId, ct);
                if (match is null)
                    return new() { IsSuccess = false, Message = "Match not found" };

                var profile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                    return new() { IsSuccess = false, Message = "You are not a participant of this match" };

                var boards = await _boardRepository.GetByMatchIdAsync(matchId, ct);
                var dtos = boards.Select(b => _mapper.Map<KanbanBoardDTO>(b)).ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "KanbanBoards retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanBoards: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanBoardDTO>> AddAsync(KanbanBoardCreateDTO dto, int currentUserId, CancellationToken ct)
        {
            try
            {
                var match = await _matchRepository.GetAsync(dto.MatchId, ct);
                if (match is null)
                    return new() { IsSuccess = false, Message = "Match not found" };

                var profile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                    return new() { IsSuccess = false, Message = "You are not a participant of this match" };

                var entity = new KanbanBoard
                {
                    MatchId = dto.MatchId,
                    AuthorId = currentUserId, // trzymasz jako UserId (spójne z JWT)
                    Title = dto.Title,
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                var added = await _boardRepository.AddAsync(entity, ct);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanBoardDTO>(added),
                    Message = "KanbanBoard created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating KanbanBoard: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanBoardDTO>> UpdateAsync(int id, KanbanBoardUpdateDTO dto, int currentUserId, CancellationToken ct)
        {
            try
            {
                var board = await _boardRepository.GetAsync(id, ct);
                if (board is null)
                    return new() { IsSuccess = false, Message = "KanbanBoard not found" };

                if (board.AuthorId != currentUserId)
                    return new() { IsSuccess = false, Message = "You are not the author of this board" };

                board.Title = dto.Title;
                board.Description = dto.Description;

                var updated = await _boardRepository.UpdateAsync(board, ct);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanBoardDTO>(updated),
                    Message = "KanbanBoard updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error updating KanbanBoard: {ex.Message}" };
            }
        }


        public async Task<Result<string>> DeleteAsync(int id, int currentUserId, CancellationToken ct)
        {
            try
            {
                var board = await _boardRepository.GetAsync(id, ct);
                if (board is null)
                    return new() { IsSuccess = false, Message = "KanbanBoard not found" };

                if (board.AuthorId != currentUserId)
                    return new() { IsSuccess = false, Message = "You are not the author of this board" };

                await _boardRepository.DeleteAsync(board, ct);

                return new()
                {
                    IsSuccess = true,
                    Data = "KanbanBoard deleted",
                    Message = "KanbanBoard soft-deleted"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error deleting KanbanBoard: {ex.Message}" };
            }
        }
    }
}
