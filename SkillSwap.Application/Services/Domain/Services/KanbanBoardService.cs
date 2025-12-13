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

        public async Task<Result<KanbanBoardDTO>> GetAsync(int id)
        {
            try
            {
                var board = await _boardRepository.GetAsync(id);
                if (board is null)
                    return new() { IsSuccess = false, Message = "KanbanBoard not found" };

                var dto = _mapper.Map<KanbanBoardDTO>(board);
                return new()
                {
                    IsSuccess = true,
                    Data = dto,
                    Message = "KanbanBoard retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanBoard: {ex.Message}" };
            }
        }

        public async Task<Result<List<KanbanBoardDTO>>> GetAsync()
        {
            try
            {
                var boards = await _boardRepository.GetAsync();
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

        public async Task<Result<List<KanbanBoardDTO>>> GetByMatchAsync(int matchId, int currentUserId)
        {
            try
            {
                var match = await _matchRepository.GetAsync(matchId);
                if (match is null)
                    return new() { IsSuccess = false, Message = "Match not found" };

                var profile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                    return new() { IsSuccess = false, Message = "You are not a participant of this match" };

                var boards = await _boardRepository.GetAsync();
                var filtered = boards.Where(b => b.MatchId == matchId).ToList();
                var dtos = filtered.Select(b => _mapper.Map<KanbanBoardDTO>(b)).ToList();

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

        public async Task<Result<KanbanBoardDTO>> AddAsync(KanbanBoardDTO dto, int currentUserId)
        {
            try
            {
                if (dto.match == null || dto.match.Id <= 0)
                    return new() { IsSuccess = false, Message = "Match is required" };

                var match = await _matchRepository.GetAsync(dto.match.Id);
                if (match is null)
                    return new() { IsSuccess = false, Message = "Match not found" };

                var profile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                    return new() { IsSuccess = false, Message = "You are not a participant of this match" };

                var entity = _mapper.Map<KanbanBoard>(dto);
                entity.MatchId = match.Id;
                entity.AuthorId = currentUserId;
                entity.CreatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;

                var added = await _boardRepository.AddAsync(entity);
                var mapped = _mapper.Map<KanbanBoardDTO>(added);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "KanbanBoard created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating KanbanBoard: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanBoardDTO>> UpdateAsync(KanbanBoardDTO dto, int currentUserId)
        {
            try
            {
                var board = await _boardRepository.GetAsync(dto.Id);
                if (board is null)
                    return new() { IsSuccess = false, Message = "KanbanBoard not found" };

                if (board.AuthorId != currentUserId)
                    return new() { IsSuccess = false, Message = "You are not the author of this board" };

                board.Title = dto.Title;
                board.Description = dto.Description;

                var updated = await _boardRepository.UpdateAsync(board);
                var mapped = _mapper.Map<KanbanBoardDTO>(updated);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "KanbanBoard updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error updating KanbanBoard: {ex.Message}" };
            }
        }

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId)
        {
            try
            {
                var board = await _boardRepository.GetAsync(id);
                if (board is null)
                    return new() { IsSuccess = false, Message = "KanbanBoard not found" };

                if (board.AuthorId != currentUserId)
                    return new() { IsSuccess = false, Message = "You are not the author of this board" };

                await _boardRepository.DeleteAsync(board); 

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
