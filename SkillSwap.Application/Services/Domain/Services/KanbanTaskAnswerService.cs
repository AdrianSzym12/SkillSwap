using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Interfaces;
using DbProfile = SkillSwap.Domain.Entities.Database.Profile;

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

        private async Task<(bool ok, string message, KanbanTask? task, DbProfile? profile)>
            EnsureUserParticipantForTaskAsync(int taskId, int currentUserId, CancellationToken ct)
        {
            var task = await _taskRepository.GetAsync(taskId, ct);
            if (task is null) return (false, "KanbanTask not found", null, null);

            var board = await _boardRepository.GetAsync(task.BoardId, ct);
            if (board is null) return (false, "KanbanBoard not found", null, null);

            var match = await _matchRepository.GetAsync(board.MatchId, ct);
            if (match is null) return (false, "Match not found", null, null);

            var profile = await _profileRepository.GetByUserIdAsync(currentUserId, ct);
            if (profile is null || profile.IsDeleted)
                return (false, "Profile for current user not found", null, null);

            if (match.Profile1Id != profile.Id && match.Profile2Id != profile.Id)
                return (false, "You are not a participant of this match", null, null);

            return (true, string.Empty, task, profile);
        }

        public async Task<Result<KanbanTaskAnswerDTO>> GetAsync(int id, CancellationToken ct)
        {
            try
            {
                var answer = await _answerRepository.GetAsync(id, ct);
                if (answer is null)
                    return new() { IsSuccess = false, Message = "KanbanTaskAnswer not found" };

                // (opcjonalnie) możesz tu dopisać kontrolę dostępu
                // var access = await EnsureUserParticipantForTaskAsync(answer.KanbanTaskId, currentUserId, ct);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanTaskAnswerDTO>(answer),
                    Message = "KanbanTaskAnswer retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error retrieving KanbanTaskAnswer: {ex.Message}" };
            }
        }

        public async Task<Result<List<KanbanTaskAnswerDTO>>> GetByTaskAsync(int taskId, int currentUserId, CancellationToken ct)
        {
            try
            {
                var access = await EnsureUserParticipantForTaskAsync(taskId, currentUserId, ct);
                if (!access.ok)
                    return new() { IsSuccess = false, Message = access.message };

                var answers = await _answerRepository.GetByTaskIdAsync(taskId, ct);
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

        public async Task<Result<KanbanTaskAnswerDTO>> AddAsync(KanbanTaskAnswerCreateDTO dto, int currentUserId, CancellationToken ct)
        {
            try
            {
                var access = await EnsureUserParticipantForTaskAsync(dto.TaskId, currentUserId, ct);
                if (!access.ok)
                    return new() { IsSuccess = false, Message = access.message };

                var entity = new KanbanTaskAnswer
                {
                    // ✅ FK do taska
                    KanbanTaskId = dto.TaskId,

                    // ✅ autor to profil aktualnego usera
                    ProfileId = access.profile!.Id,

                    Content = dto.Content,
                    CreatedAt = DateTime.UtcNow,

                    // ✅ dopiero Verify ustawia te pola
                    CheckerId = null,
                    VerifiedAt = null,

                    IsDeleted = false
                };

                var added = await _answerRepository.AddAsync(entity, ct);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanTaskAnswerDTO>(added),
                    Message = "KanbanTaskAnswer created successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error creating KanbanTaskAnswer: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanTaskAnswerDTO>> UpdateAsync(int id, KanbanTaskAnswerUpdateDTO dto, int currentUserId, CancellationToken ct)
        {
            try
            {
                var answer = await _answerRepository.GetAsync(id, ct);
                if (answer is null)
                    return new() { IsSuccess = false, Message = "KanbanTaskAnswer not found" };

                var access = await EnsureUserParticipantForTaskAsync(answer.KanbanTaskId, currentUserId, ct);
                if (!access.ok)
                    return new() { IsSuccess = false, Message = access.message };

                if (answer.ProfileId != access.profile!.Id)
                    return new() { IsSuccess = false, Message = "You are not the author of this answer" };

                answer.Content = dto.Content;

                var updated = await _answerRepository.UpdateAsync(answer, ct);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanTaskAnswerDTO>(updated),
                    Message = "KanbanTaskAnswer updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error updating KanbanTaskAnswer: {ex.Message}" };
            }
        }

        public async Task<Result<KanbanTaskAnswerDTO>> VerifyAsync(int id, int currentUserId, CancellationToken ct)
        {
            try
            {
                var answer = await _answerRepository.GetAsync(id, ct);
                if (answer is null)
                    return new() { IsSuccess = false, Message = "KanbanTaskAnswer not found" };

                var access = await EnsureUserParticipantForTaskAsync(answer.KanbanTaskId, currentUserId, ct);
                if (!access.ok)
                    return new() { IsSuccess = false, Message = access.message };

                // ✅ nie pozwalaj weryfikować samemu sobie (opcjonalnie)
                if (answer.ProfileId == access.profile!.Id)
                    return new() { IsSuccess = false, Message = "You cannot verify your own answer" };

                answer.CheckerId = currentUserId;
                answer.VerifiedAt = DateTime.UtcNow;

                var updated = await _answerRepository.UpdateAsync(answer, ct);

                return new()
                {
                    IsSuccess = true,
                    Data = _mapper.Map<KanbanTaskAnswerDTO>(updated),
                    Message = "KanbanTaskAnswer verified"
                };
            }
            catch (Exception ex)
            {
                return new() { IsSuccess = false, Message = $"Error verifying KanbanTaskAnswer: {ex.Message}" };
            }
        }

        public async Task<Result<string>> DeleteAsync(int id, int currentUserId, CancellationToken ct)
        {
            try
            {
                var answer = await _answerRepository.GetAsync(id, ct);
                if (answer is null)
                    return new() { IsSuccess = false, Message = "KanbanTaskAnswer not found" };

                var access = await EnsureUserParticipantForTaskAsync(answer.KanbanTaskId, currentUserId, ct);
                if (!access.ok)
                    return new() { IsSuccess = false, Message = access.message };

                // ✅ tylko autor może usuwać
                if (answer.ProfileId != access.profile!.Id)
                    return new() { IsSuccess = false, Message = "You are not the author of this answer" };

                await _answerRepository.DeleteAsync(answer, ct);

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
