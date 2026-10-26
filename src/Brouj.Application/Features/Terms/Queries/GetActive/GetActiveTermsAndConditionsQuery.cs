using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.DTOs;
using MediatR;

namespace Brouj.Application.Features.Terms.Queries.GetActive;

public sealed class GetActiveTermsAndConditionsQuery : IRequest<Result<TermsDto>>
{

}
