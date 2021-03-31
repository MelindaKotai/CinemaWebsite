using Licenta.Data;

using Licenta.Models;
using Licenta.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using unirest_net.http;

namespace Licenta.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
       

        public IConfiguration Configuration { get; }
        

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            Configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {   //cele mai noi 4 filme adaugate active 
            var movies = await _context.Movies.Where(x => x.active == 1).OrderByDescending(x => x.release_date).Take(4).ToListAsync();
            List<HomePageViewModel> model = new List<HomePageViewModel>();
            foreach (var movie in movies) { 
             HomePageViewModel result = new HomePageViewModel()
                {
                    id = movie.id,
                    imgpath = movie.img,
                    release_date = movie.release_date.Date,
                    title = movie.title

                  };
             model.Add(result);
            
            }
               
               
            //return list of movies
            return View(model);       

            }
             
        

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

/*
           var client = new HttpClient();

          //list of movies to be passed to view


          //get genre from api
          foreach (var movie in movies)
          {
              var titlu = movie.title;
              //request movie id
              var request = new HttpRequestMessage
              {
                  Method = HttpMethod.Get,
                  RequestUri = new Uri("https://imdb8.p.rapidapi.com/title/find?q=" + titlu),
                  Headers =
              {
                  { "x-rapidapi-key", Configuration.GetValue<string>("ApiKey")  },
                  { "x-rapidapi-host", "imdb8.p.rapidapi.com" },
              },
              };
              //get api response
              var response = await client.SendAsync(request);

              response.EnsureSuccessStatusCode();
              //get body of response as string
              var body = await response.Content.ReadAsStringAsync();

              //get list of ids returned
              IdList ids = new IdList();



                   ids = JsonConvert.DeserializeObject<IdList>(body);


                  //get id of most relevent result
                 var id = ids.results[0].id;

                  //remove /title and last slash
                  id = id.Substring(7, id.Length - 8);



              //get de movie genre
               var request2 = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://imdb8.p.rapidapi.com/title/get-genres?tconst="+id),
                    Headers =
                    {
                   { "x-rapidapi-key", Configuration.GetValue<string>("ApiKey") },
                   { "x-rapidapi-host", "imdb8.p.rapidapi.com" },
                   },
                };
               //get response 
                var response2 = await client.SendAsync(request2);
              //response2.EnsureSuccessStatusCode();

              //get list of genres
              List<string> genres = new List<string>();
              var body2 = await response2.Content.ReadAsStringAsync();

              //if request return errors movie genres are not available
              if (!body2.Contains("400") && !body2.Contains("503"))
              { genres = JsonConvert.DeserializeObject<List<string>>(body2); }
              else
                  genres.Add("N/A");
              //get object movie to return

          */