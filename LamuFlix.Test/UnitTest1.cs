using LamuFlix.Data;
using LamuFlix.Data.Models;
using LamuFlix.Web.Extensions;
using LamuFlix.Web.Models.Filmes;
using LamuFlix.Web.Models.Helper;
using LamuFlix.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace LamuFlix.Test
{
    [TestClass]
    public class UnitTest1
    {
        private static string FilePath = @"F:/Filmes/";
        private static string ApiKey => Environment.GetEnvironmentVariable("LAMUFLIX_OMDB_API_KEY") ?? string.Empty;

        private LamuFlixContext? _lazyDataContext;

        private LamuFlixContext _dataContext
        {
            get
            {
                if (_lazyDataContext is not null)
                {
                    return _lazyDataContext;
                }

                var connectionString = Environment.GetEnvironmentVariable("LAMUFLIX_TEST_CONNECTION");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    Assert.Inconclusive("LAMUFLIX_TEST_CONNECTION is not set.");
                }

                var services = new ServiceCollection();
                services.AddDbContext<LamuFlixContext>(opts => opts.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 31)), s => s.MigrationsAssembly("LamuFlix.Data")));
                _lazyDataContext = services.BuildServiceProvider().GetRequiredService<LamuFlixContext>();
                return _lazyDataContext;
            }
        }

        [TestMethod]
        [Ignore("Requires local MySQL server and media directory")]
        public void TestMethod1()
        {
            var movies = _dataContext.Movies.OrderBy(x => x.Id);
            DirectoryInfo rootDirectory = new DirectoryInfo(FilePath);
            foreach (var directory in rootDirectory.EnumerateDirectories())
            {
                if (IsCollection(directory.Name))
                {

                    DirectoryInfo subDirectory = new DirectoryInfo(directory.FullName);
                    foreach (var item in subDirectory.EnumerateDirectories())
                    {
                        var movieName = item.Name.Split('[').ElementAt(0);
                        if (!movies.Select(x => x.Title).Contains(movieName))
                        {
                            ProcessMovie(item, directory.Name);
                        }

                    }

                }
                else
                {
                    var movieName = directory.Name.Split('[').ElementAt(0);
                    if (!movies.Select(x => x.Title).Contains(movieName))
                    {
                        ProcessMovie(directory, null);
                    }
                }

                //var filename = directory.Name.Replace(directory.Extension,"");
                //string newDirectory = $"{FilePath}//{filename}";
                //if (!Directory.Exists(newDirectory))
                //{                    
                //    Directory.CreateDirectory(newDirectory);
                //    File.Move(directory.FullName, $"{newDirectory}//{directory.Name}");
                //}
            }
        }

        [TestMethod]
        [Ignore("Requires local MySQL server and media directory")]
        public void TestMethod2()
        {
            var movies = _dataContext.Movies.OrderBy(x => x.Id);
            DirectoryInfo directory = new DirectoryInfo(@"F://Filmes//Love & Mercy[2014]");

            if (IsCollection(directory.Name))
            {
                DirectoryInfo subDirectory = new DirectoryInfo(directory.FullName);
                foreach (var item in subDirectory.EnumerateDirectories())
                {
                    var movieName = item.Name.Split('[').ElementAt(0);
                    if (!movies.Select(x => x.Title).Contains(movieName))
                    {
                        ProcessMovie(item, directory.Name);
                    }

                }

            }
            else
            {
                var movieName = directory.Name.Split('[').ElementAt(0);
                if (!movies.Select(x => x.Title).Contains(movieName))
                {
                    ProcessMovie(directory, null);
                }

            }

            //var filename = directory.Name.Replace(directory.Extension,"");
            //string newDirectory = $"{FilePath}//{filename}";
            //if (!Directory.Exists(newDirectory))
            //{                    
            //    Directory.CreateDirectory(newDirectory);
            //    File.Move(directory.FullName, $"{newDirectory}//{directory.Name}");
            //}

        }


        private static readonly JsonSerializerOptions OmdbJsonOptions = new()
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
        };

        // ReSharper disable NullableWarningSuppressionIsUsed
        // deserialized result
        private void ProcessMovie(DirectoryInfo directory, string? collection)
        {
            var movie = directory.Name.Split('[');
            var movieName = movie[0];
            var movieYear = movie[1].Replace("]", "");

            var file = directory.GetFiles().SingleOrDefault(x => !x.Extension.Equals(".srt") && !x.Extension.Equals(".sub"));

            //string apiURL = $"http://www.omdbapi.com/?apikey={ApiKey}&t={movieName}&y{movieYear}";
            string apiURL = $"http://www.omdbapi.com/?apikey={ApiKey}&i=tt0903657";

            var retorno = JsonSerializer.Deserialize<ApiDataModel>(GET(apiURL), OmdbJsonOptions)!;

            Movie movieModel = retorno.Response == "True"
                ? BuildMovieFromApiResponse(retorno, movieName, movieYear, file!)
                : BuildFallbackMovie(movieName, movieYear, file!);

            if (!String.IsNullOrEmpty(collection))
            {
                AssignCollectionToMovie(movieModel, collection);
            }

            _dataContext.Add(movieModel);
            _dataContext.SaveChanges();
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        private Movie BuildMovieFromApiResponse(ApiDataModel retorno, string movieName, string movieYear, FileInfo file)
        {
            var rottenTomatoes = ExtractRottenTomatoesRating(retorno.Ratings);

            var movieModel = new Movie
            {
                Title = movieName,
                Synopsis = retorno.Plot,
                Year = Convert.ToInt32(movieYear),
                Duration = Convert.ToInt32(retorno.Runtime.Replace(" min", "")),
                Poster = retorno.Poster,
                ImdbRating = Convert.ToDecimal(retorno.imdbRating),
                RottenTomatoes = Convert.ToInt32(rottenTomatoes.Replace("%", "")),
                MetaScore = Convert.ToInt32(retorno.Metascore.Replace("N/A", "0")),
                Location = file.FullName,
                Format = file.Extension,
                IdImdb = retorno.imdbID
            };

            AddActorsFromApi(movieModel, retorno.Actors);
            AddGenresFromApi(movieModel, retorno.Genre);
            AddDirectorsFromApi(movieModel, retorno.Director);

            return movieModel;
        }

        private static Movie BuildFallbackMovie(string movieName, string movieYear, FileInfo file)
        {
            return new Movie
            {
                Title = movieName,
                Synopsis = "N/A",
                Year = Convert.ToInt32(movieYear),
                Location = file.FullName,
                Format = file.Extension
            };
        }

        private static string ExtractRottenTomatoesRating(IList<RatingsModel> ratings)
        {
            foreach (var item in ratings)
            {
                if (item.Source == "Rotten Tomatoes")
                {
                    return item.Value;
                }
            }

            return "";
        }

        private void AddActorsFromApi(Movie movieModel, string actors)
        {
            if (String.IsNullOrEmpty(actors))
            {
                return;
            }

            foreach (var actor in actors.Split(','))
            {
                var actorModel = _dataContext.Actors.SingleOrDefault(x => x.Name == actor.Trim());
                if (actorModel == null)
                {
                    actorModel = new Actor
                    {
                        Name = actor.Trim()
                    };

                    _dataContext.Add(actorModel);
                }

                MovieActors movieActors = new MovieActors
                {
                    Movie = movieModel,
                    Actor = actorModel
                };
                _dataContext.Add(movieActors);
            }
        }

        private void AddGenresFromApi(Movie movieModel, string genre)
        {
            if (String.IsNullOrEmpty(genre))
            {
                return;
            }

            foreach (var genreName in genre.Split(','))
            {
                var genreModel = _dataContext.Genres.SingleOrDefault(x => x.Name == genreName.Trim());
                if (genreModel == null)
                {
                    genreModel = new Genre
                    {
                        Name = genreName.Trim()
                    };

                    _dataContext.Add(genreModel);
                }

                MovieGenre movieGenres = new MovieGenre
                {
                    Movie = movieModel,
                    Genre = genreModel
                };
                _dataContext.Add(movieGenres);
            }
        }

        private void AddDirectorsFromApi(Movie movieModel, string director)
        {
            if (String.IsNullOrEmpty(director))
            {
                return;
            }

            foreach (var directorName in director.Split(','))
            {
                var directorModel = _dataContext.Directors.SingleOrDefault(x => x.Name == directorName.Trim());
                if (directorModel == null)
                {
                    directorModel = new Director
                    {
                        Name = directorName.Trim()
                    };

                    _dataContext.Add(directorModel);
                }

                MovieDirectors movieDirectors = new MovieDirectors
                {
                    Movie = movieModel,
                    Director = directorModel
                };
                _dataContext.Add(movieDirectors);
            }
        }

        private void AssignCollectionToMovie(Movie movieModel, string collection)
        {
            Collection? collectionModel = _dataContext.Collections.SingleOrDefault(x => x.Name == collection.Trim());

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

        private static readonly HttpClient SharedHttpClient = new();

        private static string GET(string url)
        {
            return SharedHttpClient.GetStringAsync(url).GetAwaiter().GetResult();
        }

        private static bool IsCollection(string directoryName)
        {
            if (directoryName.Contains("Collection"))
            {
                return true;
            }
            return false;

        }

        [TestMethod]
        [Ignore("Requires local BSPlayer installation and media file")]
        public void TestMethod4()
        {
            var player = @"D:\Program Files (x86)\Webteh\BSPlayer\bsplayer.exe";
            var movie = @"F:\Filmes\2 Days in New York[2012]\2.Days.In.New.York.2012.BRRip.XviD.AC3-BTRG.avi";

            var startInfo = new ProcessStartInfo
            {
                FileName = player,
                UseShellExecute = true
            };
            startInfo.ArgumentList.Add(movie);

            Process.Start(startInfo);
        }

        [TestMethod]
        [Ignore("Requires local MySQL server")]
        public void TestMethod5()
        {
            //QueryParams dtParams = new QueryParams
            //{
            //    PageSize = 16,
            //    SortBy = "Id",
            //    SortOrder = "desc",
            //    Filter = new FilmesFilterViewModel()
            //};

            QueryParams dtParams = new QueryParams
            {
                PageSize = 16,
                SortBy = "Id",
                SortOrder = "desc",
                Filter = new FilmesFilterViewModel
                {
                    Year = 2018,
                    SearchField = "man",
                    GenreIds = new List<int> { 1, 2, 4 }
                }
            };

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

            query = DynamicQuery(dtParams, query);

            var totalResults = query.Count();

            Assert.AreNotEqual(totalResults, 0);
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        // reflection lookup of a known member
        private IQueryable<T> DynamicQuery<T>(QueryParams dtParams, IQueryable<T> query)
        {
            var filter = dtParams.Filter;
            var filterHasValue = filter.HasQuery();

            if (filterHasValue)
            {
                var filterProperties = filter.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in filterProperties)
                {
                    if (filter.HasPropertyValue(property.Name))
                    {
                        var item = Expression.Parameter(typeof(T), "item");

                        object attr = property.GetCustomAttributes(true).ElementAt(0);
                        DataMapping map = (DataMapping)attr;

                        var prop = Expression.PropertyOrField(item, map.GetDestination());
                        var propType = prop.Type;
                        var value = Expression.Constant(property.GetValue(filter));
                        var queryConstExpr = Expression.Constant(query);

                        BinaryExpression equal;
                        Expression<Func<IQueryable<T>>> lambda;
                        LambdaExpression lambda1;

                        if (propType.GetInterface(nameof(IEnumerable)) != null && propType != typeof(String))
                        {
                            var innerType = propType.GetGenericArguments().FirstOrDefault()!;
                            var innerItem = Expression.Parameter(innerType, "innerItem");
                            var innerProp = Expression.PropertyOrField(innerItem, map.GetKey()!);
                            var innerPropType = innerProp.Type;
                            if (property.PropertyType.GetInterface(nameof(IEnumerable)) != null && property.PropertyType != typeof(String))
                            {
                                var containsIntMethod = typeof(Enumerable).GetMethods().Where(x => x.Name == "Contains").Single(x => x.GetParameters().Length == 2).MakeGenericMethod(typeof(int));
                                var containsExpCall = Expression.Call(containsIntMethod, value, innerProp);
                                lambda1 = Expression.Lambda(containsExpCall, innerItem);
                            }
                            else
                            {
                                equal = Expression.Equal(innerProp, Expression.Convert(value, innerPropType));
                                lambda1 = Expression.Lambda(equal, innerItem);
                            }

                            var anyMethod = typeof(Enumerable).GetTypeInfo().GetMethods().First(m => m.Name == "Any" && m.GetParameters().Count() == 2).MakeGenericMethod(innerType);
                            var anyCallExp = Expression.Call(anyMethod, prop, lambda1);
                            var lambda2 = Expression.Lambda<Func<T, bool>>(anyCallExp, item);

                            var whereExp = Expression.Call(typeof(Queryable), "Where", [typeof(T)], queryConstExpr, lambda2);
                            lambda = Expression.Lambda<Func<IQueryable<T>>>(whereExp);
                            var resultFunc = lambda.Compile();
                            query = resultFunc();
                        }
                        else
                        {
                            if (property.PropertyType == typeof(String))
                            {
                                var containsStringMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                                var toLowerMethod = typeof(string).GetTypeInfo().GetMethods().First(m => m.Name == "ToLower" && m.GetParameters().Count() == 0);
                                var toLowerPropCall = Expression.Call(prop, toLowerMethod);
                                var toLowerValueCall2 = Expression.Call(value, toLowerMethod);
                                var containsExpCall = Expression.Call(toLowerPropCall, containsStringMethod, toLowerValueCall2);
                                lambda1 = Expression.Lambda(containsExpCall, item);
                            }
                            else
                            {
                                equal = Expression.Equal(prop, Expression.Convert(value, propType));
                                lambda1 = Expression.Lambda(equal, item);
                            }
                            var whereExp = Expression.Call(typeof(Queryable), "Where", [typeof(T)], queryConstExpr, lambda1);
                            lambda = Expression.Lambda<Func<IQueryable<T>>>(whereExp);
                            var resultFunc = lambda.Compile();
                            query = resultFunc();
                        }
                    }
                }

            }

            return query;
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        private static LamuFlixContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<LamuFlixContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new LamuFlixContext(options);
        }

        [TestMethod]
        public void AssistirFilme_WhenLocalPlayDisabled_ThrowsInvalidOperationException()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "false"
                })
                .Build();

            using (var context = CreateInMemoryContext())
            {
                var service = new FilmesService(context, config);

                var ex = Assert.ThrowsException<InvalidOperationException>(() => service.AssistirFilme(1));
                Assert.AreEqual("LocalPlay is disabled.", ex.Message);
            }
        }

        [TestMethod]
        public void AssistirFilme_WhenLocalPlayEnabledAndPlayerConfigured_StartsProcessWithConfiguredPlayerAndArgumentList()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "true",
                    ["Features:PlayerPath"] = @"C:\Players\vlc.exe"
                })
                .Build();

            using (var context = CreateInMemoryContext())
            {
                var movie = new Movie
                {
                    Id = 1,
                    Title = "Test Movie",
                    Location = @"F:\Filmes\Test[2020]\test.mkv",
                    Format = ".mkv"
                };
                context.Movies.Add(movie);
                context.SaveChanges();

                ProcessStartInfo? capturedStartInfo = null;
                var service = new FilmesService(context, config)
                {
                    ProcessStarter = psi =>
                    {
                        capturedStartInfo = psi;
                        return null;
                    }
                };

                service.AssistirFilme(1);

                Assert.IsNotNull(capturedStartInfo);
                Assert.IsTrue(capturedStartInfo.UseShellExecute);
                Assert.AreEqual(@"C:\Players\vlc.exe", capturedStartInfo.FileName);
                Assert.AreEqual(1, capturedStartInfo.ArgumentList.Count);
                Assert.AreEqual(movie.Location, capturedStartInfo.ArgumentList[0]);
            }
        }

        [TestMethod]
        public void AssistirFilme_WhenLocalPlayEnabledAndPlayerNull_DefaultsToOsAssociation()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "true"
                })
                .Build();

            using (var context = CreateInMemoryContext())
            {
                var movie = new Movie
                {
                    Id = 2,
                    Title = "Test Movie OS Assoc",
                    Location = @"F:\Filmes\Test[2020]\test.mp4",
                    Format = ".unmappedformat"
                };
                context.Movies.Add(movie);
                context.SaveChanges();

                ProcessStartInfo? capturedStartInfo = null;
                var service = new FilmesService(context, config)
                {
                    ProcessStarter = psi =>
                    {
                        capturedStartInfo = psi;
                        return null;
                    }
                };

                service.AssistirFilme(2);

                Assert.IsNotNull(capturedStartInfo);
                Assert.IsTrue(capturedStartInfo.UseShellExecute);
                Assert.AreEqual(movie.Location, capturedStartInfo.FileName);
                Assert.AreEqual(0, capturedStartInfo.ArgumentList.Count);
            }
        }
    }
}
