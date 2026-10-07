using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class FacetEndpoints
{
    private const string RoleKey = "role";
    private const string ActorRole = "actor";

    internal static RouteGroupBuilder MapFacetEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/genres", GetGenresAsync)
            .WithName("GetGenres")
            .WithSummary("List the stored genres.")
            .Produces<GenreDto[]>();

        api.MapGet("/people", GetPeopleAsync)
            .WithName("GetPeople")
            .WithSummary("List the stored actor people.")
            .Produces<PersonDto[]>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async Task<Ok<GenreDto[]>> GetGenresAsync(
        IQueryHandler<GetGenresQuery, IReadOnlyList<GenreFacet>> handler,
        CancellationToken cancellationToken)
    {
        var genres = await handler.HandleAsync(new GetGenresQuery(), cancellationToken);
        return TypedResults.Ok(genres.Select(genre => new GenreDto(genre.Id, genre.Name)).ToArray());
    }

    private static async Task<Ok<PersonDto[]>> GetPeopleAsync(
        HttpRequest httpRequest,
        IQueryHandler<GetPeopleQuery, IReadOnlyList<PersonFacet>> handler,
        CancellationToken cancellationToken)
    {
        EnsureActorRole(httpRequest.Query);

        var people = await handler.HandleAsync(new GetPeopleQuery(), cancellationToken);
        return TypedResults.Ok(people.Select(person => new PersonDto(person.Id, person.Name)).ToArray());
    }

    private static void EnsureActorRole(IQueryCollection query)
    {
        if (!query.TryGetValue(RoleKey, out var values) || values.Count == 0)
        {
            throw RoleError("'role' is required.");
        }

        if (values.Count > 1)
        {
            throw RoleError("'role' must be supplied at most once.");
        }

        var value = values[0];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw RoleError("'role' must not be empty.");
        }

        if (!string.Equals(value, ActorRole, StringComparison.Ordinal))
        {
            throw RoleError("'role' must be 'actor'.");
        }
    }

    private static ValidationException RoleError(string message) =>
        new(new Dictionary<string, string[]> { [RoleKey] = [message] });
}
