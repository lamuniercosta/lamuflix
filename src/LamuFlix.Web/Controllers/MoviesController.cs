using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Data.Constants;
using LamuFlix.Web.Models;
using LamuFlix.Web.Models.Movies;
using LamuFlix.Web.Models.Helper;
using LamuFlix.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LamuFlix.Web.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IMovieService MovieService;
        private readonly TimeProvider _timeProvider;

        public MoviesController(IMovieService movieService, TimeProvider? timeProvider = null)
        {
            MovieService = movieService;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<IActionResult> Index([FromQuery] MoviesFilterViewModel filter, QueryParams parms)
        {
            ViewBag.Year = new SelectList(Enumerable.Range(1900, (_timeProvider.GetUtcNow().Year - 1899)).OrderByDescending(x => x).Select(x => new SelectListItem { Value = x.ToString(), Text = x.ToString() }), "Text", "Value", filter.Year);
            ViewBag.DirectorId = new SelectList(MovieService.GetDirectors(), "Id", "Name", filter.DirectorId);
            ViewBag.CollectionId = new SelectList(MovieService.GetCollections(), "Id", "Name", filter.CollectionId);
            ViewBag.GenreIds = new MultiSelectList(MovieService.GetGenres(), "Id", "Name", filter.GenreIds);
            ViewBag.ActorIds = new MultiSelectList(MovieService.GetActors(), "Id", "Name", filter.ActorIds);

            parms.Filter = filter;

            if (!String.IsNullOrEmpty(parms.SortBy))
            {
                if (!String.IsNullOrEmpty(parms.SortOrder) && parms.SortOrder.Contains(GeneralConstants.Descending))
                {
                    ViewData["SortOrder"] = "";
                }
                else
                {
                    ViewData["SortOrder"] = GeneralConstants.Descending;
                }
            }
            else
            {
                parms.SortBy = GeneralConstants.Id;
                parms.SortOrder = GeneralConstants.Descending;
            }

            var model = await MovieService.GetMoviesListAsync(parms);

            ViewData["CurrentFilter"] = parms.Filter;
            ViewData["CurrentSort"] = parms.SortBy;
            ViewData["CurrentSortOrder"] = parms.SortOrder;

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var model = await MovieService.GetMovieDetails(id);

            if (null == model)
                return NotFound();

            return View(model);
        }

        public IActionResult PlayMovie(int id)
        {
            try
            {
                MovieService.PlayMovie(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }

        public IActionResult ImportMovieFolder()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ImportMovieFolder(ImportMovieFolderViewModel model)
        {
            try
            {
                if (!(String.IsNullOrEmpty(model.Filme)) || !(String.IsNullOrEmpty(model.CollectionName)) || !(String.IsNullOrEmpty(model.CollectionName)))
                {
                    MovieService.ImportMovieFolder(model);
                    ViewBag.alerts = new AlertModel { Type = GeneralConstants.SUCCESS, Text = "Registro inserido com sucesso" };
                    ModelState.Clear();
                    return View();
                }
                else
                {
                    ViewBag.alerts = new AlertModel { Type = GeneralConstants.ERROR, Text = "Pelo menos um dos campos precisa ser preenchido" };
                }
            }
            catch (Exception ex)
            {
                ViewBag.alerts = new AlertModel { Type = GeneralConstants.ERROR, Text = ex.Message };
            }
            return View(model);
        }

        public IActionResult GetMoviesJson(string query) => Json(new { movies = MovieService.GetMoviesByName(query) });

        public IActionResult QuickSearch(string query) => Json(new { movies = MovieService.QuickSearch(query) });

        [HttpPost]
        public IActionResult DeleteMovie(int id)
        {
            try
            {
                MovieService.DeleteMovie(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult AddToWatchList(int id)
        {
            try
            {
                MovieService.AddToWatchList(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult RemoveFromWatchList(int id)
        {
            try
            {
                MovieService.RemoveFromWatchList(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("/Watchlist/")]
        public IActionResult Watchlist(QueryParams parms)
        {
            if (!String.IsNullOrEmpty(parms.SortBy))
            {
                if (!String.IsNullOrEmpty(parms.SortOrder) && parms.SortOrder.Contains(GeneralConstants.Descending))
                {
                    ViewData["SortOrder"] = "";
                }
                else
                {
                    ViewData["SortOrder"] = GeneralConstants.Descending;
                }
            }
            else
            {
                parms.SortBy = GeneralConstants.Id;
                parms.SortOrder = GeneralConstants.Descending;
            }

            var model = MovieService.GetWatchlist(parms);

            ViewData["CurrentFilter"] = parms.Filter;
            ViewData["CurrentSort"] = parms.SortBy;
            ViewData["CurrentSortOrder"] = parms.SortOrder;

            return View(model);
        }
    }
}