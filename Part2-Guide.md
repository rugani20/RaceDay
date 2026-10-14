# RaceDay Part 2 – Complete Guide

Everything the brief asks for in Part 2, in the order you should do it. Total: **100 marks** (Correct submission 5 · GitHub & CI/CD 10 · Auth & roles 20 · API endpoints & DB 25 · Swagger 10 · Unit tests 15 · Video 15), minus 5% if GitHub is not used properly.

## 0. Read this first
- **Be able to explain every file.** The video must be in **your own voice** (no AI voices) and explain the code structure, logic and design decisions. Read through each file once before recording.
- **Disclose AI use** (the brief requires it). A template is in `README-Part2-section.md`.
- **Commits must be real work.** You need **20+ meaningful commits** with your own GitHub account. Do not paste everything in one go. Follow the commit points below – the project is deliberately built layer by layer so each commit compiles.
- I could not run `dotnet` where I generated this, so I have not compiled it. I checked it carefully line by line, but **do step 12 (build + test) before you record anything**. If anything fails, send me the exact error text and I will fix it.

## 1. Install / check tools
```
dotnet --version
git --version
```
- This project targets **.NET 8**. If `dotnet --version` starts with 8 you are fine. If it starts with 9 or 10, either install the **.NET 8 SDK** as well (easiest, it also matches the CI file), or change `net8.0` to `net9.0`/`net10.0` and every `8.0.*` package version to `9.0.*`/`10.0.*` in both `.csproj` files and `8.0.x` in `ci.yml`.
- SQL Server: LocalDB or SQL Express (the one you used in SSMS). Find your server name in SSMS's connect dialog.
- EF tool: `dotnet tool install --global dotnet-ef`

## 2. Repository layout
Your Part 1 repo already has `/docs` and a README. Add the Part 2 code in a clearly named subfolder (the brief allows a subfolder or branch):
```
<your repo>/
  docs/                      <- Part 1 (ERD, endpoint plan, .sql) – leave as is
  README.md                  <- add the Part 2 section
  .github/workflows/ci.yml   <- Part 1 workflow: REPLACE with the new one in step 11
  Part2-API/
    RaceDay.sln
    src/RaceDay.Api/
    tests/RaceDay.Tests/
```
Work on a branch if you like (`git checkout -b part2`), or commit straight to main.

## 3. Build it layer by layer (commit after each)
The finished files are in the zip (`Part2-API/…`). Create the solution, then add the files in this order and commit at each point.

**Scaffold (commit 1)**
```
cd <your repo>
mkdir Part2-API
cd Part2-API
dotnet new sln -n RaceDay
dotnet new webapi -n RaceDay.Api -o src/RaceDay.Api --use-controllers
dotnet new xunit -n RaceDay.Tests -o tests/RaceDay.Tests
dotnet sln add src/RaceDay.Api tests/RaceDay.Tests
cd ..
dotnet new gitignore        (only if you have no .gitignore yet)
```
Delete the template's `Controllers/WeatherForecastController.cs`, `WeatherForecast.cs`, and any `.http` file.
Commit: *"Part 2: scaffold solution, API and test projects"*

| # | Commit message | Files to add / replace |
|---|---|---|
| 2 | Configure packages, XML docs and settings | `RaceDay.Api.csproj`, `appsettings.json` |
| 3 | Add entity models | `Models/*.cs` (7 files) |
| 4 | Add AppDbContext matching Part 1 ERD and SQL | `Data/AppDbContext.cs` |
| 5 | Add password hashing service | `Security/PasswordService.cs` |
| 6 | Add session security (SessionAuthorize filter, base controller) | `Security/SessionKeys.cs`, `Security/SessionAuthorizeAttribute.cs`, `Dtos/ApiResponses.cs`, `Controllers/ApiControllerBase.cs` |
| 7 | Add database seeder with sample data | `Data/DbSeeder.cs` |
| 8 | Configure Program.cs: EF Core, session, Swagger | `Program.cs` |
| 9 | Add DTOs and mapper | `Dtos/Dtos.cs`, `Dtos/Mapper.cs` |
| 10 | Add initial EF migration | see step 4 below |
| 11 | Add auth endpoints (register, login, logout) | `Controllers/AuthController.cs` |
| 12 | Add user profile endpoints | `Controllers/UsersController.cs` |
| 13 | Add events, categories, routes and organiser enrolment endpoints | `Controllers/EventsController.cs` |
| 14 | Add category update and delete endpoints | `Controllers/CategoriesController.cs` |
| 15 | Add participant enrolment endpoints | `Controllers/EnrolmentsController.cs` |
| 16 | Add results endpoints | `Controllers/ResultsController.cs` |
| 17 | Test project: factory and helpers | `tests/RaceDay.Tests/RaceDay.Tests.csproj`, `RaceDayFactory.cs`, `ApiTestBase.cs` (delete the template `UnitTest1.cs`) |
| 18 | Add password and authentication tests | `PasswordServiceTests.cs`, `AuthTests.cs` |
| 19 | Add event and category tests | `EventTests.cs` |
| 20 | Add role access tests | `RoleAccessTests.cs` |
| 21 | Add enrolment and results tests | `EnrolmentTests.cs` |
| 22 | Add GitHub Actions workflow | `.github/workflows/ci.yml` |
| 23 | Add README Part 2 section | `README.md` |
| 24 | Add CI screenshot and video link | `docs/ci-green-build.png`, README |

Tip: commits 3–9 will not all compile on their own before step 8 (Program.cs references the seeder and DbContext) – that is fine, but build (`dotnet build`) before each commit from step 8 onwards.

## 4. Create the database (EF Code-First)
In `Part2-API/src/RaceDay.Api`:
```
dotnet build
dotnet ef migrations add InitialCreate
dotnet ef database update
```
- Check `appsettings.json` → `DefaultConnection`. For SQL Express use `Server=.\\SQLEXPRESS;Database=RaceDayApiDB;Trusted_Connection=True;TrustServerCertificate=True;` (double backslash in JSON).
- Open SSMS → refresh → you should see **RaceDayApiDB** with `Users, Events, Categories, Routes, Enrolments, Results` plus `__EFMigrationsHistory`. This is your proof that the schema matches Part 1.
- The API also applies migrations and seeds the demo data on every start (seeding only happens on an empty database).
- Why a different database name? Your Part 1 script drops and recreates `RaceDayDB`. Keeping them separate avoids clashes. If you want the same name, drop `RaceDayDB` in SSMS first and change the connection string.

## 5. Run and try it
```
dotnet run
```
Open the printed URL (e.g. `https://localhost:7xxx`) → it redirects to `/swagger`.
1. `POST /api/auth/login` with `{ "email": "thabo.nkosi@raceday-events.co.za", "password": "Password123!" }` → 200.
2. Try `GET /api/users/me`, `POST /api/events`, `POST /api/events/{id}/categories`.
3. Logout, log in as `lindiwe.dlamini@example.co.za`, try `POST /api/events` → **403**. Without logging in → **401**.

## 6. How the pieces satisfy the rubric
| Rubric item | Where |
|---|---|
| Auth & role-based access (20) | `AuthController` (register/login/logout), session stores `UserId` + `Role`, `SessionAuthorizeAttribute` returns 401/403, passwords hashed with PBKDF2 (`PasswordService`) |
| API endpoints & DB (25) | All Part 1 endpoints, EF Core code-first, `AppDbContext` mirrors the SQL script, full CRUD for events/categories |
| Swagger (10) | Swashbuckle at `/swagger`; XML comments (`/// <summary>`, `<remarks>`, `<response>`) give every endpoint a purpose, expected input and responses |
| Unit tests (15) | 50+ tests: registration/login, event management, role rejection, enrolment recorded; success **and** failure cases |
| GitHub & CI/CD (10) | `ci.yml` validates repo structure, builds and runs tests on every push; 20+ commits |
| Correct submission (5) | `/docs`, README with setup, roles, CI screenshot, video link |

## 7. Run the tests
```
cd Part2-API
dotnet test
```
All should pass. The tests start the real API pipeline in memory (`WebApplicationFactory`), so they exercise sessions, the 401/403 filter and the controllers together – not just isolated methods.

## 8. Match your plan (important – "unexplained deviations affect your mark")
Your Part 1 plan and the finished API differ in a few places (token → session, extra logout endpoint, organiser code, etc.). They are all listed in the **"Deviations"** table inside `README-Part2-section.md`. Keep that table in your README, and mention it in the video.

## 9. Submission checklist
- [ ] Repo link submitted on ARC; all code pushed (no ZIP)
- [ ] 20+ meaningful commits for Part 2
- [ ] `README.md`: setup/run instructions, description of both roles, CI screenshot, YouTube link, deviations table, AI disclosure
- [ ] `.github/workflows/ci.yml` shows a **green tick** on your latest commit (Actions tab) – screenshot it into `docs/ci-green-build.png`
- [ ] `dotnet test` passes locally
- [ ] Unlisted YouTube video, link in README

## 10. Video outline (aim for ~8–10 minutes, your own voice)
1. **Intro** – RaceDay, the two roles, tech used.
2. **Project structure** – walk through folders: Models, Data, Dtos, Security, Controllers, Tests.
3. **Database** – show SSMS tables created by the migration; say it matches your Part 1 ERD/SQL.
4. **Authentication** – register (explain organiser code), login, session stores UserId/Role, password hashing, logout; show the 401/403 filter code.
5. **Swagger demo** – log in as organiser → create event, add category, view enrolments; log in as participant → enrol, cancel; organiser captures result; participant sees it. Show a 403 and a 401.
6. **Endpoints vs plan** – show the plan table and mention the deviations.
7. **Unit tests** – run `dotnet test` live, explain a success test and a failure/role test.
8. **CI/CD** – show the green build in GitHub Actions.

## 11. Notes for Part 3
- The Part 3 MVC app will call this API server-side. Because the API uses a session cookie, the MVC `HttpClient` must keep that cookie (a `CookieContainer`, or forward the `.RaceDay.Session` cookie) – ask me when you get there.
- Part 3 mentions an amber "Pending" enrolment status, but your Part 1 constraint only allows `Confirmed` and `Cancelled`. Decide now whether to add `Pending` (then add it to the CHECK constraint, a new migration, and explain it in the README).
- Part 3 adds Azure Blob image upload to this API (event banner, profile picture).
