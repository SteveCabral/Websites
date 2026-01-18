# Family Game Server

This is a minimal ASP.NET Core 8 web server project named **FamilyGameServer**.

It now includes a small Kahoot-style sample game (host + players) using SignalR so you can test joining from another computer or phone on your network.

## Project Structure

```
FamilyGameServer
├── Controllers
│   └── HomeController.cs
├── Properties
│   └── launchSettings.json
├── appsettings.json
├── FamilyGameServer.csproj
├── Program.cs
└── README.md
```

## Getting Started

To run the project, follow these steps:

1. Ensure you have the .NET SDK installed on your machine.
2. Navigate to the project directory.
3. Run the application using the command:

   ```
   dotnet run
   ```

4. Open a browser to:

    - Host UI: `http://localhost:5000/host`
    - Player UI: `http://localhost:5000/play`

    The host page creates a room code automatically. Players join with that code.

### LAN testing (phone / another PC)

This project is configured to listen on all interfaces on port 5000 in the dev profile.

- Start it on the machine running the server.
- From a phone/other PC on the same network, open:

   - `http://<server-ip>:5000/host`
   - `http://<server-ip>:5000/play`

Example: `http://192.168.1.208:5000/play`

Note: this sample runs over HTTP (no HTTPS redirect) to keep LAN testing simple.

## Deploy to another machine (example: 192.168.1.208)

### Option A: Run as a console app (Kestrel)

On your dev machine:

```
dotnet publish .\FamilyGameServer.csproj -c Release -o .\out
```

Copy the `out` folder to `192.168.1.208` (any location).

On `192.168.1.208`, run from that folder:

```
FamilyGameServer.exe --urls http://0.0.0.0:5000
```

Then browse from any device on your LAN:

- `http://192.168.1.208:5000/host`
- `http://192.168.1.208:5000/play`

### Option B: Install as a Windows Service (auto-start)

1. Publish on your dev PC:

   ```
   .\deploy\publish.ps1
   ```

2. Copy the published output folder to the server (example target folder):

   - `C:\Apps\FamilyGameServer`

3. On `192.168.1.208`, open PowerShell as Administrator and run:

   ```
   .\deploy\install-service.ps1 -AppDir "C:\Apps\FamilyGameServer" -Urls "http://0.0.0.0:5000"
   ```

4. Verify:

   - `http://192.168.1.208:5000/host`
   - `http://192.168.1.208:5000/play`

If you ever need to remove it:

```
.\deploy\uninstall-service.ps1
```

### Firewall note

If you can reach the server from localhost but not from other devices, add an inbound rule for TCP 5000 on `192.168.1.208`.

## Project Files

- **Program.cs**: Entry point of the application, sets up the web host and configures the HTTP request pipeline.
- **Hubs/GameHub.cs**: SignalR hub for rooms, questions, and answers.
- **Services/GameService.cs**: In-memory game state (rooms, players, sample questions).
- **wwwroot/host/index.html**: Host console UI.
- **wwwroot/play/index.html**: Player UI.
- **Controllers/HomeController.cs**: Still present (not used by the game UI).
- **Properties/launchSettings.json**: Configuration for launching the application, including environment variables and application URL.
- **appsettings.json**: Application configuration settings.
- **FamilyGameServer.csproj**: Project file defining dependencies and build settings.

## License

This project is licensed under the MIT License.