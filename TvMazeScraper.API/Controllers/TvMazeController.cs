using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TvMazeScraper.API.Repository;
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
        [HttpPost("RetrieveTVShows")]
        public async Task<ActionResult> GetAsync()
        {

            var result = await _showService.SyncShows();

            return Ok(result);
        }
    }
}
