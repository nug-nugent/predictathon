using FluentAssertions;
using Predictathon.Application.Services;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class ErrorLogServiceTests
{
    private static DomainEntities.ErrorLog MakeError(DateTime timeStampUtc, string level = "Error", string? message = null) => new()
    {
        Level = level,
        Message = message ?? $"Something went wrong at {timeStampUtc:O}",
        TimeStampUtc = timeStampUtc,
        Exception = "System.InvalidOperationException: boom",
    };

    private static (InMemoryApplicationDbContext DbContext, ErrorLogService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var service = new ErrorLogService(dbContext);
        return (dbContext, service);
    }

    [Fact]
    public async Task GetErrorsAsync_ReturnsNewestFirst()
    {
        var (dbContext, service) = MakeService();
        var oldest = MakeError(new DateTime(2026, 8, 1, 10, 0, 0), message: "oldest");
        var newest = MakeError(new DateTime(2026, 8, 2, 10, 0, 0), message: "newest");
        var middle = MakeError(new DateTime(2026, 8, 1, 18, 0, 0), message: "middle");
        dbContext.ErrorLog.AddRange(oldest, newest, middle);
        await dbContext.SaveChangesAsync();

        var result = await service.GetErrorsAsync(page: 1, pageSize: 10);

        result.Items.Select(i => i.Message).Should().ContainInOrder("newest", "middle", "oldest");
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetErrorsAsync_PagesResults_AndReportsFullTotalCount()
    {
        var (dbContext, service) = MakeService();
        for (var i = 0; i < 5; i++)
        {
            dbContext.ErrorLog.Add(MakeError(new DateTime(2026, 8, 1, 10, 0, 0).AddHours(i), message: $"error {i}"));
        }

        await dbContext.SaveChangesAsync();

        var secondPage = await service.GetErrorsAsync(page: 2, pageSize: 2);

        secondPage.Items.Should().HaveCount(2);
        secondPage.Items.Select(i => i.Message).Should().ContainInOrder("error 2", "error 1");
        secondPage.TotalCount.Should().Be(5);
        secondPage.Page.Should().Be(2);
        secondPage.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetErrorsAsync_StampsTimesAsUtc()
    {
        var (dbContext, service) = MakeService();
        var storedTime = new DateTime(2026, 8, 2, 9, 30, 0, DateTimeKind.Unspecified);
        dbContext.ErrorLog.Add(MakeError(storedTime));
        await dbContext.SaveChangesAsync();

        var result = await service.GetErrorsAsync(page: 1, pageSize: 10);

        var item = result.Items.Should().ContainSingle().Subject;
        item.TimeStampUtc.Should().Be(storedTime);
        item.TimeStampUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetErrorsAsync_EmptyLog_ReturnsEmptyPage()
    {
        var (_, service) = MakeService();

        var result = await service.GetErrorsAsync(page: 1, pageSize: 10);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
