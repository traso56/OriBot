using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel.DataAnnotations.Schema;

namespace OriBot.Utility;

public class SpiritContext : DbContext
{
    private sealed class DateConverter : ValueConverter<DateTime, DateTime>
    {
        public DateConverter()
            : base(d => d.AddTicks(-(d.Ticks % TimeSpan.TicksPerSecond)),
                  d => d)
        { }
    }

    private sealed class ColorConverter : ValueConverter<Color, uint>
    {
        public ColorConverter()
            : base(c => c.RawValue, u => new Color(u))
        { }
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<Punishment> Punishments { get; set; }
    public DbSet<PendingImageRole> PendingImageRoles { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<UniqueBadge> UniqueBadges { get; set; }

    public SpiritContext(DbContextOptions<SpiritContext> options)
        : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //set relations with punishments of the punished and issuer
        modelBuilder.Entity<Punishment>()
            .HasOne<User>(p => p.Punished)
            .WithMany(u => u.Punishments)
            .HasForeignKey(p => p.PunishedId);
        modelBuilder.Entity<Punishment>()
            .HasOne<User>(p => p.Issuer)
            .WithMany(u => u.PunishmentsIssued)
            .HasForeignKey(p => p.IssuerId);

        // make the UserBadge link to hold the many to many relation with payload data
        modelBuilder.Entity<User>()
            .HasMany(u => u.Badges)
            .WithMany(b => b.Users)
            .UsingEntity<UserBadge>(e => e.Property(ub => ub.Count).HasDefaultValue(1));
    }
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // make date time not save milliseconds
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<DateConverter>();

        // enum converter to use strings
        configurationBuilder
            .Properties<Enum>()
            .HaveConversion<string>();

        // color converter
        configurationBuilder
            .Properties<Color>()
            .HaveConversion<ColorConverter>();
    }
}
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class User
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required ulong UserId { get; set; }

    public required string? Title { get; set; }
    public required string? Description { get; set; }
    public required Color Color { get; set; }

    public List<UserBadge> UserBadges { get; set; }
    public List<Badge> Badges { get; set; }
    public List<UniqueBadge> UniqueBadges { get; set; }

    public List<Punishment> Punishments { get; set; }
    public List<Punishment> PunishmentsIssued { get; set; }
}
public class Badge
{
    public int BadgeId { get; set; }

    public required string Name { get; set; }
    public required string MiniDescription { get; set; }
    public required string Description { get; set; }
    public required string Emote { get; set; }
    public required int Experience { get; set; }

    public List<UserBadge> UserBadges { get; set; }
    public List<User> Users { get; set; }
}
public class UserBadge
{
    public ulong UserId { get; set; }
    public required User User { get; set; }

    public int BadgeId { get; set; }
    public required Badge Badge { get; set; }

    public required int Count { get; set; }
}
public enum UniqueBadgeType
{
    ApprovedIdea,
    EmojiCreator
}
public class UniqueBadge
{
    public int UniqueBadgeId { get; set; }

    public required string Data { get; set; }
    public required UniqueBadgeType BadgeType { get; set; }
    public required int Experience { get; set; }

    public ulong UserId { get; set; }
    public required User User { get; set; }
}
public class Punishment
{
    public ulong PunishmentId { get; set; }

    public required PunishmentType Type { get; set; }
    public required string Reason { get; set; }
    public required DateTime Issued { get; set; }
    public required DateTime? Expiry { get; set; }
    public required bool CheckForExpiry { get; set; }

    public ulong PunishedId { get; set; }
    public required User Punished { get; set; }

    public ulong IssuerId { get; set; }
    public required User Issuer { get; set; }
}
public class PendingImageRole
{
    public ulong PendingImageRoleId { get; set; }

    public required ulong UserId { get; set; }
    public required DateTime ImageRoleDateTime { get; set; }
}
public enum PunishmentType
{
    Mute, Warn, Ban, Note
}
public class Ticket
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required ulong TicketId { get; set; }

    public required ulong TicketUserId { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.