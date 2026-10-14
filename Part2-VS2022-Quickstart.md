# RaceDay Part 2 – Visual Studio 2022 Quickstart

## 1. Open the solution (not the folder)
1. Extract the zip. Inside `Part2-API` you will find **RaceDay.sln**.
2. In Visual Studio 2022: **File > Open > Project/Solution** and choose `RaceDay.sln`. (Don't use "Open Folder" – that is the Folder View you are in now.)
3. Solution Explorer now shows two projects: **RaceDay.Api** (the API) and **RaceDay.Tests**.
4. Right-click **RaceDay.Api > Set as Startup Project** (it should already be in bold).
5. Wait for NuGet to restore the packages (needs internet). Then **Build > Rebuild Solution**.

## 2. Check the .NET version
Both projects target **.NET 8**. VS 2022 17.8 or newer supports it. If the build says the target framework is not installed: open *Visual Studio Installer > Modify > Individual components* and tick **.NET 8.0 Runtime / SDK**, or change `net8.0` and every `8.0.*` to your installed version in both `.csproj` files (right-click the project > Edit Project File).

## 3. Point the connection string at YOUR SQL Server
Open `src/RaceDay.Api/appsettings.json`. The default is LocalDB:
`Server=(localdb)\\MSSQLLocalDB;Database=RaceDayApiDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`

If you connect in SSMS with a different server name, put that after `Server=` (keep `\\` doubled in JSON). Examples: `.\\SQLEXPRESS`, `localhost`, `localhost\\SQLEXPRESS`, `(local)`.

## 4. Create the database (Package Manager Console)
1. **Tools > NuGet Package Manager > Package Manager Console**.
2. Set **Default project** (dropdown at the top of that window) to **RaceDay.Api**.
3. Run:
```
Add-Migration InitialCreate
Update-Database
```
4. Open SSMS, refresh Databases: you should see **RaceDayApiDB** with Users, Events, Categories, Routes, Enrolments, Results.

## 5. Run the API
Press **F5** (profile **https** or **http**). The browser opens Swagger at `/swagger`.
- If Visual Studio asks to trust the HTTPS certificate, click **Yes**.
- Log in via `POST /api/auth/login` with `thabo.nkosi@raceday-events.co.za` / `Password123!` (Organiser) or `lindiwe.dlamini@example.co.za` / `Password123!` (Participant).

## 6. Run the unit tests
**Test > Run All Tests** (Test Explorer). All 58 should pass.

## 7. Git from Visual Studio
Use **Git > Create Git Repository / Commit All** or your existing Part 1 repo. The included `.gitignore` keeps `bin`, `obj` and `.vs` out of GitHub. Remember: the GitHub Actions file `.github/workflows/ci.yml` must sit in the **root of the repository** (move it out of `Part2-API` if your repo root is above it).

## 8. If something fails
Copy the full text from **View > Error List** (or the Output window) and send it to me.
