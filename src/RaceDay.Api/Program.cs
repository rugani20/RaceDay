using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);
//add swagger authoriser

// ---- add Database (EF Core, SQL Server). The "Testing" environment uses an in-memory database for the unit tests. ----
if (builder.Environment.IsEnvironment("Testing"))
{
    var testDbName = "RaceDayTests-" + Guid.NewGuid();
    builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(testDbName));
}
else
{
    //add xml comments to controllers
    //implement userservice interface
    //add seed data for categories
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}
//add refactor controllers to use DTOs
// ---- Session management (server-side session stores UserId + Role) ----
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".RaceDay.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromMinutes(30);
});
//upadte readme and api usage notes

//fix failing enrolment test

//add users table model
builder.Services.AddControllers();

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        //fix login returning wrong status code
        Title = "RaceDay API",
        Version = "v1",
        Description = "Event management API for South African road running, walking and cycling events. " +
                      "Log in via POST /api/auth/login first - the session cookie is then sent automatically by Swagger UI."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});
//fix nullabale warnings in models
var app = builder.Build();

// ---- Create/migrate the database and seed sample data ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational()) db.Database.Migrate();
    else db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}
// add session middleware
//added program.cs services registration
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1"));

//add project folder structure
if (!app.Environment.IsEnvironment("Testing")) app.UseHttpsRedirection();


//add events model with navigation properties
app.UseSession();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
//fix login returning wrong status code
// Makes Program visible to the test project (WebApplicationFactory<Program>).
public partial class Program { }
//add result validation