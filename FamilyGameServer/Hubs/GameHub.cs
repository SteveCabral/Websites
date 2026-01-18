using FamilyGameServer.Models;
using FamilyGameServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace FamilyGameServer.Hubs;

public sealed class GameHub(GameService gameService) : Hub
{
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        gameService.LeaveRoom(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task<string> CreateRoom()
    {
        var room = gameService.CreateRoom(Context.ConnectionId);

        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);
        await Clients.Caller.SendAsync("RoomCreated", room.Code);

        return room.Code;
    }

    public async Task<object> JoinRoom(string roomCode, string playerName)
    {
        roomCode = (roomCode ?? string.Empty).Trim().ToUpperInvariant();
        playerName = playerName ?? string.Empty;

        if (!gameService.JoinRoom(roomCode, Context.ConnectionId, playerName, out var error))
        {
            return new { ok = false, error = error ?? "Unable to join" };
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

        if (gameService.TryGetRoom(roomCode, out var room))
        {
            await Clients.Group(roomCode).SendAsync("PlayerListUpdated", gameService.GetScoreboard(room));
        }

        await Clients.Caller.SendAsync("JoinedRoom", roomCode);
        return new { ok = true };
    }

    public async Task<object> StartGame(string roomCode)
    {
        roomCode = (roomCode ?? string.Empty).Trim().ToUpperInvariant();

        if (!gameService.TryGetRoom(roomCode, out var room))
        {
            return new { ok = false, error = "Room not found" };
        }

        if (!IsHost(room))
        {
            return new { ok = false, error = "Only host can start" };
        }

        if (!gameService.StartGame(room, out var question) || question is null)
        {
            return new { ok = false, error = "No questions" };
        }

        await BroadcastQuestion(room, question);
        return new { ok = true };
    }

    public async Task<object> SubmitAnswer(string roomCode, int answerIndex)
    {
        roomCode = (roomCode ?? string.Empty).Trim().ToUpperInvariant();

        if (!gameService.TryGetRoom(roomCode, out var room))
        {
            return new { ok = false, error = "Room not found" };
        }

        if (!gameService.SubmitAnswer(room, Context.ConnectionId, answerIndex, out var error))
        {
            return new { ok = false, error = error ?? "Unable to submit" };
        }

        await Clients.Group(roomCode).SendAsync("PlayerListUpdated", gameService.GetScoreboard(room));
        await Clients.Caller.SendAsync("AnswerAccepted");

        return new { ok = true };
    }

    public async Task<object> RevealAnswers(string roomCode)
    {
        roomCode = (roomCode ?? string.Empty).Trim().ToUpperInvariant();

        if (!gameService.TryGetRoom(roomCode, out var room))
        {
            return new { ok = false, error = "Room not found" };
        }

        if (!IsHost(room))
        {
            return new { ok = false, error = "Only host can reveal" };
        }

        if (!gameService.RevealAnswers(room, out var correctIndex, out var scoreboard))
        {
            return new { ok = false, error = "No current question" };
        }

        await Clients.Group(roomCode).SendAsync("RoundEnded", new { correctIndex, scoreboard });
        await Clients.Group(roomCode).SendAsync("PlayerListUpdated", scoreboard);

        return new { ok = true };
    }

    public async Task<object> NextQuestion(string roomCode)
    {
        roomCode = (roomCode ?? string.Empty).Trim().ToUpperInvariant();

        if (!gameService.TryGetRoom(roomCode, out var room))
        {
            return new { ok = false, error = "Room not found" };
        }

        if (!IsHost(room))
        {
            return new { ok = false, error = "Only host can advance" };
        }

        if (!gameService.NextQuestion(room, out var question) || question is null)
        {
            var finalScores = gameService.GetScoreboard(room);
            await Clients.Group(roomCode).SendAsync("GameEnded", new { scoreboard = finalScores });
            return new { ok = true, done = true };
        }

        await BroadcastQuestion(room, question);
        return new { ok = true };
    }

    public async Task<object> EndRoom(string roomCode)
    {
        roomCode = (roomCode ?? string.Empty).Trim().ToUpperInvariant();

        if (!gameService.TryGetRoom(roomCode, out var room))
        {
            return new { ok = false, error = "Room not found" };
        }

        if (!IsHost(room))
        {
            return new { ok = false, error = "Only host can end" };
        }

        var finalScores = gameService.GetScoreboard(room);
        await Clients.Group(roomCode).SendAsync("GameEnded", new { scoreboard = finalScores });

        gameService.RemoveRoom(roomCode);
        return new { ok = true };
    }

    private bool IsHost(GameRoom room) => string.Equals(room.HostConnectionId, Context.ConnectionId, StringComparison.Ordinal);

    private Task BroadcastQuestion(GameRoom room, QuizQuestion question)
    {
        var payload = new
        {
            text = question.Text,
            choices = question.Choices,
            timeLimitSeconds = question.TimeLimitSeconds,
            questionNumber = room.CurrentQuestionIndex + 1,
            totalQuestions = room.Questions.Count,
        };

        return Clients.Group(room.Code).SendAsync("QuestionStarted", payload);
    }
}
