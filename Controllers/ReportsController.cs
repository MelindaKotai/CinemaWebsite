using CoreHtmlToImage;
using Licenta.Data;
using Licenta.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
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

        [Authorize(Roles = "Administrator,Manager")]
        public async Task<IActionResult> Index()
        {
            ReportsViewModel model = new ReportsViewModel();
            List<SoldTicketsPerMovie> soldTicketsPerMovie = await _context.ReservedSeats.Where(x=>x.reservation.date.Year ==DateTime.Now.Year && x.reservation.date.Month == DateTime.Now.Month && x.reservation.date.Day == DateTime.Now.Day).Where(x=>x.reservation.payed == true).GroupBy(x => x.reservation.screening.movie.title).Select(x => new SoldTicketsPerMovie{ title = x.Key, noTickets = x.Count()}).OrderByDescending(x=>x.noTickets).ToListAsync();
            var totalTickets = 0;
            foreach (var stpm in soldTicketsPerMovie)
            {
                totalTickets = totalTickets + stpm.noTickets;
            }
            model.soldTicketsPerMovies = soldTicketsPerMovie;
            model.totalTickets = totalTickets;



            var reservations = await _context.Reservations.Where(x => x.date.Year == DateTime.Now.Year && x.date.Month == DateTime.Now.Month && x.date.Day == DateTime.Now.Day).Where(x=>x.payed==true).Include(x => x.screening.movie).Include(x => x.screening).Include(x => x.reservedSeats).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).ToListAsync();
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


            var ticketsSoldOnline = await _context.ReservedSeats.Where(x => x.reservation.date.Year == DateTime.Now.Year && x.reservation.date.Month == DateTime.Now.Month && x.reservation.date.Day == DateTime.Now.Day).Where(x => x.reservation.payed == true).Where(x => x.reservation.online==true).CountAsync();
            var ticketsSoldAtCinema = await _context.ReservedSeats.Where(x => x.reservation.date.Year == DateTime.Now.Year && x.reservation.date.Month == DateTime.Now.Month && x.reservation.date.Day == DateTime.Now.Day).Where(x => x.reservation.payed == true).Where(x => x.reservation.online==false).CountAsync();

            model.ticketsSoldAtCinema = ticketsSoldAtCinema;
            model.ticketsSoldOnline = ticketsSoldOnline;
           // model-> un obiect format din aceste obiecte..
            return View(model);
        }


        [Authorize(Roles = "Administrator,Manager")]
        public string GetTop(string option)
        {
            var result = "";
            switch (option)
            {
                case "allmoviest":
                    List<KeyVal> allmoviest=_context.ReservedSeats.Where(x => x.reservation.payed == true).GroupBy(x => x.reservation.screening.movie.title).Select(x => new KeyVal{ Key = x.Key, Value = x.Count() }).OrderByDescending(x => x.Value).ToList();
                    result= JsonConvert.SerializeObject(allmoviest);
                    break;
                case "presentmoviest":
                    List<KeyVal> presentmoviest = _context.ReservedSeats.Where(x => x.reservation.payed == true).Where(x=>x.reservation.screening.movie.active==true).GroupBy(x => x.reservation.screening.movie.title).Select(x => new KeyVal{ Key = x.Key, Value = x.Count() }).OrderByDescending(x => x.Value).ToList();
                    result = JsonConvert.SerializeObject(presentmoviest);
                    break;
                    
                case "allmoviesr":
                    var reservations = _context.Reservations.Where(x=>x.payed==true).Include(x => x.screening.movie).Include(x => x.screening).Include(x => x.reservedSeats).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).ToList();
                    Dictionary<string, float> revenuePerMovie = new Dictionary<string, float>();
                    foreach (var reservation in reservations)
                    { 
                        float total = 0;
                        var price = reservation.screening.price;
                        foreach (var ticket in reservation.reservedSeats)
                        {
                            total = total + (price - ticket.TicketType.discount * price);
                        }

                        if (revenuePerMovie.ContainsKey(reservation.screening.movie.title))
                        {
                            revenuePerMovie[reservation.screening.movie.title] = revenuePerMovie[reservation.screening.movie.title] + total;
                        }
                        else
                        {
                            revenuePerMovie.Add(reservation.screening.movie.title, total);
                        }
                    }
                    revenuePerMovie = revenuePerMovie.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    List<KeyVal> allmoviesr = new List<KeyVal>();
                    foreach(var rpm in revenuePerMovie.Keys)
                    {
                        KeyVal kv = new KeyVal();
                        kv.Key = rpm;
                        kv.Value = revenuePerMovie[rpm];
                        allmoviesr.Add(kv);
                    }
                    result = JsonConvert.SerializeObject(allmoviesr);
                    break;
                case "presentmoviesr":
                    var reservations2 = _context.Reservations.Where(x => x.payed == true).Where(x=>x.screening.movie.active==true).Include(x => x.screening.movie).Include(x => x.screening).Include(x => x.reservedSeats).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).ToList();
                    Dictionary<string, float> revenuePerMovie2 = new Dictionary<string, float>();
                    foreach (var reservation in reservations2)
                    {
                        float total = 0;
                        var price = reservation.screening.price;
                        foreach (var ticket in reservation.reservedSeats)
                        {
                            total = total + (price - ticket.TicketType.discount * price);
                        }

                        if (revenuePerMovie2.ContainsKey(reservation.screening.movie.title))
                        {
                            revenuePerMovie2[reservation.screening.movie.title] = revenuePerMovie2[reservation.screening.movie.title] + total;
                        }
                        else
                        {
                            revenuePerMovie2.Add(reservation.screening.movie.title, total);
                        }
                    }
                    revenuePerMovie2= revenuePerMovie2.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    List<KeyVal> presentmoviesr = new List<KeyVal>();
                    foreach (var rpm in revenuePerMovie2.Keys)
                    {
                        KeyVal kv = new KeyVal();
                        kv.Key = rpm;
                        kv.Value = revenuePerMovie2[rpm];
                        presentmoviesr.Add(kv);
                    }
                   
                    result = JsonConvert.SerializeObject(presentmoviesr);
                    break;
                case "clients":
                    List<KeyVal> clients = _context.ReservedSeats.Where(x=>x.reservation.payed==true).Where(x=>x.reservation.UserId!=null).GroupBy(x=>x.reservation.User.Email).Select(x=>new KeyVal { Key=x.Key,Value=x.Count()}).OrderByDescending(x => x.Value).ToList();
                    result = JsonConvert.SerializeObject(clients);
                    break;
                default:
                    result = "Error";
                    break;
            }
                
            return result;
        }

        [Authorize(Roles = "Administrator,Manager")]
        public string GetReport(string option)
        {
            string result = "";
            //pe luna bilete vandute ultimele 12 luni
            switch (option)
            {
                case "ticketsm":
                    List<KeyVal> ticketsm = _context.ReservedSeats.Where(x => x.reservation.date > DateTime.Now.AddYears(-1)).Where(x => x.reservation.payed == true).GroupBy(x => x.reservation.date.Month).OrderBy(x=>x.Key).Select(x => new KeyVal { Key = x.Key.ToString(), Value = x.Count() }).ToList();
                    foreach(var t in ticketsm)
                    {
                        switch (t.Key)
                        {
                            case "1":
                                t.Key = "Ianuarie";
                                break;
                            case "2":
                                t.Key = "Februarie";
                                break;
                            case "3":
                                t.Key = "Martie";
                                break;
                            case "4":
                                t.Key = "Aprilie";
                                break;
                            case "5":
                                t.Key = "Mai";
                                break;
                            case "6":
                                t.Key = "Iunie";
                                break;
                            case "7":
                                t.Key = "Iulie";
                                break;
                            case "8":
                                t.Key = "August";
                                break;
                            case "9":
                                t.Key = "Septembrie";
                                break;
                            case "10":
                                t.Key = "Octombrie";
                                break;
                            case "11":
                                t.Key = "Noiembrie";
                                break;
                            case "12":
                                t.Key = "Decembrie";
                                break;

                        }
                    }
                    result = JsonConvert.SerializeObject(ticketsm);
                    break;
                case "revenuem":
                    var reservations = _context.Reservations.Where(x => x.date > DateTime.Now.AddYears(-1)).Where(x => x.payed == true).Include(x => x.screening).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).OrderBy(x=>x.date).ToList();
                    Dictionary<string, float> revenuePerMonth = new Dictionary<string, float>();
                    foreach (var reservation in reservations)
                    {
                        float total = 0;
                        var price = reservation.screening.price;
                        foreach (var ticket in reservation.reservedSeats)
                        {
                            total = total + (price - ticket.TicketType.discount * price);
                        }

                        if (revenuePerMonth.ContainsKey(reservation.date.ToString("MMMM", new CultureInfo("ro"))))
                        {
                            revenuePerMonth[reservation.date.ToString("MMMM", new CultureInfo("ro"))] = revenuePerMonth[reservation.date.ToString("MMMM", new CultureInfo("ro"))] + total;
                        }
                        else
                        {
                            revenuePerMonth.Add(reservation.date.ToString("MMMM", new CultureInfo("ro")), total);
                        }
                    }
                  
                    List<KeyVal> revenuem = new List<KeyVal>();
                    foreach (var rpm in revenuePerMonth.Keys)
                    {
                        KeyVal kv = new KeyVal();
                        kv.Key = rpm;
                        kv.Value = revenuePerMonth[rpm];
                        revenuem.Add(kv);
                    }

                    result = JsonConvert.SerializeObject(revenuem);


                    break;
                default:
                    result = "Error";
                    break;
            }
            //pe luna venituri realizate ultimele 12 luni
            return result;
        }

        [Authorize(Roles = "Administrator,Manager")]
        public string GetDailyReport(string option, DateTime date)
        {
            string result = "";
           
            switch (option)
            {
                case "ticketsd":
                    List<KeyVal> ticketsd =  _context.ReservedSeats.Where(x => x.reservation.date.Year == date.Year && x.reservation.date.Month ==date.Month && x.reservation.date.Day == date.Day).Where(x => x.reservation.payed == true).GroupBy(x => x.reservation.screening.movie.title).Select(x => new KeyVal { Key = x.Key, Value = x.Count() }).OrderByDescending(x => x.Value).ToList();
                    result = JsonConvert.SerializeObject(ticketsd);
                    break;
                case "revenued":
                    var reservations = _context.Reservations.Where(x => x.date.Year == date.Year && x.date.Month == date.Month && x.date.Day == date.Day).Where(x => x.payed == true).Include(x => x.screening.movie).Include(x => x.screening).Include(x => x.reservedSeats).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).ToList();
                    Dictionary<string, float> revenued = new Dictionary<string, float>();
                    foreach (var reservation in reservations)
                    {
                        //sa se calculeze cat costa rezervarea-> sa se adune la titlul corespunsator
                        float total = 0;
                        var price = reservation.screening.price;
                        foreach (var ticket in reservation.reservedSeats)
                        {
                            total = total + (price - ticket.TicketType.discount * price);
                        }

                        if (revenued.ContainsKey(reservation.screening.movie.title))
                        {
                            revenued[reservation.screening.movie.title] = revenued[reservation.screening.movie.title] + total;
                        }
                        else
                        {
                            revenued.Add(reservation.screening.movie.title, total);
                        }
                    }
                    revenued = revenued.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    List<KeyVal> r = new List<KeyVal>();
                    foreach (var rpm in revenued.Keys)
                    {
                        KeyVal kv = new KeyVal();
                        kv.Key = rpm;
                        kv.Value = revenued[rpm];
                        r.Add(kv);
                    }

                    result = JsonConvert.SerializeObject(r);
                    break;
                default:
                    result = "Error";
                    break;
            }
           
            return result;
        }





        [Authorize(Roles = "Administrator,Manager")]

        public IActionResult ExportTop(string option)
        {
          
            var builder = new StringBuilder();
            var i = 1;
            switch (option)
            {
                case "allmoviest":
                    List<KeyVal> allmoviest = _context.ReservedSeats.Where(x => x.reservation.payed == true).GroupBy(x => x.reservation.screening.movie.title).Select(x => new KeyVal { Key = x.Key, Value = x.Count() }).OrderByDescending(x => x.Value).ToList();
                   
                    builder.AppendLine("Nr,Film,Bilete vandute");
                  
                    foreach (var r in allmoviest)
                    {
                        builder.AppendLine($"{i},{r.Key},{r.Value}");
                        i++;
                    }

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Top.csv");
                   
                case "presentmoviest":
                    List<KeyVal> presentmoviest = _context.ReservedSeats.Where(x => x.reservation.payed == true).Where(x => x.reservation.screening.movie.active == true).GroupBy(x => x.reservation.screening.movie.title).Select(x => new KeyVal { Key = x.Key, Value = x.Count() }).OrderByDescending(x => x.Value).ToList();
                    builder.AppendLine("Nr,Film,Bilete vandute");
                   
                    foreach (var r in presentmoviest)
                    {
                        builder.AppendLine($"{i},{r.Key},{r.Value}");
                        i++;
                    }

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Top.csv");

                case "allmoviesr":
                    var reservations = _context.Reservations.Where(x => x.payed == true).Include(x => x.screening.movie).Include(x => x.screening).Include(x => x.reservedSeats).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).ToList();
                    Dictionary<string, float> revenuePerMovie = new Dictionary<string, float>();
                    foreach (var reservation in reservations)
                    {
                        float total = 0;
                        var price = reservation.screening.price;
                        foreach (var ticket in reservation.reservedSeats)
                        {
                            total = total + (price - ticket.TicketType.discount * price);
                        }

                        if (revenuePerMovie.ContainsKey(reservation.screening.movie.title))
                        {
                            revenuePerMovie[reservation.screening.movie.title] = revenuePerMovie[reservation.screening.movie.title] + total;
                        }
                        else
                        {
                            revenuePerMovie.Add(reservation.screening.movie.title, total);
                        }
                    }
                    revenuePerMovie = revenuePerMovie.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    List<KeyVal> allmoviesr = new List<KeyVal>();
                    foreach (var rpm in revenuePerMovie.Keys)
                    {
                        KeyVal kv = new KeyVal();
                        kv.Key = rpm;
                        kv.Value = revenuePerMovie[rpm];
                        allmoviesr.Add(kv);
                    }
                    builder.AppendLine("Nr,Film,Venit generat");

                    foreach (var r in allmoviesr)
                    {
                        builder.AppendLine($"{i},{r.Key},{r.Value}");
                        i++;
                    }

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Top.csv");
                case "presentmoviesr":
                    var reservations2 = _context.Reservations.Where(x => x.payed == true).Where(x => x.screening.movie.active == true).Include(x => x.screening.movie).Include(x => x.screening).Include(x => x.reservedSeats).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).ToList();
                    Dictionary<string, float> revenuePerMovie2 = new Dictionary<string, float>();
                    foreach (var reservation in reservations2)
                    {
                        float total = 0;
                        var price = reservation.screening.price;
                        foreach (var ticket in reservation.reservedSeats)
                        {
                            total = total + (price - ticket.TicketType.discount * price);
                        }

                        if (revenuePerMovie2.ContainsKey(reservation.screening.movie.title))
                        {
                            revenuePerMovie2[reservation.screening.movie.title] = revenuePerMovie2[reservation.screening.movie.title] + total;
                        }
                        else
                        {
                            revenuePerMovie2.Add(reservation.screening.movie.title, total);
                        }
                    }
                    revenuePerMovie2 = revenuePerMovie2.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    List<KeyVal> presentmoviesr = new List<KeyVal>();
                    foreach (var rpm in revenuePerMovie2.Keys)
                    {
                        KeyVal kv = new KeyVal();
                        kv.Key = rpm;
                        kv.Value = revenuePerMovie2[rpm];
                        presentmoviesr.Add(kv);
                    }

                    builder.AppendLine("Nr,Film,Venituri generate");

                    foreach (var r in presentmoviesr)
                    {
                        builder.AppendLine($"{i},{r.Key},{r.Value}");
                        i++;
                    }

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Top.csv");
                case "clients":
                    List<KeyVal> clients = _context.ReservedSeats.Where(x => x.reservation.payed == true).Where(x => x.reservation.UserId != null).GroupBy(x => x.reservation.User.Email).Select(x => new KeyVal { Key = x.Key, Value = x.Count() }).OrderByDescending(x => x.Value).ToList();
                    builder.AppendLine("Nr,Client,Bilete cumparate");

                    foreach (var r in clients)
                    {
                        builder.AppendLine($"{i},{r.Key},{r.Value}");
                        i++;
                    }

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Top.csv");
                default:
                    builder.AppendLine("Eroare, datele nu au putut fi culese");

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Top.csv");
            }
         
        }

        [Authorize(Roles = "Administrator,Manager")]
        public IActionResult ExportReport(string option)
        {

            var builder = new StringBuilder();
            //pe luna bilete vandute ultimele 12 luni
            switch (option)
            {
                case "ticketsm":
                    List<KeyVal> ticketsm = _context.ReservedSeats.Where(x => x.reservation.date > DateTime.Now.AddYears(-1)).Where(x => x.reservation.payed == true).GroupBy(x => x.reservation.date.Month).OrderBy(x => x.Key).Select(x => new KeyVal { Key = x.Key.ToString(), Value = x.Count() }).ToList();
                    foreach (var t in ticketsm)
                    {
                        switch (t.Key)
                        {
                            case "1":
                                t.Key = "Ianuarie";
                                break;
                            case "2":
                                t.Key = "Februarie";
                                break;
                            case "3":
                                t.Key = "Martie";
                                break;
                            case "4":
                                t.Key = "Aprilie";
                                break;
                            case "5":
                                t.Key = "Mai";
                                break;
                            case "6":
                                t.Key = "Iunie";
                                break;
                            case "7":
                                t.Key = "Iulie";
                                break;
                            case "8":
                                t.Key = "August";
                                break;
                            case "9":
                                t.Key = "Septembrie";
                                break;
                            case "10":
                                t.Key = "Octombrie";
                                break;
                            case "11":
                                t.Key = "Noiembrie";
                                break;
                            case "12":
                                t.Key = "Decembrie";
                                break;

                        }
                    }
                    builder.AppendLine("Luna,Bilete vandute");

                    foreach (var r in ticketsm)
                    {
                        builder.AppendLine($"{r.Key},{r.Value}");
                   
                    }

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Report.csv");
                case "revenuem":
                    var reservations = _context.Reservations.Where(x => x.date > DateTime.Now.AddYears(-1)).Where(x => x.payed == true).Include(x => x.screening).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).OrderBy(x => x.date).ToList();
                    Dictionary<string, float> revenuePerMonth = new Dictionary<string, float>();
                    foreach (var reservation in reservations)
                    {
                        float total = 0;
                        var price = reservation.screening.price;
                        foreach (var ticket in reservation.reservedSeats)
                        {
                            total = total + (price - ticket.TicketType.discount * price);
                        }

                        if (revenuePerMonth.ContainsKey(reservation.date.ToString("MMMM", new CultureInfo("ro"))))
                        {
                            revenuePerMonth[reservation.date.ToString("MMMM", new CultureInfo("ro"))] = revenuePerMonth[reservation.date.ToString("MMMM", new CultureInfo("ro"))] + total;
                        }
                        else
                        {
                            revenuePerMonth.Add(reservation.date.ToString("MMMM", new CultureInfo("ro")), total);
                        }
                    }

                    List<KeyVal> revenuem = new List<KeyVal>();
                    foreach (var rpm in revenuePerMonth.Keys)
                    {
                        KeyVal kv = new KeyVal();
                        kv.Key = rpm;
                        kv.Value = revenuePerMonth[rpm];
                        revenuem.Add(kv);
                    }

                    builder.AppendLine("Luna,venit realizat");

                    foreach (var r in revenuem)
                    {
                        builder.AppendLine($"{r.Key},{r.Value}");

                    }

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Report.csv");
                default:
                    builder.AppendLine("Nu au putut fi culese date");

                 

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Error.csv");
            }
         
        }

        [Authorize(Roles = "Administrator,Manager")]
        public IActionResult ExportDailyReport(string option, DateTime date)
        {

            var builder = new StringBuilder();

            switch (option)
            {
                case "ticketsd":
                    List<KeyVal> ticketsd = _context.ReservedSeats.Where(x => x.reservation.date.Year == date.Year && x.reservation.date.Month == date.Month && x.reservation.date.Day == date.Day).Where(x => x.reservation.payed == true).GroupBy(x => x.reservation.screening.movie.title).Select(x => new KeyVal { Key = x.Key, Value = x.Count() }).OrderByDescending(x => x.Value).ToList();
                    builder.AppendLine("Film,Bilete vandute");

                    foreach (var res in ticketsd)
                    {
                        builder.AppendLine($"{res.Key},{res.Value}");

                    }

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Report.csv");
                case "revenued":
                    var reservations = _context.Reservations.Where(x => x.date.Year == date.Year && x.date.Month == date.Month && x.date.Day == date.Day).Where(x => x.payed == true).Include(x => x.screening.movie).Include(x => x.screening).Include(x => x.reservedSeats).Include(x => x.reservedSeats).ThenInclude(x => x.TicketType).ToList();
                    Dictionary<string, float> revenued = new Dictionary<string, float>();
                    foreach (var reservation in reservations)
                    {
                        //sa se calculeze cat costa rezervarea-> sa se adune la titlul corespunsator
                        float total = 0;
                        var price = reservation.screening.price;
                        foreach (var ticket in reservation.reservedSeats)
                        {
                            total = total + (price - ticket.TicketType.discount * price);
                        }

                        if (revenued.ContainsKey(reservation.screening.movie.title))
                        {
                            revenued[reservation.screening.movie.title] = revenued[reservation.screening.movie.title] + total;
                        }
                        else
                        {
                            revenued.Add(reservation.screening.movie.title, total);
                        }
                    }
                    revenued = revenued.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                    List<KeyVal> r = new List<KeyVal>();
                    foreach (var rpm in revenued.Keys)
                    {
                        KeyVal kv = new KeyVal();
                        kv.Key = rpm;
                        kv.Value = revenued[rpm];
                        r.Add(kv);
                    }

                    builder.AppendLine("Film,Venituri realizate");

                    foreach (var res in r)
                    {
                        builder.AppendLine($"{res.Key},{res.Value}");

                    }

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Report.csv");
                default:
                    builder.AppendLine("Nu au putut fi returnate date!");

                

                    return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "Report.csv");
            }

        }



    }
}
