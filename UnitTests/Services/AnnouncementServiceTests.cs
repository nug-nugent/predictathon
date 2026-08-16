using FluentAssertions;
using MapsterMapper;
using Predictathon.Application.Constants;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Application.Validators;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class AnnouncementServiceTests
{
    private static (InMemoryApplicationDbContext DbContext, AnnouncementService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateAnnouncementModel, AnnouncementModel>(dbContext, new Mapper());
        var service = new AnnouncementService(dependencyAggregate, dbContext);
        return (dbContext, service);
    }

    [Fact]
    public async Task GetActiveAsync_ExcludesExpiredAnnouncements()
    {
        var (dbContext, service) = MakeService();
        dbContext.Announcement.Add(new DomainEntities.Announcement
        {
            Content = "Expired",
            CreatedByUserID = Guid.NewGuid(),
            Severity = AnnouncementSeverities.Info,
            ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(-1),
        });
        await dbContext.SaveChangesAsync();

        var active = await service.GetActiveAsync();

        active.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveAsync_IncludesAnnouncementsWithNoExpiryOrFutureExpiry()
    {
        var (dbContext, service) = MakeService();
        dbContext.Announcement.AddRange(
            new DomainEntities.Announcement { Content = "No expiry", CreatedByUserID = Guid.NewGuid(), Severity = AnnouncementSeverities.Info, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10) },
            new DomainEntities.Announcement { Content = "Future expiry", CreatedByUserID = Guid.NewGuid(), Severity = AnnouncementSeverities.Info, ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(1), CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5) });
        await dbContext.SaveChangesAsync();

        var active = await service.GetActiveAsync();

        active.Select(a => a.Content).Should().BeEquivalentTo(["No expiry", "Future expiry"]);
    }

    [Fact]
    public async Task GetActiveAsync_OrdersByCreatedAtUtcDescending()
    {
        var (dbContext, service) = MakeService();
        dbContext.Announcement.AddRange(
            new DomainEntities.Announcement { Content = "Older", CreatedByUserID = Guid.NewGuid(), Severity = AnnouncementSeverities.Info, CreatedAtUtc = DateTime.UtcNow.AddDays(-2) },
            new DomainEntities.Announcement { Content = "Newer", CreatedByUserID = Guid.NewGuid(), Severity = AnnouncementSeverities.Info, CreatedAtUtc = DateTime.UtcNow.AddDays(-1) });
        await dbContext.SaveChangesAsync();

        var active = await service.GetActiveAsync();

        active.Select(a => a.Content).Should().Equal("Newer", "Older");
    }

    [Fact]
    public async Task GetAllAsync_IncludesExpiredAnnouncements()
    {
        // Unlike GetActiveAsync, the admin list needs to show expired announcements too.
        var (dbContext, service) = MakeService();
        dbContext.Announcement.Add(new DomainEntities.Announcement
        {
            Content = "Expired",
            CreatedByUserID = Guid.NewGuid(),
            Severity = AnnouncementSeverities.Info,
            ExpiryDateTimeUtc = DateTime.UtcNow.AddDays(-1),
        });
        await dbContext.SaveChangesAsync();

        var all = await service.GetAllAsync();

        all.Should().ContainSingle(a => a.Content == "Expired");
    }

    [Fact]
    public async Task CreateAsync_ValidModel_SetsCreatedByUserAndAtUtc()
    {
        var (dbContext, service) = MakeService();
        var userId = Guid.NewGuid();

        var result = await service.CreateAsync(new CreateAnnouncementModel { Content = "Hello" }, userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedByUserID.Should().Be(userId);
        result.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        dbContext.Announcement.Should().ContainSingle(a => a.Content == "Hello" && a.CreatedByUserID == userId);
    }

    [Fact]
    public async Task CreateAsync_SetsShowOnHomepageFromModel()
    {
        var (dbContext, service) = MakeService();

        var result = await service.CreateAsync(
            new CreateAnnouncementModel { Content = "Hello", ShowOnLoginPage = true, ShowOnHomepage = false },
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.ShowOnHomepage.Should().BeFalse();
        dbContext.Announcement.Should().ContainSingle(a => a.Content == "Hello" && !a.ShowOnHomepage);
    }

    [Fact]
    public async Task GetActiveAsync_IncludesShowOnHomepage()
    {
        var (dbContext, service) = MakeService();
        dbContext.Announcement.Add(new DomainEntities.Announcement
        {
            Content = "Homepage only",
            CreatedByUserID = Guid.NewGuid(),
            Severity = AnnouncementSeverities.Info,
            ShowOnLoginPage = false,
            ShowOnHomepage = true,
        });
        await dbContext.SaveChangesAsync();

        var active = await service.GetActiveAsync();

        active.Should().ContainSingle(a => a.Content == "Homepage only" && a.ShowOnHomepage);
    }

    [Fact]
    public async Task CreateAsync_SetsSeverityFromModel()
    {
        var (dbContext, service) = MakeService();

        var result = await service.CreateAsync(
            new CreateAnnouncementModel { Content = "Deploy incoming", Severity = AnnouncementSeverities.Warning },
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.Severity.Should().Be(AnnouncementSeverities.Warning);
        dbContext.Announcement.Should().ContainSingle(a => a.Content == "Deploy incoming" && a.Severity == AnnouncementSeverities.Warning);
    }

    [Fact]
    public async Task GetActiveAsync_IncludesSeverity()
    {
        var (dbContext, service) = MakeService();
        dbContext.Announcement.Add(new DomainEntities.Announcement
        {
            Content = "Deploy incoming",
            CreatedByUserID = Guid.NewGuid(),
            Severity = AnnouncementSeverities.Warning,
        });
        await dbContext.SaveChangesAsync();

        var active = await service.GetActiveAsync();

        active.Should().ContainSingle(a => a.Content == "Deploy incoming" && a.Severity == AnnouncementSeverities.Warning);
    }

    [Fact]
    public async Task CreateAsync_InvalidModel_ReturnsFailureWithoutSaving()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var dependencyAggregate = new CrudServiceDependencyAggregate<CreateAnnouncementModel, AnnouncementModel>(dbContext, new Mapper());
        var service = new AnnouncementService(dependencyAggregate, dbContext, new CreateAnnouncementModelValidator());

        var result = await service.CreateAsync(new CreateAnnouncementModel { Content = "" }, Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        dbContext.Announcement.Should().BeEmpty();
    }
}
