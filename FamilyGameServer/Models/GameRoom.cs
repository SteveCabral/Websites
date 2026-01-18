using System.Collections.Concurrent;

namespace FamilyGameServer.Models;

public sealed class GameRoom
{
    public required string Code { get; init; }
    public required string HostConnectionId { get; set; }

    public List<QuizQuestion> Questions { get; } = new();

    public int CurrentQuestionIndex { get; set; } = -1;
    public DateTimeOffset? QuestionStartedAtUtc { get; set; }
    public bool QuestionActive { get; set; }

    public ConcurrentDictionary<string, PlayerState> Players { get; } = new();

    public object SyncRoot { get; } = new();
}
