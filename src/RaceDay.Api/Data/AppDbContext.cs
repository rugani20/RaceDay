using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RaceRoute> Routes => Set<RaceRoute>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    // Mirrors the Part 1 SQL script: same tables, column types, keys, defaults and CHECK/UNIQUE constraints.
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            e.HasKey(x => x.UserID);
            e.Property(x => x.FullName).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.Email).HasColumnType("varchar(150)").IsRequired();
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
            e.Property(x => x.PasswordHash).HasColumnType("varchar(256)").IsRequired();
            e.Property(x => x.Role).HasColumnType("varchar(20)").IsRequired();
            e.Property(x => x.PhoneNumber).HasColumnType("varchar(20)");
            e.Property(x => x.City).HasColumnType("varchar(50)");
            e.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
        });

        mb.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_Type", "EventType IN ('Running', 'Walking', 'Cycling')");
                t.HasCheckConstraint("CK_Events_Status", "[Status] IN ('Upcoming', 'Completed', 'Cancelled')");
            });
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasColumnType("varchar(150)").IsRequired();
            e.Property(x => x.EventType).HasColumnType("varchar(20)").IsRequired();
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.Description).HasColumnType("varchar(1000)");
            e.Property(x => x.Status).HasColumnType("nvarchar(20)").IsRequired().HasDefaultValue(EventStatus.Upcoming);
            e.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserID).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Events_Organiser");
        });

        mb.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(x => x.EntryFee).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
            e.HasIndex(x => new { x.EventID, x.CategoryName }).IsUnique().HasDatabaseName("UQ_Categories_EventName");
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Categories_Event");
        });

        mb.Entity<RaceRoute>(e =>
        {
            e.ToTable("Routes");
            e.HasKey(x => x.RouteID);
            e.Property(x => x.RouteName).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(x => x.MapUrl).HasColumnType("varchar(300)");
            e.HasOne(x => x.Event).WithMany(ev => ev.Routes)
                .HasForeignKey(x => x.EventID).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Routes_Event");
        });

        mb.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed', 'Cancelled')"));
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime2").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Status).HasColumnType("varchar(20)").IsRequired().HasDefaultValue(EnrolmentStatus.Confirmed);
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique().HasDatabaseName("UQ_Enrolments_ParticipantCategory");
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantID).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Enrolments_Participant");
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Enrolments_Category");
        });

        mb.Entity<Result>(e =>
        {
            e.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished', 'DNF', 'DQ')"));
            e.HasKey(x => x.ResultID);
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasColumnType("varchar(20)").IsRequired().HasDefaultValue(ResultStatus.Finished);
            e.HasIndex(x => x.EnrolmentID).IsUnique().HasDatabaseName("UQ_Results_Enrolment");
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentID).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Results_Enrolment");
        });
    }
}
