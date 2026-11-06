using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Feedback;

public sealed class SetMovieReviewCommandHandler(IMovieRepository movies)
    : ICommandHandler<SetMovieReviewCommand, Unit>
{
    private const string ReviewField = "review";
    private const string InvalidReviewMessage = "Review must be 'none', 'liked' or 'disliked'.";

    public async Task<Unit> HandleAsync(SetMovieReviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Review.TryParse(command.Review, out var review))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [ReviewField] = [InvalidReviewMessage],
            });
        }

        var movie = await movies.GetAsync(command.Id, cancellationToken)
                    ?? throw new NotFoundException();
        movie.AssignReview(review);
        await movies.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
