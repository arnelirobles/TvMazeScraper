using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using TvMazeScraper.API.Repository;

namespace TvMazeScraper.API.Services
{
    public class ShowService : IShowService
    {
        private readonly IConfiguration _configuration;

        public ShowService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private List<Show> GetShowFromTV(int page)
        {
            var client = new RestClient("https://api.tvmaze.com/shows?page=1");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            return JsonConvert.DeserializeObject<List<Show>>(response.Content);
        }

        public async Task<List<Show>> SyncShows()
        {
            var client = new RestClient("https://api.tvmaze.com/shows?page=1");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);

            var shows = GetShowFromTV(1);
            shows.AddRange(GetShowFromTV(2));

            string constr = _configuration.GetConnectionString("DefaultConnection");

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
           ,[_links])
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
            )";

            using (SqlConnection con = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                  
                    cmd.Connection = con;
                    con.Open();
                    foreach (var show in shows)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@id", show.id);

                        cmd.Parameters.AddWithValue("@url", !string.IsNullOrWhiteSpace(show.url) ? show.url : string.Empty);

                        cmd.Parameters.AddWithValue("@name", !string.IsNullOrWhiteSpace(show.name) ? show.name:string.Empty);

                        cmd.Parameters.AddWithValue("@type", !string.IsNullOrWhiteSpace(show.type) ? show.type:string.Empty);

                        cmd.Parameters.AddWithValue("@language", !string.IsNullOrWhiteSpace(show.language) ? show.language:string.Empty);

                        cmd.Parameters.AddWithValue("@genres", JsonConvert.SerializeObject(show.genres));

                        cmd.Parameters.AddWithValue("@status", !string.IsNullOrWhiteSpace(show.status) ? show.status:string.Empty);

                        cmd.Parameters.AddWithValue("@runtime", show.runtime ?? 0);

                        cmd.Parameters.AddWithValue("@averageRuntime", show.averageRuntime ?? 0);

                        cmd.Parameters.AddWithValue("@premiered", !string.IsNullOrWhiteSpace(show.premiered) ? show.premiered: string.Empty);

                        cmd.Parameters.AddWithValue("@ended", !string.IsNullOrWhiteSpace(show.ended) ? show.ended: string.Empty);

                        cmd.Parameters.AddWithValue("@officialSite", !string.IsNullOrWhiteSpace(show.officialSite) ? show.officialSite : string.Empty);

                        cmd.Parameters.AddWithValue("@schedule", JsonConvert.SerializeObject(show.schedule));

                        cmd.Parameters.AddWithValue("@rating", JsonConvert.SerializeObject(show.rating));

                        cmd.Parameters.AddWithValue("@weight", show.weight ?? 0);

                        cmd.Parameters.AddWithValue("@network", JsonConvert.SerializeObject(show.network));

                        cmd.Parameters.AddWithValue("@webChannel", JsonConvert.SerializeObject(show.webChannel));

                        cmd.Parameters.AddWithValue("@dvdCountry", !string.IsNullOrWhiteSpace(show.dvdCountry)? show.dvdCountry : string.Empty);

                        cmd.Parameters.AddWithValue("@externals", JsonConvert.SerializeObject(show.externals));

                        cmd.Parameters.AddWithValue("@image", JsonConvert.SerializeObject(show.image));

                        cmd.Parameters.AddWithValue("@summary", !string.IsNullOrWhiteSpace(show.summary) ? show.summary:string.Empty);

                        cmd.Parameters.AddWithValue("@updated", show.updated ?? 0);

                        cmd.Parameters.AddWithValue("@links", JsonConvert.SerializeObject(show._links));

                        cmd.ExecuteScalar();
                    }

                    con.Close();
                }
            }

            return shows;
        }
    }
}