using FluentAssertions;
using Predictathon.Application.Errors;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Application.Validators;
using Predictathon.UnitTests.TestDoubles;
using DomainEntities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.Services;

public class PaymentCreditServiceTests
{
    private static (InMemoryApplicationDbContext DbContext, PaymentCreditService Service) MakeService(bool withValidator = false)
    {
        var dbContext = new InMemoryApplicationDbContext();
        var service = withValidator
            ? new PaymentCreditService(dbContext, new CreatePaymentCreditModelValidator())
            : new PaymentCreditService(dbContext);
        return (dbContext, service);
    }

    private static DomainEntities.Competition MakeCompetition() => new()
    {
        CompetitionID = Guid.NewGuid(),
        CompetitionName = "Premier League 2025/26",
    };

    [Fact]
    public async Task GetAllAsync_OrdersByIssueDateDescendingThenExpectedUsername()
    {
        var (dbContext, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        dbContext.PaymentCredit.AddRange(
            new DomainEntities.PaymentCredit { PaymentCreditID = Guid.NewGuid(), ForCompetitionID = competition.CompetitionID, ExpectedUsername = "Zara", UniquePaymentCode = "CODE1", IssuedByUserID = Guid.NewGuid(), IssueDate = DateTime.UtcNow.AddDays(-1) },
            new DomainEntities.PaymentCredit { PaymentCreditID = Guid.NewGuid(), ForCompetitionID = competition.CompetitionID, ExpectedUsername = "Bob", UniquePaymentCode = "CODE2", IssuedByUserID = Guid.NewGuid(), IssueDate = DateTime.UtcNow },
            new DomainEntities.PaymentCredit { PaymentCreditID = Guid.NewGuid(), ForCompetitionID = competition.CompetitionID, ExpectedUsername = "Alice", UniquePaymentCode = "CODE3", IssuedByUserID = Guid.NewGuid(), IssueDate = DateTime.UtcNow.AddDays(-1) });
        await dbContext.SaveChangesAsync();

        var result = await service.GetAllAsync();

        result.Select(r => r.ExpectedUsername).Should().Equal("Bob", "Alice", "Zara");
        result.Should().OnlyContain(r => r.CompetitionName == competition.CompetitionName);
    }

    [Fact]
    public async Task CreateAsync_InvalidModel_ReturnsFailureWithoutSaving()
    {
        var (dbContext, service) = MakeService(withValidator: true);

        var result = await service.CreateAsync(new CreatePaymentCreditModel { CompetitionID = Guid.Empty, ExpectedUsername = "" }, Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        dbContext.PaymentCredit.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_CompetitionNotFound_ReturnsNotFound()
    {
        var (_, service) = MakeService();

        var result = await service.CreateAsync(new CreatePaymentCreditModel { CompetitionID = Guid.NewGuid(), ExpectedUsername = "dave" }, Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task CreateAsync_ValidModel_GeneratesUniqueCodeAndSaves()
    {
        var (dbContext, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();
        var issuedBy = Guid.NewGuid();

        var result = await service.CreateAsync(new CreatePaymentCreditModel { CompetitionID = competition.CompetitionID, ExpectedUsername = "  dave  " }, issuedBy);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpectedUsername.Should().Be("dave");
        result.Value.CompetitionName.Should().Be(competition.CompetitionName);
        result.Value.UniquePaymentCode.Should().HaveLength(10);
        result.Value.UniquePaymentCode.Should().MatchRegex("^[A-Z0-9]+$");

        var saved = dbContext.PaymentCredit.Should().ContainSingle().Subject;
        saved.IssuedByUserID.Should().Be(issuedBy);
        saved.UniquePaymentCode.Should().Be(result.Value.UniquePaymentCode);
    }

    [Fact]
    public async Task CreateAsync_CalledTwice_GeneratesDifferentCodes()
    {
        var (dbContext, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        await dbContext.SaveChangesAsync();

        var first = await service.CreateAsync(new CreatePaymentCreditModel { CompetitionID = competition.CompetitionID, ExpectedUsername = "dave" }, Guid.NewGuid());
        var second = await service.CreateAsync(new CreatePaymentCreditModel { CompetitionID = competition.CompetitionID, ExpectedUsername = "dave" }, Guid.NewGuid());

        first.Value.UniquePaymentCode.Should().NotBe(second.Value.UniquePaymentCode);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsNotFound()
    {
        var (_, service) = MakeService();

        var result = await service.DeleteAsync(Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task DeleteAsync_Found_RemovesIt()
    {
        var (dbContext, service) = MakeService();
        var competition = MakeCompetition();
        dbContext.Competition.Add(competition);
        var credit = new DomainEntities.PaymentCredit { PaymentCreditID = Guid.NewGuid(), ForCompetitionID = competition.CompetitionID, UniquePaymentCode = "CODE1", IssuedByUserID = Guid.NewGuid(), IssueDate = DateTime.UtcNow };
        dbContext.PaymentCredit.Add(credit);
        await dbContext.SaveChangesAsync();

        var result = await service.DeleteAsync(credit.PaymentCreditID);

        result.IsSuccess.Should().BeTrue();
        dbContext.PaymentCredit.Should().BeEmpty();
    }
}
