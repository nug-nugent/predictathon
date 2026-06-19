using FluentResults;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;
using Predictathon.Domain.Persistence;

namespace Predictathon.Application.Services;

public class CompetitionService : ICompetitionService
{
    private readonly IDefaultDbContext _dbContext;

    public CompetitionService(IDefaultDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Result<Competition>> Create(CompetitionModel model)
    {
        var competition = model.ToCompetition();

        _dbContext.Competition.Add(competition);

        await _dbContext.SaveChangesAsync();

        return competition;
    }

    public Task<Result> DeleteById(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Competition?> GetById(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Result<Competition>> Update(CompetitionModel model)
    {
        throw new NotImplementedException();
    }
}