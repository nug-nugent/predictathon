using Predictathon.Application.Interfaces.Base;
using Predictathon.Application.Models;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Interfaces;

public interface ICompetitionService : ICrudService<Guid, CompetitionModel, Competition>
{
}