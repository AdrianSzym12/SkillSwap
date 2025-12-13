using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application.Services.Domain.Services
{
    public class KanbanTaskAnswerService : IKanbanTaskAnswerService
    {
        private readonly IKanbanTaskAnswerRepository _answerRepository;
        private readonly IKanbanTaskRepository _taskRepository;
        private readonly IKanbanBoardRepository _boardRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IMapper _mapper;

        public KanbanTaskAnswerService(
            IKanbanTaskAnswerRepository answerRepository,
            IKanbanTaskRepository taskRepository,
            IKanbanBoardRepository boardRepository,
            IMatchRepository matchRepository,
            IProfileRepository profileRepository,
            IMapper mapper)
        {
            _answerRepository = answerRepository;
            _taskRepository = taskRepository;
            _boardRepository = boardRepository;
            _matchRepository = matchRepository;
            _profileRepository = profileRepository;
            _mapper = mapper;
        }

        private async Task<(bool ok, string message)> EnsureUserParticipantForTaskAsync(int taskId, int currentUserId)
        {
            var task = await _taskRepository.GetAsync(taskId);
            if (task is null)
                return (false, "KanbanTask not found");

            var board = await _boardRepository.GetAsync(task.BoardId);
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

        public async Task<Result<KanbanTaskAnswerDTO>> GetAsync(int id)
        {
            try
            {
                var answer = await _answerRepository.GetAsync(id);
                if (answer is null)
                    return new() { IsSuccess = false, Message = "KanbanTaskAnswer not found" };

                var dto = _mapper.Map<KanbanTaskAnswerDTO>(answer);
                return new()
                {
                    IsSuccess = true,
                    Data = dto,
                    Message = "KanbanTaskAnswer retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanTaskAnswer: {ex.Message}" };
            }
        }

        public async Task<Result<List<KanbanTaskAnswerDTO>>> GetAsync()
        {
            try
            {
                var answers = await _answerRepository.GetAsync();
                var dtos = answers.Select(a => _mapper.Map<KanbanTaskAnswerDTO>(a)).ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "KanbanTaskAnswers retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanTaskAnswers: {ex.Message}" };
            }
        }

        public async Task<Result<List<KanbanTaskAnswerDTO>>> GetByTaskAsync(int taskId, int currentUserId)
        {
            try
            {
                var (ok, message) = await EnsureUserParticipantForTaskAsync(taskId, currentUserId);
                if (!ok)
                    return new() { IsSuccess = false, Message = message };

                var answers = await _answerRepository.GetAsync();
                var filtered = answers.Where(a => a.kanbanTask != null && a.kanbanTask.Id == taskId).ToList();
                var dtos = filtered.Select(a => _mapper.Map<KanbanTaskAnswerDTO>(a)).ToList();

                return new()
                {
                    IsSuccess = true,
                    Data = dtos,
                    Message = "KanbanTaskAnswers retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanTaskAnswers: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanTaskAnswerDTO>> AddAsync(KanbanTaskAnswerDTO dto, int currentUserId)
        {
            try
            {
                if (dto.kanbanTask == null || dto.kanbanTask.Id <= 0)
                    return new() { IsSuccess = false, Message = "KanbanTask is required" };

                var (ok, message) = await EnsureUserParticipantForTaskAsync(dto.kanbanTask.Id, currentUserId);
                if (!ok)
                    return new() { IsSuccess = false, Message = message };

                var profile = await _profileRepository.GetByUserIdAsync(currentUserId);
                if (profile is null || profile.IsDeleted)
                    return new() { IsSuccess = false, Message = "Profile for current user not found" };

                var task = await _taskRepository.GetAsync(dto.kanbanTask.Id);
                if (task is null)
                    return new() { IsSuccess = false, Message = "KanbanTask not found" };

                var entity = _mapper.Map<KanbanTaskAnswer>(dto);
                entity.ProfileId = profile.Id;
                entity.kanbanTask = task;
                entity.CreatedAt = DateTime.UtcNow;
                entity.IsDeleted = false;

                var added = await _answerRepository.AddAsync(entity);
                var mapped = _mapper.Map<KanbanTaskAnswerDTO>(added);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "KanbanTaskAnswer created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating KanbanTaskAnswer: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanTaskAnswerDTO>> UpdateAsync(KanbanTaskAnswerDTO dto, int currentUserId)
        {
            try
            {
                var answer = await _answerRepository.GetAsync(dto.Id);
                if (answer is null)
                    return new() { IsSuccess = false, Message = "KanbanTaskAnswer not found" };

                var profile = await _profileRepository.GetAsync(answer.ProfileId);
                if (profile is null)
                    return new() { IsSuccess = false, Message = "Profile not found" };

                bool isOwner = profile.UserId == currentUserId;
                bool isChecker = answer.CheckerId == currentUserId;

                if (!isOwner && !isChecker)
                    return new() { IsSuccess = false, Message = "You are not allowed to update this answer" };

                if (isOwner)
                {
                    answer.Content = dto.Content;
                }

                if (isChecker)
                {
                    answer.VerifiedAt = dto.VerifiedAt ?? DateTime.UtcNow;
                }

                var updated = await _answerRepository.UpdateAsync(answer);
                var mapped = _mapper.Map<KanbanTaskAnswerDTO>(updated);

                return new()
                {
                    IsSuccess = true,
                    Data = mapped,
                    Message = "KanbanTaskAnswer updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error updating KanbanTaskAnswer: {ex.Message}" };
            }
        }

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId)
        {
            try
            {
                var answer = await _answerRepository.GetAsync(id);
                if (answer is null)
                    return new() { IsSuccess = false, Message = "KanbanTaskAnswer not found" };

                var profile = await _profileRepository.GetAsync(answer.ProfileId);
                if (profile is null)
                    return new() { IsSuccess = false, Message = "Profile not found" };

                bool isOwner = profile.UserId == currentUserId;
                bool isChecker = answer.CheckerId == currentUserId;

                if (!isOwner && !isChecker)
                    return new() { IsSuccess = false, Message = "You are not allowed to delete this answer" };

                await _answerRepository.DeleteAsync(answer); 

                return new()
                {
                    IsSuccess = true,
                    Data = "KanbanTaskAnswer deleted",
                    Message = "KanbanTaskAnswer soft-deleted"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error deleting KanbanTaskAnswer: {ex.Message}" };
            }
        }
    }
}
