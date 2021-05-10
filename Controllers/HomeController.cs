using Licenta.Data;

using Licenta.Models;
using Licenta.Services;
using Licenta.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
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

        private readonly IEmailSender _emailSender;
        public IConfiguration Configuration { get; }
     


        public HomeController(IEmailSender emailSender, ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _emailSender = emailSender;
            Configuration = configuration;
       
        }

        public async Task<IActionResult> Index()
        {   //cele mai noi 4 filme adaugate active 
            var movies = await _context.Movies.Where(x => x.active == true).OrderByDescending(x => x.release_date).Take(4).ToListAsync();
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


        //returneaza pagina de contact
        public IActionResult Contact()
        {
            if (TempData["SuccesMessage"] != null)
            {
                ViewBag.SuccesMessage = TempData["SuccesMessage"]; 

            }
            ContactFormViewModel model = new ContactFormViewModel();
            return View(model);
        }


        [HttpPost]
        //se trimite mail cu mesajul transmis de utilizator
        public async Task<IActionResult> ContactAsync(ContactFormViewModel model)
        {

            if (!ModelState.IsValid) return View(model);
            var nume= model.fullName;
            var email=model.email;
            var telefon= model.phone;
            var mesaj=model.mesage;

            mesaj = "<h1> Acest mesaj este trimis de către un client de pe pagina de contact! </h1> <br/> Nume: " + nume + "<br/> Email: " + email + "<br/> Telefon: " + telefon + "<br/><br/> <b>Mesajul transmis este:</b><br/> " + mesaj;
            TempData["SuccesMessage"]="Mesajul a fost trimis cu succes. Dacă este necesar, unul din operatorii noștri vă va contacta prin email sau telefon pentru a vă raspunde. Mulțumim!";
            await  _emailSender.SendEmailAsync("cinemawebsitelicense@gmail.com", "UserMessage", mesaj);
            return RedirectToAction("Contact", new ContactFormViewModel());
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

         