using System.Collections.Concurrent;
using System.Security.Cryptography;
using FamilyGameServer.Models;

namespace FamilyGameServer.Services;

public sealed class GameService
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public GameRoom CreateRoom(string hostConnectionId)
    {
        var code = GenerateUniqueRoomCode();

        var room = new GameRoom
        {
            Code = code,
            HostConnectionId = hostConnectionId,
        };

        room.Questions.AddRange(GetSampleQuestions());

        if (!_rooms.TryAdd(code, room))
        {
            // Extremely unlikely; retry.
            return CreateRoom(hostConnectionId);
        }

        return room;
    }

    public bool TryGetRoom(string code, out GameRoom room) => _rooms.TryGetValue(code, out room!);

    public bool RemoveRoom(string code) => _rooms.TryRemove(code, out _);

    public bool JoinRoom(string code, string connectionId, string playerName, out string? error)
    {
        error = null;

        if (!_rooms.TryGetValue(code, out var room))
        {
            error = "Room not found";
            return false;
        }

        playerName = playerName.Trim();
        if (playerName.Length is < 1 or > 24)
        {
            error = "Name must be 1-24 characters";
            return false;
        }

        // Prevent duplicate names within a room (case-insensitive).
        if (room.Players.Values.Any(p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase)))
        {
            error = "That name is already taken in this room";
            return false;
        }

        var player = new PlayerState
        {
            ConnectionId = connectionId,
            Name = playerName,
            Score = 0,
        };

        return room.Players.TryAdd(connectionId, player);
    }

    public void LeaveRoom(string connectionId)
    {
        foreach (var kvp in _rooms)
        {
            var room = kvp.Value;
            if (room.HostConnectionId == connectionId)
            {
                // If host disconnects, remove the room.
                _rooms.TryRemove(room.Code, out _);
                continue;
            }

            room.Players.TryRemove(connectionId, out _);
        }
    }

    public bool StartGame(GameRoom room, out QuizQuestion? question)
    {
        lock (room.SyncRoot)
        {
            if (room.Questions.Count == 0)
            {
                question = null;
                return false;
            }

            room.CurrentQuestionIndex = 0;
            question = room.Questions[room.CurrentQuestionIndex];
            BeginQuestion(room, question);
            return true;
        }
    }

    public bool TryGetCurrentQuestion(GameRoom room, out QuizQuestion? question)
    {
        lock (room.SyncRoot)
        {
            if (room.CurrentQuestionIndex < 0 || room.CurrentQuestionIndex >= room.Questions.Count)
            {
                question = null;
                return false;
            }

            question = room.Questions[room.CurrentQuestionIndex];
            return true;
        }
    }

    public bool RevealAnswers(GameRoom room, out int correctIndex, out IReadOnlyList<ScoreboardEntry> scoreboard)
    {
        lock (room.SyncRoot)
        {
            if (!TryGetCurrentQuestion(room, out var question) || question is null)
            {
                correctIndex = -1;
                scoreboard = Array.Empty<ScoreboardEntry>();
                return false;
            }

            if (!room.QuestionActive)
            {
                correctIndex = question.CorrectIndex;
                scoreboard = GetScoreboard(room);
                return true;
            }

            room.QuestionActive = false;
            correctIndex = question.CorrectIndex;

            var startedAt = room.QuestionStartedAtUtc ?? DateTimeOffset.UtcNow;
            var elapsedSeconds = (DateTimeOffset.UtcNow - startedAt).TotalSeconds;
            var remaining = Math.Max(0, question.TimeLimitSeconds - elapsedSeconds);

            foreach (var player in room.Players.Values)
            {
                if (!player.HasAnsweredCurrentQuestion || player.LastAnswerIndex is null)
                {
                    continue;
                }

                if (player.LastAnswerIndex.Value == question.CorrectIndex)
                {
                    // Simple scoring: base + a small time bonus.
                    var timeBonus = (int)Math.Round(remaining * 10);
                    player.Score += 500 + timeBonus;
                }
            }

            scoreboard = GetScoreboard(room);
            return true;
        }
    }

    public bool NextQuestion(GameRoom room, out QuizQuestion? question)
    {
        lock (room.SyncRoot)
        {
            var nextIndex = room.CurrentQuestionIndex + 1;
            if (nextIndex >= room.Questions.Count)
            {
                question = null;
                room.QuestionActive = false;
                return false;
            }

            room.CurrentQuestionIndex = nextIndex;
            question = room.Questions[room.CurrentQuestionIndex];
            BeginQuestion(room, question);
            return true;
        }
    }

    public bool SubmitAnswer(GameRoom room, string connectionId, int answerIndex, out string? error)
    {
        error = null;

        lock (room.SyncRoot)
        {
            if (!room.QuestionActive)
            {
                error = "Question is not active";
                return false;
            }

            if (!TryGetCurrentQuestion(room, out var question) || question is null)
            {
                error = "No current question";
                return false;
            }

            if (!room.Players.TryGetValue(connectionId, out var player))
            {
                error = "Player not in room";
                return false;
            }

            if (player.HasAnsweredCurrentQuestion)
            {
                error = "Already answered";
                return false;
            }

            if (answerIndex < 0 || answerIndex >= question.Choices.Count)
            {
                error = "Invalid answer";
                return false;
            }

            player.LastAnswerIndex = answerIndex;
            player.HasAnsweredCurrentQuestion = true;
            return true;
        }
    }

    public IReadOnlyList<ScoreboardEntry> GetScoreboard(GameRoom room)
    {
        // Snapshot; stable sort.
        var list = room.Players.Values
            .Select(p => new ScoreboardEntry(p.Name, p.Score, p.HasAnsweredCurrentQuestion, p.LastAnswerIndex))
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return list;
    }

    private void BeginQuestion(GameRoom room, QuizQuestion question)
    {
        room.QuestionStartedAtUtc = DateTimeOffset.UtcNow;
        room.QuestionActive = true;

        foreach (var player in room.Players.Values)
        {
            player.HasAnsweredCurrentQuestion = false;
            player.LastAnswerIndex = null;
        }
    }

    private static IReadOnlyList<QuizQuestion> GetSampleQuestions()
    {
        return new List<QuizQuestion>
        {
            new(
                Text: "What year is it today?",
                Choices: new[] { "2024", "2025", "2026", "2027" },
                CorrectIndex: 2,
                TimeLimitSeconds: 15
            ),
            new(
                Text: "Which one is a fruit?",
                Choices: new[] { "Carrot", "Apple", "Celery", "Potato" },
                CorrectIndex: 1,
                TimeLimitSeconds: 12
            ),
            new(
                Text: "2 + 2 = ?",
                Choices: new[] { "3", "4", "5", "22" },
                CorrectIndex: 1,
                TimeLimitSeconds: 10
            ),
        };
    }

    private string GenerateUniqueRoomCode()
    {
        // 4 chars, avoid confusing characters.
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        for (var attempt = 0; attempt < 25; attempt++)
        {
            var code = GenerateCode(alphabet, 4);
            if (!_rooms.ContainsKey(code))
            {
                return code;
            }
        }

        // Fallback: 6 chars.
        return GenerateCode(alphabet, 6);
    }

    private static string GenerateCode(string alphabet, int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);

        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }
}
