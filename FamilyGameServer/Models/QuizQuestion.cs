namespace FamilyGameServer.Models;

public sealed record QuizQuestion(
    string Text,
    IReadOnlyList<string> Choices,
    int CorrectIndex,
    int TimeLimitSeconds
);
