using CoreHtmlToImage;
using Licenta.Data;
using Licenta.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    public class ReportsController : Controller
    {

       
        private readonly ApplicationDbContext _context;
     
        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            ReportsViewModel model = new ReportsViewModel();
            List<SoldTicketsPerMovie> soldTicketsPerMovie = await _context.ReservedSeats.Where(x=>x.reservation.date.Year ==DateTime.Now.Year && x.reservation.date.Month == DateTime.Now.Month && x.reservation.date.Day == DateTime.Now.Day).Where(x=>x.reservation.payed == 1).GroupBy(x => x.reservation.screening.movie.title).Select(x => new SoldTicketsPerMovie{ title = x.Key, noTickets = x.Count()}).OrderByDescending(x=>x.noTickets).ToListAsync();
            var totalTickets = 0;
            foreach (var stpm in soldTicketsPerMovie)
            {
                totalTickets = totalTickets + stpm.noTickets;
            }
            model.soldTicketsPerMovies = soldTicketsPerMovie;
            model.totalTickets = totalTickets;



            var reservations = await _context.Reservations.Where(x => x.date.Year == DateTime.Now.Year && x.date.Month == DateTime.Now.Month && x.date.Day == DateTime.Now.Day).Include(x => x.screening.movie).Include(x => x.screening).Include(x => x.reservedSeats).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).ToListAsync();
            Dictionary<string, float> revenuePerMovie = new Dictionary<string, float>();
            float totalRevenue = 0;
            foreach (var reservation in reservations)
            {
                //sa se calculeze cat costa rezervarea-> sa se adune la titlul corespunsator
                float total = 0;
                var price = reservation.screening.price;
                foreach(var ticket in reservation.reservedSeats)
                {
                    total = total + (price - ticket.TicketType.discount * price);
                }

                if (revenuePerMovie.ContainsKey(reservation.screening.movie.title))
                {
                    revenuePerMovie[reservation.screening.movie.title] = revenuePerMovie[reservation.screening.movie.title] + total;
                }
                else
                {
                    revenuePerMovie.Add(reservation.screening.movie.title,total);
                }
            }

            foreach(var rpm in revenuePerMovie)
            {
                totalRevenue = totalRevenue + rpm.Value;
            }
            model.revenuePerMovies = revenuePerMovie;
            model.totalRevenue = totalRevenue;


            var ticketsSoldOnline = await _context.ReservedSeats.Where(x => x.reservation.date.Year == DateTime.Now.Year && x.reservation.date.Month == DateTime.Now.Month && x.reservation.date.Day == DateTime.Now.Day).Where(x => x.reservation.payed == 1).Where(x => x.reservation.online==true).CountAsync();
            var ticketsSoldAtCinema = await _context.ReservedSeats.Where(x => x.reservation.date.Year == DateTime.Now.Year && x.reservation.date.Month == DateTime.Now.Month && x.reservation.date.Day == DateTime.Now.Day).Where(x => x.reservation.payed == 1).Where(x => x.reservation.online==false).CountAsync();

            model.ticketsSoldAtCinema = ticketsSoldAtCinema;
            model.ticketsSoldOnline = ticketsSoldOnline;
           // model-> un obiect format din aceste obiecte..
            return View(model);
        }

        
    }
}
