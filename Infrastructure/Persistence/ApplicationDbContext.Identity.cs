using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Predictathon.Domain.Identity;

namespace Predictathon.Infrastructure.Persistence;

/// <summary>
/// Identity model wiring for <see cref="ApplicationDbContext"/>. ApplicationDbContext deliberately does
/// NOT inherit IdentityDbContext&lt;,,&gt; (it already inherits GenericDbContext&lt;ApplicationDbContext&gt;,
/// and C# doesn't allow two DbContext base classes) - instead this reproduces the entity configuration
/// IdentityUserContext/IdentityDbContext would otherwise apply automatically, so it's visible and can be
/// reviewed/updated deliberately on a future ASP.NET Core upgrade rather than living invisibly in a base class.
///
/// Adapted from the MIT-licensed dotnet/aspnetcore source:
/// src/Identity/EntityFrameworkCore/src/IdentityUserContext.cs and IdentityDbContext.cs (OnModelCreating),
/// for TUser = ApplicationUser, TRole = IdentityRole&lt;Guid&gt;, TKey = Guid, using the default
/// IdentityUserClaim/IdentityUserLogin/IdentityUserToken/IdentityUserRole/IdentityRoleClaim entity types.
///
/// Table/column shapes here must match Database/Identity/Tables/Users.sql etc. exactly (SSDT-managed,
/// database-first - EF Core Migrations are not used for this schema). Tables live in a dedicated
/// [Identity] schema (Database/Security/Identity.sql), separate from the legacy dbo schema.
/// </summary>
public partial class ApplicationDbContext
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<IdentityRole<Guid>> Roles => Set<IdentityRole<Guid>>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    private void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("Users", "Identity");
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
            b.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");

            b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();
            b.Property(u => u.UserName).HasMaxLength(256);
            b.Property(u => u.NormalizedUserName).HasMaxLength(256);
            b.Property(u => u.Email).HasMaxLength(256);
            b.Property(u => u.NormalizedEmail).HasMaxLength(256);

            // Extended profile columns carried over from dbo.User - match its VARCHAR (non-Unicode) shape.
            b.Property(u => u.Forenames).HasMaxLength(50).IsUnicode(false);
            b.Property(u => u.Surname).HasMaxLength(50).IsUnicode(false);
            b.Property(u => u.FavouriteTeam).HasMaxLength(50).IsUnicode(false);
            b.Property(u => u.Location).HasMaxLength(50).IsUnicode(false);
            b.Property(u => u.Caption).HasMaxLength(30).IsUnicode(false);
            b.Property(u => u.ProfileText).IsUnicode(false);

            b.HasMany<IdentityUserClaim<Guid>>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
            b.HasMany<IdentityUserLogin<Guid>>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
            b.HasMany<IdentityUserToken<Guid>>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
            b.HasMany<IdentityUserRole<Guid>>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
        });

        builder.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("UserClaims", "Identity");
            b.HasKey(uc => uc.Id);
        });

        builder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("UserLogins", "Identity");
            b.HasKey(l => new { l.LoginProvider, l.ProviderKey });
            b.Property(l => l.LoginProvider).HasMaxLength(450);
            b.Property(l => l.ProviderKey).HasMaxLength(450);
        });

        builder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("UserTokens", "Identity");
            b.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
            b.Property(t => t.LoginProvider).HasMaxLength(450);
            b.Property(t => t.Name).HasMaxLength(450);
        });

        builder.Entity<IdentityRole<Guid>>(b =>
        {
            b.ToTable("Roles", "Identity");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();

            b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();
            b.Property(r => r.Name).HasMaxLength(256);
            b.Property(r => r.NormalizedName).HasMaxLength(256);

            b.HasMany<IdentityUserRole<Guid>>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
            b.HasMany<IdentityRoleClaim<Guid>>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
        });

        builder.Entity<IdentityRoleClaim<Guid>>(b =>
        {
            b.ToTable("RoleClaims", "Identity");
            b.HasKey(rc => rc.Id);
        });

        builder.Entity<IdentityUserRole<Guid>>(b =>
        {
            b.ToTable("UserRoles", "Identity");
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
        });
    }

    /// <summary>
    /// Our own addition, not part of the copied Identity source above - configures the
    /// app-specific RefreshTokens table (Database/Identity/Tables/RefreshTokens.sql).
    /// </summary>
    private void ConfigureRefreshTokens(ModelBuilder builder)
    {
        builder.Entity<RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens", "Identity");
            b.HasKey(t => t.Id);
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => t.UserId);

            b.Property(t => t.TokenHash).HasMaxLength(32).IsFixedLength();

            b.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.UserId).IsRequired();
        });
    }
}
