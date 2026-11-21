using Brouj.Application.Common.Models;
using Brouj.Application.Features.ContactSubmissions.DTOs;
using MediatR;

namespace Brouj.Application.Features.ContactSubmissions.Commands.Submit;

public sealed class SubmitContactSubmissionCommand : IRequest<Result<Guid>>
{
    public ContactSubmissionInput Input { get; init; } = new();
}
