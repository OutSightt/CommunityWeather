# Community Weather Monitoring and Disaster Information System
**Project Development Increment 1: User Accounts and Authentication**

ASP.NET Core MVC (.NET 8), MySQL (Entity Framework Core with Pomelo), cookie authentication with role-based authorization.
Also includes the weather dashboard (OpenWeather), advisories, evacuation map (Google Maps), preparedness guides and email alerts (SendGrid) from the project proposal.

## Account features (this increment)
| Requirement | Where |
|---|---|
| Registration with validation, duplicate prevention, hashed passwords, role assignment | `Services/AccountService.cs`, `Controllers/AccountController.cs`, `Views/Account/Register.cshtml` |
| Login, error messages, lockout, secure session, logout | `AccountController`, `Program.cs` (cookie options), `Views/Account/Login.cshtml` |
| Role dashboards, redirect after login, role-based menus | `Controllers/ResidentController.cs`, `Controllers/AdminController.cs`, `Views/Shared/_Layout.cshtml` |
| Role-based access control (backend) | `[Authorize(Roles = ...)]` on controllers, checked again on every request in `Program.cs` |
| Admin user and role management | `AdminController.Users`, `Views/Admin/Users.cshtml` |

Details and manual test cases: `docs/ACCOUNT-FEATURE.md`.

## Requirements
- .NET 8 SDK
- MySQL Server 8 and MySQL Workbench
- VS Code with the C# Dev Kit extension

## Setup
1. Clone the repository and open the folder in VS Code.
2. Copy `appsettings.Local.example.json` to `appsettings.Local.json` (this file is git-ignored, so your secrets are never pushed). Fill in:
   - `ConnectionStrings:DefaultConnection`: your MySQL password
   - `SeedAdmin:Email` and `SeedAdmin:Password`: the first administrator (password needs 8+ characters with upper case, lower case and a number)
   - Optional: `Weather:ApiKey` (OpenWeather), `GoogleMaps:ApiKey`, `Email:SendGridApiKey`
   - Map: with a `GoogleMaps:ApiKey` the evacuation map uses Google Maps. With no key it automatically uses Leaflet with OpenStreetMap (free, no card), and addresses are converted to coordinates with OpenStreetMap Nominatim.
3. Make sure MySQL Server is running. The database `community_weather_pd1` and its tables are created automatically on first run. `database/community_weather_pd1.sql` is the same schema if you prefer to create it in MySQL Workbench first.
4. In the terminal, from the project folder: `dotnet run`
5. Open the HTTPS address printed in the terminal.

## First run
- The first administrator is created in the database from `SeedAdmin` (the password is hashed before it is stored).
- Residents create their own accounts on the Register page.
- Administrators promote other users on **Manage > Users and roles**.

## Project structure
```
Controllers/  Account, Resident, Admin, Home, Advisories, Centers, Guides
Services/     AccountService, PasswordHelper, WeatherService, GeocodingService, EmailService
Models/       User, Roles, Advisory, EvacuationCenter, PreparednessGuide, view models
Data/         AppDbContext, DbSeeder
Views/        Razor views
database/     MySQL schema script
docs/         Feature notes, test cases, Git workflow, completion report draft
```
