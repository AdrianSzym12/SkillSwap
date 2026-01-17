using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;
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

        private async Task<(bool ok, string message, KanbanBoard? board)> EnsureUserParticipantForBoardAsync(
            int boardId, int currentUserId, CancellationToken ct)
        {
            var board = await _boardRepository.GetAsync(boardId, ct);
            if (board is null) return (false, "KanbanBoard not found", null);

            var match = await _matchRepository.GetAsync(board.MatchId, ct);
            if (match is null) return (false, "Match not found", null);

            var profile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
            if (profile is null || profile.IsDeleted) return (false, "Profile for current user not found", null);

            if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                return (false, "You are not a participant of this match", null);

            return (true, string.Empty, board);
        }

        public async Task<Result<KanbanTaskDTO>> GetAsync(int id, CancellationToken ct)
        {
            try
            {
                var task = await _taskRepository.GetAsync(id, ct);
                if (task is null) return new() { IsSuccess = false, Message = "KanbanTask not found" };

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanTaskDTO>(task),
                    Message = "KanbanTask retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanTask: {ex.Message}" };
            }
        }

        public async Task<Result<List<KanbanTaskDTO>>> GetAsync(CancellationToken ct)
        {
            try
            {
                var tasks = await _taskRepository.GetAsync(ct);
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

        public async Task<Result<List<KanbanTaskDTO>>> GetByBoardAsync(int boardId, int currentUserId, CancellationToken ct)
        {
            try
            {
                var (ok, message, _) = await EnsureUserParticipantForBoardAsync(boardId, currentUserId, ct);
                if (!ok) return new() { IsSuccess = false, Message = message };

                var tasks = await _taskRepository.GetByBoardIdAsync(boardId, ct);
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

        public async Task<Result<KanbanTaskDTO>> AddAsync(KanbanTaskCreateDTO dto, int currentUserId, CancellationToken ct)
        {
            try
            {
                var (ok, message, _) = await EnsureUserParticipantForBoardAsync(dto.BoardId, currentUserId, ct);
                if (!ok) return new() { IsSuccess = false, Message = message };

                var entity = new KanbanTask
                {
                    BoardId = dto.BoardId,
                    AssignedId = dto.AssignedId ?? 0, // jeśli masz int, a nie nullable
                    Title = dto.Title,
                    Description = dto.Description,
                    Status = KanbanTaskStatus.In_Progress,    // albo default enum
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = null,
                    IsDeleted = false
                };

                var added = await _taskRepository.AddAsync(entity, ct);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanTaskDTO>(added),
                    Message = "KanbanTask created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating KanbanTask: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanTaskDTO>> UpdateAsync(int id, KanbanTaskUpdateDTO dto, int currentUserId, CancellationToken ct)
        {
            try
            {
                var task = await _taskRepository.GetAsync(id, ct);
                if (task is null) return new() { IsSuccess = false, Message = "KanbanTask not found" };

                var (ok, message, _) = await EnsureUserParticipantForBoardAsync(task.BoardId, currentUserId, ct);
                if (!ok) return new() { IsSuccess = false, Message = message };

                task.Title = dto.Title;
                task.Description = dto.Description;
                task.Status = dto.Status;
                task.AssignedId = dto.AssignedId ?? task.AssignedId;
                task.CompletedAt = dto.CompletedAt;

                var updated = await _taskRepository.UpdateAsync(task, ct);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanTaskDTO>(updated),
                    Message = "KanbanTask updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error updating KanbanTask: {ex.Message}" };
            }
        }

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId, CancellationToken ct)
        {
            try
            {
                var task = await _taskRepository.GetAsync(id, ct);
                if (task is null) return new() { IsSuccess = false, Message = "KanbanTask not found" };

                var (ok, message, _) = await EnsureUserParticipantForBoardAsync(task.BoardId, currentUserId, ct);
                if (!ok) return new() { IsSuccess = false, Message = message };

                await _taskRepository.DeleteAsync(task, ct);

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
