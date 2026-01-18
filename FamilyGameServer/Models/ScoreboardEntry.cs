namespace FamilyGameServer.Models;

public sealed record ScoreboardEntry(
    string Name,
    int Score,
    bool Answered,
    int? LastAnswerIndex
);
