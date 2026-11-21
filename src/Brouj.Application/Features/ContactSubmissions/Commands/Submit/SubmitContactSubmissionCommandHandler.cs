using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Domain.Entities;
using MediatR;

namespace Brouj.Application.Features.ContactSubmissions.Commands.Submit;

public sealed class SubmitContactSubmissionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SubmitContactSubmissionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SubmitContactSubmissionCommand request, CancellationToken cancellationToken)
    {
        var submission = new ContactSubmission
        {
            Id = Guid.NewGuid(),
            Name = request.Input.Name,
            Phone = request.Input.Phone,
            Inquiry = request.Input.Inquiry
        };

        db.ContactSubmissions.Add(submission);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(submission.Id);
    }
}
