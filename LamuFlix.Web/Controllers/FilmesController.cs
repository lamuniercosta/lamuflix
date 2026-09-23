using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Data.Constants;
using LamuFlix.Web.Models;
using LamuFlix.Web.Models.Filmes;
using LamuFlix.Web.Models.Helper;
using LamuFlix.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LamuFlix.Web.Controllers
{
    public class FilmesController : Controller
    {
        private readonly IFilmesService FilmesService;
        private readonly TimeProvider _timeProvider;

        public FilmesController(IFilmesService filmesService, TimeProvider? timeProvider = null)
        {
            FilmesService = filmesService;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<IActionResult> Index([FromQuery] FilmesFilterViewModel filter, QueryParams parms)
        {
            ViewBag.Year = new SelectList(Enumerable.Range(1900, (_timeProvider.GetUtcNow().Year - 1899)).OrderByDescending(x => x).Select(x => new SelectListItem { Value = x.ToString(), Text = x.ToString() }), "Text", "Value", filter.Year);
            ViewBag.DirectorId = new SelectList(FilmesService.GetDirectors(), "Id", "Name", filter.DirectorId);
            ViewBag.CollectionId = new SelectList(FilmesService.GetCollections(), "Id", "Name", filter.CollectionId);
            ViewBag.GenreIds = new MultiSelectList(FilmesService.GetGenres(), "Id", "Name", filter.GenreIds);
            ViewBag.ActorIds = new MultiSelectList(FilmesService.GetActors(), "Id", "Name", filter.ActorIds);

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

            var model = await FilmesService.GetFilmesListAsync(parms);

            ViewData["CurrentFilter"] = parms.Filter;
            ViewData["CurrentSort"] = parms.SortBy;
            ViewData["CurrentSortOrder"] = parms.SortOrder;

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var model = await FilmesService.GetDetalhesFilmeAsync(id);

            if (null == model)
                return NotFound();

            return View(model);
        }

        public IActionResult AssistirFilme(int id)
        {
            try
            {
                FilmesService.AssistirFilme(id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok();
        }

        public IActionResult CriarFilme()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CriarFilme(CriarFilmeViewModel model)
        {
            try
            {
                if (!(String.IsNullOrEmpty(model.Filme)) || !(String.IsNullOrEmpty(model.CollectionName)) || !(String.IsNullOrEmpty(model.CollectionName)))
                {
                    FilmesService.CriarFilme(model);
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

        public IActionResult GetFilmesJson(string query) => Json(new { filmes = FilmesService.GetMoviesByName(query) });

        public IActionResult QuickSearch(string query) => Json(new { filmes = FilmesService.QuickSearch(query) });

        [HttpPost]
        public IActionResult ExcluirFilme(int id)
        {
            try
            {
                FilmesService.ExcluirFilme(id);
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
                FilmesService.AddToWatchList(id);
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
                FilmesService.RemoveFromWatchList(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("/MinhaLista/")]
        public IActionResult MinhaLista(QueryParams parms)
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

            var model = FilmesService.GetMinhaLista(parms);

            ViewData["CurrentFilter"] = parms.Filter;
            ViewData["CurrentSort"] = parms.SortBy;
            ViewData["CurrentSortOrder"] = parms.SortOrder;

            return View(model);
        }
    }
}