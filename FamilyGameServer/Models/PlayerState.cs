namespace FamilyGameServer.Models;

public sealed class PlayerState
{
    public required string ConnectionId { get; init; }
    public required string Name { get; init; }

    public int Score { get; set; }
    public int? LastAnswerIndex { get; set; }
    public bool HasAnsweredCurrentQuestion { get; set; }
}
