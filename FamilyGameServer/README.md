# Family Game Server

This is an ASP.NET Core 8 web server project named **FamilyGameServer**. It's a real-time, multiplayer trivia game platform built with SignalR for seamless communication between the host (quiz master) and players.

**Features:**
- Real-time multiplayer gameplay using SignalR WebSocket connections
- Host and Player UIs for managing quizzes and answering questions
- In-memory game state with support for multiple concurrent game rooms
- Automatic room cleanup when the host disconnects
- Configurable deployment to any IP/port (including Windows Service installation)
- LAN and remote access via HTTP

## Project Structure

```
FamilyGameServer
├── Controllers/
│   └── HomeController.cs                 # Basic HTTP endpoint (fallback)
├── Models/                               # Data structures
│   ├── GameRoom.cs                       # Quiz room state & players
│   ├── PlayerState.cs                    # Individual player progress
│   ├── QuizQuestion.cs                   # Question definition
│   └── ScoreboardEntry.cs                # Leaderboard entry
├── Services/
│   └── GameService.cs                    # Game logic & state management
├── Hubs/
│   └── GameHub.cs                        # SignalR hub for real-time communication
├── Properties/
│   └── launchSettings.json               # Development launch profiles
├── wwwroot/                              # Static files
│   ├── host/
│   │   └── index.html                    # Host UI (quiz master)
│   ├── play/
│   │   └── index.html                    # Player UI
│   ├── shared.js                         # Shared client-side logic
│   └── site.css                          # Styling
├── deploy/                               # Deployment scripts
│   ├── publish.ps1                       # Build and publish script
│   ├── install-service.ps1               # Windows Service installation
│   ├── uninstall-service.ps1             # Service removal
│   └── run-console.ps1                   # Console runner
├── appsettings.json                      # App configuration
├── FamilyGameServer.csproj               # Project file
├── FamilyGameServer.sln                  # Solution file
├── Program.cs                            # Entry point & middleware setup
├── .gitignore                            # Git ignore rules
└── README.md                             # This file
```

## Getting Started

To run the project locally, follow these steps:

1. Ensure you have the .NET SDK installed on your machine.
2. Navigate to the project directory.
3. Run the application:

   ```
   dotnet run
   ```

4. Open a browser to:

    - **Host UI**: `http://localhost:5000/host`
    - **Player UI**: `http://localhost:5000/play`

    The host page creates a room code automatically. Players join with that code to participate.

### LAN Testing (Phone / Another PC)

This project is configured to listen on `192.168.1.208:5000` (or modify in Program.cs).

- Start the server on the machine running the application.
- From a phone/other PC on the same network, open:

   - `http://<server-ip>:5000/host`
   - `http://<server-ip>:5000/play`

**Example**: `http://192.168.1.208:5000/play`

**Note**: This sample runs over HTTP (no HTTPS redirect) to keep LAN testing simple.

## Deployment

### Option A: Publish as a Console App (Kestrel)

On your dev machine:

```powershell
dotnet publish -c Release
```

Copy the output folder (`bin\Release\net8.0\publish`) to the target machine (e.g., `192.168.1.208`).

On `192.168.1.208`, run from that folder:

```powershell
.\FamilyGameServer.exe
```

Then browse from any device on your LAN:

- `http://192.168.1.208:5000/host`
- `http://192.168.1.208:5000/play`

### Option B: Install as a Windows Service (auto-start)

1. Publish on your dev PC:

   ```powershell
   .\deploy\publish.ps1
   ```

2. Copy the published output folder to the server (e.g., `C:\Apps\FamilyGameServer`).

3. On `192.168.1.208`, open PowerShell as Administrator and run:

   ```powershell
   .\deploy\install-service.ps1 -AppDir "C:\Apps\FamilyGameServer" -Urls "http://192.168.1.208:5000"
   ```

4. Verify the service is running:

   ```powershell
   Get-Service FamilyGameServer
   ```

5. Access the application:

   - `http://192.168.1.208:5000/host`
   - `http://192.168.1.208:5000/play`

To remove the service:

```powershell
.\deploy\uninstall-service.ps1
```

### Firewall Note

If you can reach the server from localhost but not from other devices, add an inbound rule for TCP 5000 on `192.168.1.208`:

```powershell
# On the server machine, as Administrator:
netsh advfirewall firewall add rule name="FamilyGameServer" dir=in action=allow protocol=tcp localport=5000
```

## Technical Architecture

This section explains the key components and how they interact to create the multiplayer game experience.

### Core Components

#### **Program.cs** - Application Startup
- Configures ASP.NET Core services (MVC, SignalR)
- Sets up Windows Service hosting (for deployment as a service)
- Configures Kestrel to listen on `192.168.1.208:5000`
- Maps the SignalR hub to `/gameHub`
- Configures static file serving (wwwroot)
- Registers middleware for routing, authorization, and error handling

#### **Models** - Data Structures

**GameRoom.cs**
- Represents a single quiz game instance
- Contains a unique 4-6 character alphanumeric code for joining
- Stores the host's SignalR connection ID
- Maintains a list of questions for the quiz
- Tracks current question index and whether a question is active
- Holds all players in the room (concurrent dictionary for thread safety)
- Includes a `SyncRoot` object for synchronizing state changes

**PlayerState.cs**
- Represents an individual player in a room
- Tracks the player's name and SignalR connection ID
- Stores the player's current score
- Records the player's last answer and whether they answered the current question

**QuizQuestion.cs**
- Immutable record defining a quiz question
- Contains question text, multiple choice answers, correct answer index, and time limit

**ScoreboardEntry.cs**
- Immutable record for displaying a player on the leaderboard
- Contains name, score, and answer status for the current question

#### **Services** - Business Logic

**GameService.cs** - In-Memory Game State Management
- Manages all active game rooms using a `ConcurrentDictionary`
- **Room Management**:
  - `CreateRoom()`: Generates a unique 4-6 character code and initializes a new game room
  - `JoinRoom()`: Adds a player to a room with validation (name length, uniqueness, room exists)
  - `LeaveRoom()`: Removes a player; if host leaves, the entire room is destroyed
  - `RemoveRoom()`: Explicitly deletes a room after game ends

- **Game Flow**:
  - `StartGame()`: Initializes the first question
  - `SubmitAnswer()`: Records a player's answer with thread-safe locking
  - `RevealAnswers()`: Calculates scores based on correctness and time remaining
  - `NextQuestion()`: Advances to the next question or signals game end

- **Scoring**:
  - Correct answer: 500 base points + time bonus (10 points per second remaining)
  - Incorrect answer: 0 points

- **Data Access**:
  - `TryGetRoom()`: Safely retrieves a room
  - `GetScoreboard()`: Returns players sorted by score (descending) then name (ascending)

#### **Hubs** - SignalR Hub

**GameHub.cs** - Real-Time Communication Gateway
- Extends `Hub` for SignalR WebSocket communication
- **Host Methods** (called from host UI):
  - `CreateRoom()`: Creates a new game and returns a room code
  - `StartGame()`: Begins the quiz with the first question
  - `RevealAnswers()`: Shows the correct answer and updates scores
  - `NextQuestion()`: Advances to the next question or ends the game
  - `EndRoom()`: Manually terminates the game room

- **Player Methods** (called from player UI):
  - `JoinRoom()`: Joins an existing room with a player name
  - `SubmitAnswer()`: Records the player's answer to the current question

- **Lifecycle**:
  - `OnDisconnectedAsync()`: Automatically handles player/host disconnection

- **Broadcasting**:
  - Uses SignalR Groups (one group per room code) to broadcast updates to all connected clients
  - `SendAsync()` to caller only for personal confirmations
  - `Clients.Group()` to notify all participants of game state changes

**SignalR Events Sent to Clients:**
- `RoomCreated` - Returns the new room code to the host
- `JoinedRoom` - Confirms successful join to the player
- `QuestionStarted` - Broadcasts the current question to all players
- `PlayerListUpdated` - Updates the leaderboard for all clients
- `AnswerAccepted` - Confirms the player's answer was recorded
- `RoundEnded` - Reveals the correct answer and new scores
- `GameEnded` - Sends final scores and ends the game

#### **Static Files** - Frontend

**wwwroot/host/index.html**
- HTML/CSS/JavaScript for the quiz master interface
- Features: room creation, question display, answer reveal, next question controls, final score display
- Communicates via SignalR to control the game flow

**wwwroot/play/index.html**
- HTML/CSS/JavaScript for the player interface
- Features: room code entry, player name input, question display with timer, answer submission, scoreboard
- Real-time updates as other players answer and scores change

**wwwroot/shared.js**
- Shared client-side utilities for both host and player UIs
- Handles SignalR connection management
- Utility functions for DOM manipulation and event handling

**wwwroot/site.css**
- Styling for both interfaces

#### **Controllers** - HTTP Fallback

**HomeController.cs**
- Provides a basic HTTP endpoint returning "Hello World"
- Serves as a fallback for non-game routes
- Not actively used by the game UI, which relies entirely on static files and SignalR

### Data Flow Architecture

1. **Room Creation** (Host)
   - Host calls `GameHub.CreateRoom()`
   - `GameService.CreateRoom()` generates a unique code and initializes game state
   - Room code is returned to host and displayed

2. **Player Joining** (Players)
   - Player enters room code and name
   - Calls `GameHub.JoinRoom(roomCode, playerName)`
   - `GameService.JoinRoom()` validates and adds the player
   - All clients in the room receive updated scoreboard via `PlayerListUpdated` event

3. **Question Delivery** (Host → Players)
   - Host clicks "Start Game"
   - Calls `GameHub.StartGame()` → `GameService.StartGame()`
   - First question is queued; all clients receive via `QuestionStarted` event
   - Players see question text, answer choices, and countdown timer

4. **Answer Submission** (Player → Server)
   - Player selects an answer before time expires
   - Calls `GameHub.SubmitAnswer(roomCode, answerIndex)`
   - `GameService.SubmitAnswer()` records the answer in `PlayerState`
   - Server broadcasts updated scoreboard (showing who answered) via `PlayerListUpdated`

5. **Answer Reveal** (Host → Players)
   - Host clicks "Reveal Answers"
   - Calls `GameHub.RevealAnswers()`
   - `GameService.RevealAnswers()` calculates points and updates scores
   - All clients receive correct answer and new scores via `RoundEnded` + `PlayerListUpdated`

6. **Next Question** (Host → Server → Players)
   - Host clicks "Next Question"
   - Calls `GameHub.NextQuestion()`
   - Repeats from step 3, or if no questions remain, triggers `GameEnded`

### Thread Safety

- **GameRoom** uses `SyncRoot` lock for synchronized access to question state and player answers
- **ConcurrentDictionary** used for room and player collections (thread-safe without explicit locks)
- **PlayerState** updates are protected by GameRoom's lock to prevent race conditions

### In-Memory State Management

All game data is stored in application memory:
- Game rooms exist only while the server is running
- Stopping the server will lose all active games
- For production use, consider adding persistence (database, file storage)

### Extensibility Points

Future enhancements could include:
- Database persistence for game history
- Custom question management (database-backed)
- Admin panel for managing questions
- Player authentication and stats tracking
- Customizable scoring algorithms
- Question categories and difficulty levels
- Timer extensions and hint system

## Project Files Summary

| File | Purpose |
|------|---------|
| **Program.cs** | Entry point, middleware setup, service registration |
| **GameHub.cs** | SignalR hub handling real-time client communication |
| **GameService.cs** | Core game logic, room and player state management |
| **Models/*.cs** | Data structures (GameRoom, PlayerState, Question, Scoreboard) |
| **Controllers/HomeController.cs** | Fallback HTTP endpoint |
| **wwwroot/** | Static HTML, CSS, and JavaScript for frontend |
| **deploy/*.ps1** | PowerShell scripts for publishing and service installation |
| **appsettings.json** | Application configuration |
| **FamilyGameServer.csproj** | Project dependencies and build settings |

## Dependencies

- **Microsoft.AspNetCore.Mvc.NewtonsoftJson** - JSON serialization for API responses
- **Microsoft.Extensions.DependencyInjection** - Dependency injection framework
- Built-in: SignalR, ASP.NET Core, Kestrel web server

## Learning Resources

As a beginner web developer, this project is designed as a learning tool. Check out the documentation in the `docs/` folder:

- **[DeveloperLearningGuide.md](docs/DeveloperLearningGuide.md)** - Essential explanations for web development concepts, configuration decisions, and best practices learned from this project. Start here for understanding WHY certain choices were made.

This guide covers topics like:
- Network binding and the `0.0.0.0` wildcard address
- Localhost vs. network IPs
- Firewall configuration
- Port selection
- And more!

## License

This project is licensed under the MIT License.
