using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Pipeline;

public sealed class ImdbIdConflictException : Exception
{
    public ImdbIdConflictException(ImdbId imdbId, MovieId heldBy)
        : base($"IMDb id {imdbId.Value} is already assigned to movie {heldBy.Value}.")
    {
        ImdbId = imdbId;
        HeldBy = heldBy;
    }

    public ImdbId ImdbId { get; }

    public MovieId HeldBy { get; }
}
