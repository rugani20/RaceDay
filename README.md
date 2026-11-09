# RaceDay – Part 2: RESTful API

> Paste this section into the README that already exists in your repository (keep your Part 1 section). Fill in the three `TODO` items.

## System description
RaceDay is a web-based event management system for South African road running, walking and cycling events. Organisers create and manage events, categories and results; Participants browse events, enrol in a category and track their results. Part 2 is the ASP.NET Core Web API that powers the whole system.

## Technology
ASP.NET Core Web API (C#, .NET 8) · Entity Framework Core (Code-First) · SQL Server · Session-based authentication · Swagger (Swashbuckle) · xUnit · GitHub Actions

## The two roles
| Role | Can do |
|---|---|
| **Organiser** | Create, edit and delete their own events; manage categories; view all enrolments for their events; capture and update results; view/update own profile. Registering as an Organiser requires a secret organiser code. |
| **Participant** | Register, browse events and categories, enrol in an event by selecting a category, cancel own enrolment, view own enrolments and results, view/update own profile. |

Role-based access is enforced on the API with a `[SessionAuthorize]` filter: **401** when not logged in, **403** when the role (or ownership) is wrong.

## Project structure
```
Part2-API/
  RaceDay.sln
  src/RaceDay.Api/          Models, Data (DbContext + seeder), Dtos, Security, Controllers, Migrations, Program.cs
  tests/RaceDay.Tests/      xUnit tests (run against the real pipeline with an in-memory database)
docs/                       Part 1: ERD, endpoint plan, SQL script
.github/workflows/ci.yml    Build + test on every push
```

## Setup and run
1. Install the .NET 8 SDK, SQL Server (LocalDB or SQL Server Express) and Git.
2. Clone the repo and open a terminal in `Part2-API/src/RaceDay.Api`.
3. Check the connection string in `appsettings.json` (default: `(localdb)\MSSQLLocalDB`, database `RaceDayApiDB`). Change the server name if you use SQL Express (e.g. `.\SQLEXPRESS`).
4. Create the database (tables are created from the EF migration, sample data is seeded on first start):
   ```
   dotnet tool install --global dotnet-ef
   dotnet ef database update
   ```
5. Run the API:
   ```
   dotnet run
   ```
6. Open the URL shown in the console and go to `/swagger` (the root URL redirects there).

### Demo accounts (all use the password `Password123!`)
| Role | Email |
|---|---|
| Organiser | thabo.nkosi@raceday-events.co.za |
| Organiser | sarah.vdm@raceday-events.co.za |
| Participant | lindiwe.dlamini@example.co.za |
| Participant | johan.botha@example.co.za |

Organiser registration code (appsettings.json → `Registration:OrganiserCode`): `RACEDAY-ORG-2026`

### Using Swagger
Call `POST /api/auth/login` first. The session cookie is then sent automatically by the browser, so every other endpoint can be tried straight from Swagger UI. `POST /api/auth/logout` ends the session.

### Run the unit tests
```
cd Part2-API
dotnet test
```

## CI/CD
GitHub Actions (`.github/workflows/ci.yml`) validates the repository structure, restores, builds and runs all unit tests on every push.

![Green build](docs/ci-green-build.png)  <!-- TODO: add your screenshot to /docs and keep this path -->

## Video
YouTube (unlisted): TODO paste link

## Deviations from the Part 1 plan (and why)
| # | Part 1 plan | Part 2 implementation | Reason |
|---|---|---|---|
| 1 | `POST /api/auth/login` returns "token + user profile" | Returns the user profile and starts a **server-side session** (cookie) | The Part 2 brief requires session management, with the session storing the user id and role |
| 2 | – | Added `POST /api/auth/logout` | Needed to end a session |
| 3 | Register body had `role` only | Organiser registration also needs `organiserCode` (403 if wrong) | The brief asks how Organiser registration should be managed – otherwise anyone could make themselves an Organiser |
| 4 | `POST /api/events` body: eventName, eventType, eventDate, maxParticipants | Also `location` and `description` | The brief requires events to capture name, description, date, location, distance and type |
| 5 | – | Added `GET /api/events/mine` (Organiser) | Lets the Organiser dashboard list all of their events, including past ones |
| 6 | `GET /api/events` "searchable/filterable" | Query parameters `search`, `eventType`, `includePast` | Concrete implementation of the planned filtering |
| 7 | `DELETE /api/enrolments/{id}` "cancels" | Sets enrolment status to `Cancelled` (row kept) | Keeps history and matches the `Cancelled` value in the Part 1 CHECK constraint |
| 8 | – | Extra 409 responses (duplicate category name, deleting a category that has enrolments, result already exists) and 400 when an event is not open for enrolment | Protects data integrity |
| 9 | Database `RaceDayDB` (SQL script) | EF Core creates `RaceDayApiDB` from migrations | Avoids clashing with the Part 1 script; tables, columns, keys and constraints are identical |
| 10 | Table `Routes` | Class `RaceRoute`, table still `Routes` | Avoids a name clash with MVC's `[Route]` attribute |

## AI disclosure
TODO – write this in your own words and be specific, for example: "I used Claude (Anthropic) to help plan the Part 2 project structure and to generate a first draft of the API and unit tests. I reviewed, ran and modified the code, and I can explain how each part works."
