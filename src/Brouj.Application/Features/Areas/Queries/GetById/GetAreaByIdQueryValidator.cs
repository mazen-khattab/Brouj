using FluentValidation;

namespace Brouj.Application.Features.Areas.Queries.GetById;

public sealed class GetAreaByIdQueryValidator : AbstractValidator<GetAreaByIdQuery>
{
    public GetAreaByIdQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
