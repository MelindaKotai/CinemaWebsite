using Licenta.Data;

using Licenta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


using Licenta.ViewModels;
using Licenta.APIModels;
using System.Net;

namespace Licenta.Controllers
{
    public class MovieController:Controller
    {

        public IConfiguration Configuration { get; }
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public MovieController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment hostEnvironment)
            {

                _context = context;
                Configuration = configuration;
                _hostEnvironment = hostEnvironment;
           }




        [Route("Movie/{id}")]
        public async Task<IActionResult> Index(int id)
        { 
             //se gaseste filmul cerut
             var movie = await _context.Movies.Where(x => x.id == id).FirstOrDefaultAsync();

            //daca nu se gaseste filmul se returneaza mesaj de eroare
            if (movie == null)
            {
                ViewBag.ErrorMessage = "Filmul nu a fost găsit!";
                return View();
            }
                
                //lista de date
                //ia ecranizarile care ruleaza in urmatoarea saptamana pt filmul ales
                var screeningsdates =await _context.Screenings.Where(x => x.movieId == id).Where(x => x.date > DateTime.Now.AddDays(-1)).Where(x => x.date <= DateTime.Now.AddDays(6)).OrderBy(X=>X.date).ToListAsync();
               

                //lista de date calendaristice in care ruleaza filmul
                List<DateTime> dates = new List<DateTime>();
                foreach (var screening in screeningsdates)
                {   
                    if(!dates.Contains(screening.date))
                      dates.Add(screening.date);
                }   
            

                //apelare API
                var client = new HttpClient();

                    //se obtin genurile
                    var titlu = movie.title;


                    //se obtine id-ul filmului din baza de date interogata
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
                    FindResult movies = new FindResult();
                    string IMDBid = "";
                    int i = 0;
                    movies = JsonConvert.DeserializeObject<FindResult>(body);
                   //se gaseste primul rezultat tip titlu
                   foreach(var movieresulted in movies.results)
                    {
                        if (movieresulted.id.Substring(1, movieresulted.id.Length - 12) == "title")
                        {
                            IMDBid = movieresulted.id;
                            break;
                        }
                        else
                            i++;
                   }

                    List<string> genres = new List<string>();
                    List<string> actors = new List<string>();
                    double rating = 0;
                        

                    //se verifica daca s-a gasit vreun film
                    if (IMDBid != "")
                    {
                        //remove /title and last slash
                        IMDBid = IMDBid.Substring(7, IMDBid.Length - 8);
                            
                        //get de movie genre           
                        var request2 = new HttpRequestMessage
                        {                         
                            Method = HttpMethod.Get,                                
                            RequestUri = new Uri("https://imdb8.p.rapidapi.com/title/get-genres?tconst=" + IMDBid),                              
                            Headers =
                              {
                                 { "x-rapidapi-key", Configuration.GetValue<string>("ApiKey") },
                                 { "x-rapidapi-host", "imdb8.p.rapidapi.com" },
                             },
                        };
                            
                        //get response 
                        var response2 = await client.SendAsync(request2);                          
                        if (response2.StatusCode != HttpStatusCode.OK)
                            {
                                  genres.Add("N/A");
                            }
                        else
                            {   //get list of genres
                                var body2 = await response2.Content.ReadAsStringAsync();  
                                genres = JsonConvert.DeserializeObject<List<string>>(body2); 
                            }
                   
                       

                        //get IMDB rating
                        var request3 = new HttpRequestMessage
                        {
                            Method = HttpMethod.Get,
                            RequestUri = new Uri("https://imdb8.p.rapidapi.com/title/get-ratings?tconst="+IMDBid),
                                                    Headers =
                                    {
                                        { "x-rapidapi-key", Configuration.GetValue<string>("ApiKey") },
                                        { "x-rapidapi-host", "imdb8.p.rapidapi.com" },
                                    },
                        };
                        var response3 = await client.SendAsync(request3);
                        if (response3.StatusCode == HttpStatusCode.OK)
                        {
                             IMDBRating ratingresult = new IMDBRating();
                             var body3 = await response3.Content.ReadAsStringAsync();                
                             ratingresult = JsonConvert.DeserializeObject<IMDBRating>(body3); 
                             rating = ratingresult.rating;
                        }

                        //get leading actors
                        var actorsresult = movies.results[i].principals;
                        if (actorsresult != null)
                        {
                            foreach (var a in actorsresult)
                            {
                                actors.Add(a.name);
                            }
                        }else
                            actors.Add("N/A");
                    }
                    else
                    {
                        genres.Add("N/A");
                        actors.Add("N/A");

                    }


                    //se creaza modelul final care se trimite pe front-end
                    MoviePageViewModel model = new MoviePageViewModel();
    
                        model.id = movie.id;
                        model.title = movie.title;
                        model.video = movie.video;
                        model.img = movie.img;
                        model.description = movie.description;
                        model.duration = movie.duration;
                        model.release_date = movie.release_date;
                        model.genres = genres;
                        model.actors = actors;
                        model.rating = rating;
                        model.dates = dates;

                    return View(model);
        }


     
        [Route("/Movie/GetHours")]
        //returneaza orele la care ruleaza un film intr-o anumita zi
        public string GetHours(DateTime? date,int id)
        {
            if (date != null)
                if ( date <= DateTime.Now.AddDays(-1) && date > DateTime.Now.AddDays(6))
                    return " ";
           
            var ScreeningHours = _context.Screenings.Where(x => x.movieId == id).Where(x => x.date == date).OrderBy(x => x.s_hour).ToList();
            List<ScreeningInfo> screenings = new List<ScreeningInfo>();
            foreach (var screeningHour in ScreeningHours)
            {
                    var nohours = screeningHour.s_hour.Hour;
                    var nominutes = screeningHour.s_hour.Minute;
                    var notoadd = nohours * 60 + nominutes - 30;
                    DateTime date2 = (DateTime)date;
                    if (DateTime.Now < (DateTime)date2.AddMinutes(notoadd)) {
                    ScreeningInfo screening = new ScreeningInfo()
                    {
                        screeningId = screeningHour.Id,
                        hour = screeningHour.s_hour,
                        is3D=screeningHour.is3D
                    };
                    screenings.Add(screening);
                } 
            }

            if (screenings.Count > 0)
            {
                var response = "<p> Ore disponbile:</p> ";

                foreach (var screening in screenings)
                {
                    response = response + "<a style='text-decoration:none;color:white;' href='/Screening/ChooseTickets/id=" + screening.screeningId + "'><div  class='bg-danger p-2 hour-link'>" + screening.hour.ToString("HH:mm") ;
                    if (screening.is3D != false)
                    {
                        response = response + "<span> 3D</span></div></a>";
                    }
                    else
                        response = response + "</div></a>";
               
                }
                response = response +
             "<p>Alegeți o oră și faceți o rezervare!</p>";
                return response;
            }
            else
            {
                var response = "<div class='alert alert-danger mt-2 ml-2 mb-2 mr-2'>Nu există ore disponibile </div> ";
                return response;
            }

        }

        [Authorize(Roles="Administrator")]
        //lista de filme 
        public async Task<IActionResult> List(string q, string f,int p)
        {
            if (p == null || p<=0)
            {
                p = 1;
            }

            List<MovieListViewModel> movies = await _context.Movies.Select(x => new MovieListViewModel { id = x.id, img = x.img, title = x.title, date_added = x.date_added, release_date = x.release_date, active = x.active }).ToListAsync();
            if (q != null)
            {
                q = q.ToLower();
                movies = movies.Where(x=>x.title.ToLower().Contains(q)).ToList();
                
            }
            if (f != null)
            {
                switch (f)
                {
                    case "viitor":
                        movies = movies.Where(x => x.active == false).Where(x => x.release_date > DateTime.Now).ToList();
                        break;
                    case "prezent":
                        movies = movies.Where(x => x.active == true).ToList();
                        break;
                    case "trecut":
                        movies = movies.Where(x => x.active == false).Where(x => x.release_date < DateTime.Now).ToList();
                        break;
                    case "toate":
                        break;
                }
              
            }

            //numarul de pagini obtinut dupa paginare
            if(movies.Count%5==0)
              ViewBag.noPages = (movies.Count / 5);
            else
              ViewBag.noPages = (movies.Count / 5)+1;


            movies = movies.Skip((p-1) * 5).Take(5).ToList();
            if (movies.Count == 0)
            {
                ViewBag.ErrorMessage = "Nu au fost găsite filme!";
                return View();
            }

           
            ViewBag.currPage = p;

            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }
            if (TempData["SuccesMessage"] != null)
            {
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
            }
          
            return View(movies);
        }



        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [Route("/Movie/ChangeStatus/{id}")]
        public async Task<string> ChangeStatus(int id)
        {
            var result = "";
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                result = "Filmul nu a fost găsita!";
                return result;
            }
            if (movie.active == false)
                movie.active = true;
            else
               movie.active = false;

            _context.Movies.Update(movie);
            await _context.SaveChangesAsync();
            result = "Statusul filmului a fost schimbat cu succes!";
            return result;
        }




        //pagina unde se creaza film nou
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            CreateMovieViewModel model = new CreateMovieViewModel();
            return View(model);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync(CreateMovieViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uploadFolder = _hostEnvironment.WebRootPath;
                string filename = Path.GetFileNameWithoutExtension(model.img.FileName);
                string fileextension = Path.GetExtension(model.img.FileName);
                filename = filename + DateTime.Now.ToString("yymmssfff") + fileextension;
                string path = Path.Combine(uploadFolder + "/img/", filename);
               
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    model.img.CopyTo(fileStream);
                }

                string filename2 = Path.GetFileNameWithoutExtension(model.video.FileName);
                string fileextension2 = Path.GetExtension(model.video.FileName);
                filename2 = filename2 + DateTime.Now.ToString("yymmssfff") + fileextension2;
                string path2 = Path.Combine(uploadFolder + "/videos/", filename2);

                            
                using (var fileStream = new FileStream(path2, FileMode.Create))
                {
                    model.video.CopyTo(fileStream);
                }

                Models.Movie movie = new Models.Movie();
                movie.title = model.title;
                movie.video = "videos/"+filename2;
                movie.release_date = model.release_date;
                movie.img = "img/" +filename;
                movie.active = model.active;
                movie.duration = model.duration;
                movie.description = model.description;
                movie.date_added = DateTime.Now;
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction("List");

            }

            else
                return View(model);
           
        }






        [Authorize(Roles = "Administrator")]
        [Route("/Movie/Update/{id}")]
        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "Filmul nu a fost găsit!";
                return RedirectToAction("List");
            }

            UpdateMovieViewModel model = new UpdateMovieViewModel()
            {
                id = movie.id,
                img1 = movie.img,
                video1 = movie.video,
                title =movie.title,
                description =movie.description,
                duration =movie.duration,               
                release_date =movie.release_date,
                active =movie.active
            };
            return View(model);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> UpdateAsync(UpdateMovieViewModel model)
        {
            if (ModelState.IsValid)
            { 
                Models.Movie movie = await _context.Movies.FindAsync(model.id);
                string uploadFolder = _hostEnvironment.WebRootPath;
                if (model.img != null)
                {            
                    string filename = Path.GetFileNameWithoutExtension(model.img.FileName);
                    string fileextension = Path.GetExtension(model.img.FileName);
                    filename = filename + DateTime.Now.ToString("yymmssfff") + fileextension;
                    string path = Path.Combine(uploadFolder + "/img/", filename);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        model.img.CopyTo(fileStream);
                    }  
                    movie.img = "img/" + filename;
                  
                }
                if (model.video != null)
                {

                    string filename2 = Path.GetFileNameWithoutExtension(model.video.FileName);
                    string fileextension2 = Path.GetExtension(model.video.FileName);
                    filename2 = filename2 + DateTime.Now.ToString("yymmssfff") + fileextension2;
                    string path2 = Path.Combine(uploadFolder + "/videos/", filename2);

                    using (var fileStream = new FileStream(path2, FileMode.Create))
                    {
                        model.video.CopyTo(fileStream);
                    }  
                    movie.video = "videos/" + filename2;
                }
               
                movie.title = model.title;
                movie.release_date = model.release_date;
                movie.active = model.active;
                movie.duration = model.duration;
                movie.description = model.description;
                movie.date_added = DateTime.Now;
                _context.Movies.Update(movie);
                await _context.SaveChangesAsync();
                TempData["SuccesMessage"] = "Filmul a fost actualizat cu succes!";
                return RedirectToAction("List");
            }
            else
                return View(model);
        }


    }

}
