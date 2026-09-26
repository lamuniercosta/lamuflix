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

namespace LamuFlix.Web.Controllers;

public class MoviesController(IMovieService movieService, TimeProvider? timeProvider = null)
    : Controller
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<IActionResult> Index([FromQuery] MoviesFilterViewModel filter, QueryParams parms)
    {
        ViewBag.Year = new SelectList(Enumerable.Range(1900, (_timeProvider.GetUtcNow().Year - 1899)).OrderByDescending(x => x).Select(x => new SelectListItem { Value = x.ToString(), Text = x.ToString() }), "Text", "Value", filter.Year);
        ViewBag.DirectorId = new SelectList(movieService.GetDirectors(), "Id", "Name", filter.DirectorId);
        ViewBag.CollectionId = new SelectList(movieService.GetCollections(), "Id", "Name", filter.CollectionId);
        ViewBag.GenreIds = new MultiSelectList(movieService.GetGenres(), "Id", "Name", filter.GenreIds);
        ViewBag.ActorIds = new MultiSelectList(movieService.GetActors(), "Id", "Name", filter.ActorIds);

        parms.Filter = filter;

        if (!string.IsNullOrEmpty(parms.SortBy))
        {
            if (!string.IsNullOrEmpty(parms.SortOrder) && parms.SortOrder.Contains(GeneralConstants.Descending))
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

        var model = await movieService.GetMoviesListAsync(parms);

        ViewData["CurrentFilter"] = parms.Filter;
        ViewData["CurrentSort"] = parms.SortBy;
        ViewData["CurrentSortOrder"] = parms.SortOrder;

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await movieService.GetMovieDetails(id);

        if (null == model)
            return NotFound();

        return View(model);
    }

    public IActionResult PlayMovie(int id)
    {
        try
        {
            movieService.PlayMovie(id);
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
            if (!(string.IsNullOrEmpty(model.Movie)) || !(string.IsNullOrEmpty(model.CollectionName)) || !(string.IsNullOrEmpty(model.CollectionName)))
            {
                movieService.ImportMovieFolder(model);
                ViewBag.alerts = new AlertModel { Type = GeneralConstants.Success, Text = "Registro inserido com sucesso" };
                ModelState.Clear();
                return View();
            }
            else
            {
                ViewBag.alerts = new AlertModel { Type = GeneralConstants.Error, Text = "Pelo menos um dos campos precisa ser preenchido" };
            }
        }
        catch (Exception ex)
        {
            ViewBag.alerts = new AlertModel { Type = GeneralConstants.Error, Text = ex.Message };
        }
        return View(model);
    }

    public IActionResult GetMoviesJson(string query) => Json(new { movies = movieService.GetMoviesByName(query) });

    public IActionResult QuickSearch(string query) => Json(new { movies = movieService.QuickSearch(query) });

    [HttpPost]
    public IActionResult DeleteMovie(int id)
    {
        try
        {
            movieService.DeleteMovie(id);
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
            movieService.AddToWatchList(id);
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
            movieService.RemoveFromWatchList(id);
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
        if (!string.IsNullOrEmpty(parms.SortBy))
        {
            if (!string.IsNullOrEmpty(parms.SortOrder) && parms.SortOrder.Contains(GeneralConstants.Descending))
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

        var model = movieService.GetWatchlist(parms);

        ViewData["CurrentFilter"] = parms.Filter;
        ViewData["CurrentSort"] = parms.SortBy;
        ViewData["CurrentSortOrder"] = parms.SortOrder;

        return View(model);
    }
}