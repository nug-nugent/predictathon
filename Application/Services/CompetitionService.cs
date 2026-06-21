using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Services;

[ScopedService]
public class CompetitionService : CrudService<Guid, CompetitionModel, Competition>, ICompetitionService
{
    public CompetitionService(
        ICrudServiceDependencyAggregate<CompetitionModel> dependencyAggregate
    ) : base(dependencyAggregate)
    {
    }

    //public async Task<Result<Competition>> Create(CompetitionModel model)
    //{
    //    var competition = model.ToCompetition();

    //    _dbContext.Competition.Add(competition);

    //    await _dbContext.SaveChangesAsync();

    //    return competition;
    //}

    //public async Task<Result> DeleteById(Guid id)
    //{
    //    throw new NotImplementedException();
    //}

    //public Task<Competition?> GetById(Guid id)
    //{
    //    throw new NotImplementedException();
    //}

    //public Task<Result<Competition>> Update(Guid id, CompetitionModel model)
    //{
    //    throw new NotImplementedException();
    //}
}