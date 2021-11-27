using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using TvMazeScraper.API.Models;

namespace TvMazeScraper.API.Services
{
    public class ShowService : IShowService
    {
        private static PolicyBuilder queuePolicy = Policy.Handle<Exception>();
        private readonly IConfiguration _configuration;

        public ShowService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task<List<int>> GetShowsFromTVAsync(int page)
        {
            //retry mechanism
            var _policy = queuePolicy.WaitAndRetryAsync(3,
                         retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)),
                         (exception, timeSpan, retryCount, context) =>
                         {
                             Console.WriteLine(exception.Message);
                         });
            var result = new List<Show>();

            var client = new RestClient("https://api.tvmaze.com/shows?page=" + page);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);

            await _policy.ExecuteAsync(async () =>
            {

                IRestResponse response = client.Execute(request);

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                result = JsonConvert.DeserializeObject<List<Show>>(response.Content, settings);
            });



            return result.Select(c => c.id).ToList();
        }

        private Show GetCastFromShow(int showId)
        {
            var client = new RestClient("https://api.tvmaze.com/shows/" + showId + "?embed=cast");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            return JsonConvert.DeserializeObject<Show>(response.Content);
        }

        private void ClearShows()
        {
            string query = "DELETE FROM Shows";

            string constr = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
        }

        public async Task<List<Show>> SyncShowsAsync()
        {
            var showIds = await GetShowsFromTVAsync(1);
            showIds.AddRange(await GetShowsFromTVAsync(2));

            string constr = _configuration.GetConnectionString("DefaultConnection");


            ClearShows();
            var shows = new List<Show>();
            string query = @"INSERT INTO [dbo].[Shows]
           ([id]
           ,[url]
           ,[name]
           ,[type]
           ,[language]
           ,[genres]
           ,[status]
           ,[runtime]
           ,[averageRuntime]
           ,[premiered]
           ,[ended]
           ,[officialSite]
           ,[schedule]
           ,[rating]
           ,[weight]
           ,[network]
           ,[webChannel]
           ,[dvdCountry]
           ,[externals]
           ,[image]
           ,[summary]
           ,[updated]
           ,[_links]
           ,[_embedded])
     VALUES
           (
            @id
           ,@url
           ,@name
           ,@type
           ,@language
           ,@genres
           ,@status
           ,@runtime
           ,@averageRuntime
           ,@premiered
           ,@ended
           ,@officialSite
           ,@schedule
           ,@rating
           ,@weight
           ,@network
           ,@webChannel
           ,@dvdCountry
           ,@externals
           ,@image
           ,@summary
           ,@updated
           ,@links
           ,@embedded
            )";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    con.Open();

                    foreach (var showid in showIds)
                    {
                        var show = GetCastFromShow(showid);

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@id", show.id);

                        cmd.Parameters.AddWithValue("@url", !string.IsNullOrWhiteSpace(show.url) ? show.url : string.Empty);

                        cmd.Parameters.AddWithValue("@name", !string.IsNullOrWhiteSpace(show.name) ? show.name : string.Empty);

                        cmd.Parameters.AddWithValue("@type", !string.IsNullOrWhiteSpace(show.type) ? show.type : string.Empty);

                        cmd.Parameters.AddWithValue("@language", !string.IsNullOrWhiteSpace(show.language) ? show.language : string.Empty);

                        cmd.Parameters.AddWithValue("@genres", JsonConvert.SerializeObject(show.genres));

                        cmd.Parameters.AddWithValue("@status", !string.IsNullOrWhiteSpace(show.status) ? show.status : string.Empty);

                        cmd.Parameters.AddWithValue("@runtime", show.runtime ?? 0);

                        cmd.Parameters.AddWithValue("@averageRuntime", show.averageRuntime ?? 0);

                        cmd.Parameters.AddWithValue("@premiered", !string.IsNullOrWhiteSpace(show.premiered) ? show.premiered : string.Empty);

                        cmd.Parameters.AddWithValue("@ended", !string.IsNullOrWhiteSpace(show.ended) ? show.ended : string.Empty);

                        cmd.Parameters.AddWithValue("@officialSite", !string.IsNullOrWhiteSpace(show.officialSite) ? show.officialSite : string.Empty);

                        cmd.Parameters.AddWithValue("@schedule", JsonConvert.SerializeObject(show.schedule));

                        cmd.Parameters.AddWithValue("@rating", JsonConvert.SerializeObject(show.rating));

                        cmd.Parameters.AddWithValue("@weight", show.weight ?? 0);

                        cmd.Parameters.AddWithValue("@network", JsonConvert.SerializeObject(show.network));

                        cmd.Parameters.AddWithValue("@webChannel", show.webChannel == null ? string.Empty : show.webChannel.ToString());

                        cmd.Parameters.AddWithValue("@dvdCountry", show.dvdCountry == null ? string.Empty : show.dvdCountry.ToString());

                        cmd.Parameters.AddWithValue("@externals", JsonConvert.SerializeObject(show.externals));

                        cmd.Parameters.AddWithValue("@image", JsonConvert.SerializeObject(show.image));

                        cmd.Parameters.AddWithValue("@summary", !string.IsNullOrWhiteSpace(show.summary) ? show.summary : string.Empty);

                        cmd.Parameters.AddWithValue("@updated", show.updated ?? 0);

                        cmd.Parameters.AddWithValue("@links", JsonConvert.SerializeObject(show._links));

                        cmd.Parameters.AddWithValue("@embedded", JsonConvert.SerializeObject(show._embedded));

                        cmd.ExecuteScalar();

                        shows.Add(show);
                    }

                    con.Close();
                }
            }

            return shows;
        }

        public PagedList<ShowVM> GetShows(int pageNumber, int pageSize)
        {
            string constr = _configuration.GetConnectionString("DefaultConnection");

            var shows = new List<ShowVM>();
            string query = @"Select * From Shows";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    con.Open();

                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            shows.Add(new ShowVM
                            {
                                id = Convert.ToInt32(sdr["id"])
                                ,url = sdr["url"].ToString()
                                ,name = sdr["name"].ToString()
                                ,type = sdr["type"].ToString()
                                ,language = sdr["language"].ToString()
                                ,genres = JsonConvert.DeserializeObject<List<string>>(sdr["genres"].ToString())
                                ,status = sdr["status"].ToString()
                                ,runtime = Convert.ToInt32(sdr["runtime"]) 
                                ,averageRuntime = Convert.ToInt32(sdr["averageRuntime"])  
                                ,premiered = sdr["premiered"].ToString()
                                ,ended = sdr["ended"].ToString()
                                ,officialSite = sdr["officialSite"].ToString()
                                ,schedule = JsonConvert.DeserializeObject<Schedule>( sdr["schedule"].ToString())
                                ,rating = JsonConvert.DeserializeObject<Rating>(sdr["rating"].ToString())
                                ,weight = Convert.ToInt32(sdr["weight"])  
                                ,network = JsonConvert.DeserializeObject<Network>(sdr["network"].ToString())
                                ,webChannel = sdr["webChannel"].ToString()
                                ,dvdCountry = sdr["dvdCountry"].ToString()
                                ,externals = JsonConvert.DeserializeObject<Externals>(sdr["externals"].ToString())
                                ,image = JsonConvert.DeserializeObject<Image>(sdr["image"].ToString())
                                ,summary = sdr["summary"].ToString()
                                ,updated = Convert.ToInt32(sdr["updated"])
                                ,_links = JsonConvert.DeserializeObject<Links>(sdr["_links"].ToString())
                                ,casts = JsonConvert.DeserializeObject<Embedded>(sdr["_embedded"].ToString()).cast


                            });
                        }
                    }

                    con.Close();
                }
            }

            return new PagedList<ShowVM>
            {
                Items = shows.Skip(pageNumber - 1).Take(pageSize).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = shows.Count
            };

          
        }

        public ShowVM GetShow(int showId)
        {
            string constr = _configuration.GetConnectionString("DefaultConnection");

            var shows = new List<ShowVM>();
            string query = @"Select * From Shows where id = " + showId;

                     using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    con.Open();

                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            shows.Add(new ShowVM
                            {
                                id = Convert.ToInt32(sdr["id"])
                                ,url = sdr["url"].ToString()
                                ,name = sdr["name"].ToString()
                                ,type = sdr["type"].ToString()
                                ,language = sdr["language"].ToString()
                                ,genres = JsonConvert.DeserializeObject<List<string>>(sdr["genres"].ToString())
                                ,status = sdr["status"].ToString()
                                ,runtime = Convert.ToInt32(sdr["runtime"]) 
                                ,averageRuntime = Convert.ToInt32(sdr["averageRuntime"])  
                                ,premiered = sdr["premiered"].ToString()
                                ,ended = sdr["ended"].ToString()
                                ,officialSite = sdr["officialSite"].ToString()
                                ,schedule = JsonConvert.DeserializeObject<Schedule>( sdr["schedule"].ToString())
                                ,rating = JsonConvert.DeserializeObject<Rating>(sdr["rating"].ToString())
                                ,weight = Convert.ToInt32(sdr["weight"])  
                                ,network = JsonConvert.DeserializeObject<Network>(sdr["network"].ToString())
                                ,webChannel = sdr["webChannel"].ToString()
                                ,dvdCountry = sdr["dvdCountry"].ToString()
                                ,externals = JsonConvert.DeserializeObject<Externals>(sdr["externals"].ToString())
                                ,image = JsonConvert.DeserializeObject<Image>(sdr["image"].ToString())
                                ,summary = sdr["summary"].ToString()
                                ,updated = Convert.ToInt32(sdr["updated"])
                                ,_links = JsonConvert.DeserializeObject<Links>(sdr["_links"].ToString())
                                ,casts = JsonConvert.DeserializeObject<Embedded>(sdr["_embedded"].ToString()).cast


                            });
                        }
                    }

                    con.Close();
                }
            }

            return shows.FirstOrDefault();
        }
    }
}