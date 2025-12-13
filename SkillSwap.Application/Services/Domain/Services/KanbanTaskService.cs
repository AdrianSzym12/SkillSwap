using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class KanbanTaskService : IKanbanTaskService
    {
        private readonly IKanbanTaskRepository _taskRepository;
        private readonly IKanbanBoardRepository _boardRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IMapper _mapper;

        public KanbanTaskService(
            IKanbanTaskRepository taskRepository,
            IKanbanBoardRepository boardRepository,
            IMatchRepository matchRepository,
            IProfileRepository profileRepository,
            IMapper mapper)
        {
            _taskRepository = taskRepository;
            _boardRepository = boardRepository;
            _matchRepository = matchRepository;
            _profileRepository = profileRepository;
            _mapper = mapper;
        }

        private async Task<(bool ok, string message)> EnsureUserParticipantForBoardAsync(int boardId, int currentUserId)
        {
            var board = await _boardRepository.GetAsync(boardId);
            if (board is null)
                return (false, "KanbanBoard not found");

            var match = await _matchRepository.GetAsync(board.MatchId);
            if (match is null)
                return (false, "Match not found");

            var profile = await _profileRepository.GetByUserIdAsync(currentUserId);
            if (profile is null || profile.IsDeleted)
                return (false, "Profile for current user not found");

            if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                return (false, "You are not a participant of this match");

            return (true, string.Empty);
        }

        public async Task<Result<KanbanTaskDTO>> GetAsync(int id)
        {
            try
            {
                var task = await _taskRepository.GetAsync(id);
                if (task is null)
                    return new() { IsSuccess = false, Message = "KanbanTask not found" };

                var dto = _mapper.Map<KanbanTaskDTO>(task);
                return new()
                {
                    IsSuccess = true,
                    Data = dto,
                    Message = "KanbanTask retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanTask: {ex.Message}" };
            }
        }

        public async Task<Result<List<KanbanTaskDTO>>> GetAsync()
        {
            try
            {
                var tasks = await _taskRepository.GetAsync();
                var dtos = tasks.Select(t => _mapper.Map<KanbanTaskDTO>(t)).ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "KanbanTasks retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanTasks: {ex.Message}" };
            }
        }

        public async Task<Result<List<KanbanTaskDTO>>> GetByBoardAsync(int boardId, int currentUserId)
        {
            try
            {
                var (ok, message) = await EnsureUserParticipantForBoardAsync(boardId, currentUserId);
                if (!ok)
                    return new() { IsSuccess = false, Message = message };

                var tasks = await _taskRepository.GetAsync();
                var filtered = tasks.Where(t => t.BoardId == boardId).ToList();
                var dtos = filtered.Select(t => _mapper.Map<KanbanTaskDTO>(t)).ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "KanbanTasks retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanTasks: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanTaskDTO>> AddAsync(KanbanTaskDTO dto, int currentUserId)
        {
            try
            {
                if (dto.kanbanBoard == null || dto.kanbanBoard.Id <= 0)
                    return new() { IsSuccess = false, Message = "KanbanBoard is required" };

                var (ok, message) = await EnsureUserParticipantForBoardAsync(dto.kanbanBoard.Id, currentUserId);
                if (!ok)
                    return new() { IsSuccess = false, Message = message };

                var entity = _mapper.Map<KanbanTask>(dto);
                entity.BoardId = dto.kanbanBoard.Id;
                entity.CreatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;

                var added = await _taskRepository.AddAsync(entity);
                var mapped = _mapper.Map<KanbanTaskDTO>(added);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "KanbanTask created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating KanbanTask: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanTaskDTO>> UpdateAsync(KanbanTaskDTO dto, int currentUserId)
        {
            try
            {
                var task = await _taskRepository.GetAsync(dto.Id);
                if (task is null)
                    return new() { IsSuccess = false, Message = "KanbanTask not found" };

                var (ok, message) = await EnsureUserParticipantForBoardAsync(task.BoardId, currentUserId);
                if (!ok)
                    return new() { IsSuccess = false, Message = message };

                task.Title = dto.Title;
                task.Description = dto.Description;
                task.Status = dto.Status;
                task.AssignedId = dto.AssignedId;
                task.CompletedAt = dto.CompletedAt;

                var updated = await _taskRepository.UpdateAsync(task);
                var mapped = _mapper.Map<KanbanTaskDTO>(updated);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "KanbanTask updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error updating KanbanTask: {ex.Message}" };
            }
        }

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId)
        {
            try
            {
                var task = await _taskRepository.GetAsync(id);
                if (task is null)
                    return new() { IsSuccess = false, Message = "KanbanTask not found" };

                var (ok, message) = await EnsureUserParticipantForBoardAsync(task.BoardId, currentUserId);
                if (!ok)
                    return new() { IsSuccess = false, Message = message };

                await _taskRepository.DeleteAsync(task); 

                return new()
                {
                    IsSuccess = true,
                    Data = "KanbanTask deleted",
                    Message = "KanbanTask soft-deleted"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error deleting KanbanTask: {ex.Message}" };
            }
        }
    }
}
