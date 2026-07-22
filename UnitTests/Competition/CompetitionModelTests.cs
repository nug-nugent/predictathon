using AutoFixture;
using FluentAssertions;
using Mapster;

namespace Predictathon.UnitTests.Competition
{
    public class CompetitionModelTests
    {
        [Fact]
        public void CompetitionModel_Map_ToEntity_AndBack_PreservesData()
        {
            // Arrange
            var fixture = new AutoFixture.Fixture();
            // (no special behaviours required for this simple POCO)

            // Ensure AutoFixture produces safe DateOnly values
            fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(fixture.Create<int>() % 3650 - 1825)));

            var model = fixture.Create<Predictathon.Application.Models.CompetitionModel>();
            // Make sure EndDate is after StartDate for mapping assertions
            var start = fixture.Create<DateOnly>();
            var end = start.AddDays(Math.Abs(fixture.Create<int>() % 30) + 1);
            model.StartDate = start;
            model.EndDate = end;

            // Ensure Mapster configuration is applied
            Predictathon.Application.Mapping.MapsterConfiguration.Configure();

            // Act
            var entity = model.Adapt<Predictathon.Domain.Entities.Competition>();
            var roundtrip = entity.Adapt<Predictathon.Application.Models.CompetitionModel>();

            // Assert
            roundtrip.Should().BeEquivalentTo(model, opts => opts
                .Using<DateOnly>(ctx => ctx.Subject.Should().Be(ctx.Expectation))
                .WhenTypeIs<DateOnly>());
        }
    }
}
