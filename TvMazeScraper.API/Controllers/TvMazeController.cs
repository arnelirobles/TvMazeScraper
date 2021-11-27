using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TvMazeScraper.API.Models;
using TvMazeScraper.API.Services;

namespace TvMazeScraper.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TvMazeController : ControllerBase
    {
        private readonly IShowService _showService;

        public TvMazeController(IShowService showService)
        {
            _showService = showService;
        }

        [HttpPost("ScrapTVShowswithCast")]
        public async Task<ActionResult> GetAsync()
        {
            var result = await _showService.SyncShowsAsync();

            return Ok(result);
        }

        [HttpGet("Shows")]
        public ActionResult GetShows(int pageNumber = 1, int pageSize = 20)
        {
            var result = _showService.GetShows(pageNumber, pageNumber);
            return Ok(result);
        }

        [HttpGet("Shows/{showId}")]
        public ActionResult GetShows(int showId)
        {
            var result = _showService.GetShow(showId);
            if (result != null) return Ok(result);
            else return NotFound();

        }
    }
}