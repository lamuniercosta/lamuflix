using LamuFlix.Data;
using LamuFlix.Data.Models;
using LamuFlix.Web.Extensions;
using LamuFlix.Web.Models.Filmes;
using LamuFlix.Web.Models.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LamuFlix.Web.Services
{
    public interface IFilmesService
    {
        Task<PagedListing<FilmesListViewModel>> GetFilmesListAsync(QueryParams dtParams);

        IEnumerable<String> GetMoviesByName(string query);

        IEnumerable<FilmesListViewModel> QuickSearch(string query);

        Task<Movie?> GetDetalhesFilmeAsync(int filmeId);

        void AssistirFilme(int filmeId);

        void CriarFilme(CriarFilmeViewModel model);

        Movie? GetMovie(int filmeId);

        IEnumerable<Actor> GetActors();

        IEnumerable<Director> GetDirectors();

        IEnumerable<Genre> GetGenres();

        IEnumerable<Collection> GetCollections();

        void ExcluirFilme(int filmeId);

        void AddToWatchList(int filmeId);

        void RemoveFromWatchList(int filmeId);

        PagedListing<FilmesListViewModel> GetMinhaLista(QueryParams dtParams);
    }

    public class FilmesService : IFilmesService
    {
        private readonly LamuFlixContext _dataContext;
        private readonly IConfiguration? _configuration;
        private readonly IEnrichmentQueuePublisher? _queuePublisher;
        private static string _filePath = @"F:/Filmes";

        internal Func<ProcessStartInfo, Process?> ProcessStarter { get; set; } = Process.Start;

        public FilmesService(LamuFlixContext dataContext, IConfiguration? configuration = null, IEnrichmentQueuePublisher? queuePublisher = null)
        {
            _dataContext = dataContext;
            _configuration = configuration;
            _queuePublisher = queuePublisher;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        // nullable-column read; ! preserves the pre-DEV-360 contract (DEV-360 FR-004)
        public void AssistirFilme(int filmeId)
        {
            if (!(_configuration?.GetValue<bool>("Features:LocalPlay") ?? false))
            {
                throw new InvalidOperationException("LocalPlay is disabled.");
            }

            var filme = GetMovie(filmeId) ?? throw new InvalidOperationException("Filme não encontrado.");
            var player = ResolvePlayerPath(filme.Format!);
            var startInfo = BuildProcessStartInfo(player, filme.Location!);
            ProcessStarter(startInfo);
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        private string? ResolvePlayerPath(string format)
        {
            var configuredPath = _configuration?["Features:PlayerPath"];
            return !string.IsNullOrWhiteSpace(configuredPath) ? configuredPath : GetPlayer(format);
        }

        private static ProcessStartInfo BuildProcessStartInfo(string? player, string location)
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true
            };
            if (!string.IsNullOrWhiteSpace(player))
            {
                startInfo.FileName = player;
                startInfo.ArgumentList.Add(location);
            }
            else
            {
                startInfo.FileName = location;
            }
            return startInfo;
        }

        private string? GetPlayer(string format) =>
            string.IsNullOrEmpty(format)
                ? null
                : _dataContext.Players.SingleOrDefault(x => x.Formats != null && x.Formats.Contains(format))?.Path;

        public IEnumerable<Actor> GetActors() => _dataContext.Actors;

        public IEnumerable<Director> GetDirectors() => _dataContext.Directors;

        public IEnumerable<Collection> GetCollections() => _dataContext.Collections;

        public IEnumerable<Genre> GetGenres() => _dataContext.Genres;

        public Movie? GetMovie(int filmeId) => _dataContext.Movies.Find(filmeId);

        // ReSharper disable NullableWarningSuppressionIsUsed
        // nullable-column read; ! preserves the pre-DEV-360 contract (DEV-360 FR-004)
        public Task<Movie?> GetDetalhesFilmeAsync(int filmeId) => _dataContext.Movies
                .Include(x => x.Actors)
                .ThenInclude(x => x.Actor)
                .Include(x => x.Directors)
                .ThenInclude(x => x.Director)
                .Include(x => x.Genres)
                .ThenInclude(x => x.Genre)
                .Include(x => x.Collection)
                .ThenInclude(x => x!.Movies)
                .SingleOrDefaultAsync(x => x.Id == filmeId);
        // ReSharper restore NullableWarningSuppressionIsUsed

        // ReSharper disable NullableWarningSuppressionIsUsed
        // nullable-column read; ! preserves the pre-DEV-360 contract (DEV-360 FR-004)
        public async Task<PagedListing<FilmesListViewModel>> GetFilmesListAsync(QueryParams dtParams)
        {
            var queryResult = CreateFilmesListQuery(dtParams);
            var listResult = await queryResult.Query.Select(m =>
                new FilmesListViewModel
                {
                    Id = m.Id,
                    Title = m.Title!,
                    Duration = m.Duration,
                    IMDB = m.ImdbRating,
                    MetaScore = m.MetaScore,
                    RottenTomatoes = m.RottenTomatoes,
                    Year = m.Year,
                    Poster = m.Poster ?? string.Empty,
                    IsInWatchList = m.IsInWatchList
                }).ToListAsync();

            return new PagedListing<FilmesListViewModel>(listResult, queryResult.TotalRecords, dtParams.Page, dtParams.PageSize);
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        private QueryableResult<Movie> CreateFilmesListQuery(QueryParams dtParams)
        {
            QueryableResult<Movie> result = new QueryableResult<Movie>();

            var query = _dataContext.Movies
                .Include(x => x.Actors)
                .ThenInclude(x => x.Actor)
                .Include(x => x.Directors)
                .ThenInclude(x => x.Director)
                .Include(x => x.Genres)
                .ThenInclude(x => x.Genre)
                .Include(x => x.Collection)
                .OrderBy(x => x.Title)
                .AsNoTracking();

            var filter = dtParams.Filter as FilmesFilterViewModel;

            query = query.DynamicQuery(filter);

            // Save total records
            result.TotalRecords = query.Count();

            query = query.DynamicSort(filter, dtParams.SortBy, dtParams.SortOrder);

            // Paging
            result.Query = query.Skip(dtParams.PageSize * (dtParams.Page - 1)).Take(dtParams.PageSize);

            return result;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        // nullable-column read; ! preserves the pre-DEV-360 contract (DEV-360 FR-004)
        public void CriarFilme(CriarFilmeViewModel model)
        {
            var existingTitles = _dataContext.Movies.Select(x => x.Title!).ToList();

            if (!string.IsNullOrEmpty(model.MovieId))
            {
                ProcessMovieWithId(model);
            }
            else if (string.IsNullOrEmpty(model.Filme) && !string.IsNullOrEmpty(model.CollectionName))
            {
                ProcessCollectionDirectory(model, existingTitles);
            }
            else if (!string.IsNullOrEmpty(model.Filme) && !string.IsNullOrEmpty(model.CollectionName))
            {
                ProcessMovieInCollection(model, existingTitles);
            }
            else
            {
                ProcessStandaloneMovie(model, existingTitles);
            }
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        private void ProcessMovieWithId(CriarFilmeViewModel model)
        {
            var directory = new DirectoryInfo($"{_filePath}//{model.CollectionName}//{model.Filme}");
            ProcessarFilme(directory, model.CollectionName, model.MovieId);
        }

        private void ProcessCollectionDirectory(CriarFilmeViewModel model, List<string> existingTitles)
        {
            var caminhoCollection = new DirectoryInfo($"{_filePath}//{model.CollectionName}");
            foreach (var item in caminhoCollection.EnumerateDirectories())
            {
                EnsureMovieNotRegistered(item, existingTitles);
                ProcessarFilme(item, model.CollectionName, null);
            }
        }

        private void ProcessMovieInCollection(CriarFilmeViewModel model, List<string> existingTitles)
        {
            var directory = new DirectoryInfo($"{_filePath}//{model.CollectionName}//{model.Filme}");
            EnsureMovieNotRegistered(directory, existingTitles);
            ProcessarFilme(directory, model.CollectionName, null);
        }

        private void ProcessStandaloneMovie(CriarFilmeViewModel model, List<string> existingTitles)
        {
            var directory = new DirectoryInfo($"{_filePath}//{model.Filme}");
            EnsureMovieNotRegistered(directory, existingTitles);
            ProcessarFilme(directory, null, null);
        }

        private static void EnsureMovieNotRegistered(DirectoryInfo directory, List<string> existingTitles)
        {
            var movieName = directory.Name.Split('[').ElementAt(0);
            if (existingTitles.Contains(movieName))
            {
                throw new Exception("Filme já cadastrado.");
            }
        }

        private void ProcessarFilme(DirectoryInfo directory, string? collection, string? movieId)
        {
            var (movieName, movieYear) = ParseDirectoryInfo(directory);
            var file = GetMovieFile(directory);
            var movieModel = CreateMovieModel(directory, file, movieName, movieYear);

            AssignCollection(movieModel, collection);
            SaveMovie(movieModel);
            PublishEnrichment(movieModel, movieId);
        }

        private static (string name, int year) ParseDirectoryInfo(DirectoryInfo directory)
        {
            var parts = directory.Name.Split('[');
            var name = parts[0];
            var year = parts.Length > 1 ? Convert.ToInt32(parts[1].Replace("]", "")) : 0;
            return (name, year);
        }

        private static FileInfo? GetMovieFile(DirectoryInfo directory)
        {
            return directory.GetFiles().SingleOrDefault(x => !x.Extension.Equals(".srt") && !x.Extension.Equals(".sub"));
        }

        private static Movie CreateMovieModel(DirectoryInfo directory, FileInfo? file, string name, int year)
        {
            var location = file != null ? file.FullName : directory.FullName;
            var format = file != null ? file.Extension : string.Empty;
            return new Movie
            {
                Title = name,
                Synopsis = "Pending",
                Year = year,
                Location = location,
                Format = format,
                Status = MovieEnrichmentStatus.Pending
            };
        }

        private void SaveMovie(Movie movieModel)
        {
            _dataContext.Add(movieModel);
            _dataContext.SaveChanges();
        }

        private void AssignCollection(Movie movieModel, string? collection)
        {
            if (string.IsNullOrEmpty(collection)) return;

            var collectionModel = _dataContext.Collections.SingleOrDefault(x => x.Name == collection.Trim());
            if (collectionModel == null)
            {
                collectionModel = new Collection
                {
                    Name = collection.Trim()
                };
                _dataContext.Add(collectionModel);
            }

            movieModel.Collection = collectionModel;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        // DI-injected configuration; nullable-column read preserving the pre-DEV-360 contract (DEV-360 FR-004)
        private void PublishEnrichment(Movie movieModel, string? movieId)
        {
            var publisher = _queuePublisher ?? new RabbitMqEnrichmentQueuePublisher(_configuration!);
            publisher.PublishAsync(new MovieEnrichmentMessage
            {
                MovieId = movieModel.Id,
                Title = movieModel.Title!,
                Year = movieModel.Year,
                ImdbId = movieId
            }).GetAwaiter().GetResult();
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        // ReSharper disable NullableWarningSuppressionIsUsed
        // nullable-column read; ! preserves the pre-DEV-360 contract (DEV-360 FR-004)
        public IEnumerable<string> GetMoviesByName(string query) => [.. _dataContext.Movies.Where(x => x.Title!.Contains(query)).Select(x => x.Title!)];
        // ReSharper restore NullableWarningSuppressionIsUsed

        public void ExcluirFilme(int filmeId)
        {
            var filme = _dataContext.Movies.Find(filmeId);
            if (filme != null)
            {
                _dataContext.Movies.Remove(filme);
                _dataContext.SaveChanges();
            }
        }

        public void AddToWatchList(int filmeId)
        {
            var movie = _dataContext.Movies.Find(filmeId);
            if (movie != null)
            {
                movie.IsInWatchList = true;
                _dataContext.Update(movie);
                _dataContext.SaveChanges();
            }
        }

        public void RemoveFromWatchList(int filmeId)
        {
            var movie = _dataContext.Movies.Find(filmeId);
            if (movie != null)
            {
                movie.IsInWatchList = false;
                _dataContext.Update(movie);
                _dataContext.SaveChanges();
            }
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        // nullable-column read; ! preserves the pre-DEV-360 contract (DEV-360 FR-004)
        public PagedListing<FilmesListViewModel> GetMinhaLista(QueryParams dtParams)
        {
            QueryableResult<Movie> result = new QueryableResult<Movie>();

            var query = _dataContext.Movies
                .Include(x => x.Actors)
                .ThenInclude(x => x.Actor)
                .Include(x => x.Directors)
                .ThenInclude(x => x.Director)
                .Include(x => x.Genres)
                .ThenInclude(x => x.Genre)
                .Include(x => x.Collection)
                .Where(x => x.IsInWatchList)
                .OrderBy(x => x.Title)
                .AsNoTracking();

            query = query.DynamicSort(dtParams.Filter, dtParams.SortBy, dtParams.SortOrder);

            // Paging
            result.TotalRecords = query.Count();

            result.Query = query.Skip(dtParams.PageSize * (dtParams.Page - 1)).Take(dtParams.PageSize);

            var listResult = result.Query.Select(m =>
                new FilmesListViewModel
                {
                    Id = m.Id,
                    Title = m.Title!,
                    Duration = m.Duration,
                    IMDB = m.ImdbRating,
                    MetaScore = m.MetaScore,
                    RottenTomatoes = m.RottenTomatoes,
                    Year = m.Year,
                    Poster = m.Poster ?? string.Empty,
                    IsInWatchList = m.IsInWatchList
                }).ToList();

            return new PagedListing<FilmesListViewModel>(listResult, result.TotalRecords, dtParams.Page, dtParams.PageSize);
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        // ReSharper disable NullableWarningSuppressionIsUsed
        // nullable-column read; ! preserves the pre-DEV-360 contract (DEV-360 FR-004)
        public IEnumerable<FilmesListViewModel> QuickSearch(string query) => [.. _dataContext.Movies.Where(x => x.Title!.Contains(query)).Select(x => new FilmesListViewModel { Id = x.Id, Title = x.Title!, Poster = x.Poster ?? string.Empty })];
        // ReSharper restore NullableWarningSuppressionIsUsed
    }

}
