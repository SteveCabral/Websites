# Family Game Server

This is a minimal ASP.NET Core 8 web server project named **FamilyGameServer**. The project serves a basic "Hello World" website for testing purposes.

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

4. Open your web browser and navigate to `http://localhost:5000` to see the "Hello World" message.

## Project Files

- **Program.cs**: Entry point of the application, sets up the web host and configures the HTTP request pipeline.
- **Controllers/HomeController.cs**: Contains the `HomeController` class with an `Index` method that returns a "Hello World" message.
- **Properties/launchSettings.json**: Configuration for launching the application, including environment variables and application URL.
- **appsettings.json**: Application configuration settings.
- **FamilyGameServer.csproj**: Project file defining dependencies and build settings.

## License

This project is licensed under the MIT License.