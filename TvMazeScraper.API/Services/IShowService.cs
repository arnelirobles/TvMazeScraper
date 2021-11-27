using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TvMazeScraper.API.Repository;

namespace TvMazeScraper.API.Services
{
    public interface IShowService
    {
        Task<List<Show>> SyncShows();
    }
}
