using Mapster;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Mapping;

public static class MapsterConfiguration
{
    public static void Configure()
    {
        var config = TypeAdapterConfig.GlobalSettings;

        // Example explicit mapping between CompetitionModel and Competition. Mapster will by default
        // map properties with matching names, but explicit configuration is useful for exceptions.
        config.NewConfig<CompetitionModel, Competition>();
        config.NewConfig<Competition, CompetitionModel>();

        config.NewConfig<MatchModel, Match>();
        config.NewConfig<Match, MatchModel>();

        config.NewConfig<Team, TeamModel>();

        // Add further mappings here as your application grows.
    }
}
