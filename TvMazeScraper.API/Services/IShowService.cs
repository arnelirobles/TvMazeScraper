using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TvMazeScraper.API.Models;

namespace TvMazeScraper.API.Services
{
    public interface IShowService
    {
        Task<List<Show>> SyncShowsAsync();
        PagedList<ShowVM> GetShows(int pageNumber, int pageSize);
        ShowVM GetShow(int showId);
    }
}
