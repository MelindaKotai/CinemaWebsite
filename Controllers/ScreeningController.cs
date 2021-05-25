using Licenta.Data;
using Licenta.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Licenta.Extension;
using Licenta.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CoreHtmlToImage;
using System.IO;
using QRCoder;
using System.Drawing;
using Microsoft.Extensions.Options;
using Licenta.Services;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text;
using System.IO.Compression;
using Newtonsoft.Json;
using Stripe;

namespace Licenta.Controllers
{
    public class ScreeningController: Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<User> _userManager;
        private readonly MessageSender Options;

        public ScreeningController(UserManager<User> userManager, ApplicationDbContext context, IEmailSender emailSender, RoleManager<IdentityRole> roleManager, IOptions<MessageSender> optionsAccessor)
        {
           
            _context = context;
            _emailSender = emailSender;
            _userManager = userManager;
            Options = optionsAccessor.Value;
        }


        //se verifica daca ecranizarea este in urmatoarele 7 zile si nu este setata la o ora din ziua de astazi din trecut
        public bool VerifyScreening(int id)
        {   
            var screening =  _context.Screenings.Where(x => x.Id == id).FirstOrDefault();
            if (screening == null)
                return true;
            var nohours = screening.s_hour.Hour;
            var nominutes = screening.s_hour.Minute;
            var notoadd = nohours * 60 + nominutes - 30;
            if (screening.date <= DateTime.Now.AddDays(-1) || screening.date > DateTime.Now.AddDays(6) || DateTime.Now > screening.date.AddMinutes(notoadd))
                return true;
            else
                return false;
        }

        //transforma un bitmat in bytearray
        private static Byte[] BitmapToBytes(Bitmap img)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        //functia care creaza qr code cu un link catre actiunea de schimbarea de status al unui bilet 
        public string CreateQR(int id)
        {
            var link = "https://localhost:44348/ReservedSeats/ChangeStatus/" + id;
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            var qrcode = BitmapToBytes(qrCodeImage);
            var base64qrcode = Convert.ToBase64String(qrcode);
            var imgSrcQrCode = String.Format("data:image/png;base64,{0}", base64qrcode);
            return imgSrcQrCode;
        }


        //se verifica daca se poate crea o rezervrae cu detaliile trimise ca parametru
        public async Task<VerifyModelError> VerifyModelAsync(ChooseSeatsViewModel details)
        {
           
            var response = new VerifyModelError();
            //se verifica daca ecranizarea la care se doreste sa se faca rezervare exista
            var screening = await _context.Screenings.Where(x => x.Id == details.screeningId).Include(x => x.hall).FirstOrDefaultAsync();
            if (screening == null)
            {
                response.action = "ChooseTickets";
                response.message = "Ecranizarea nu există!";
                response.isValid = false;
                return response;
            }


            //pt useri si utilizatori normali se verifica daca ecranizarea este in saptamana curenta
            if (User != null)
            {
                if (User.IsInRole("Client"))
                {
                    if (VerifyScreening(details.screeningId))
                    {
                        response.message = "Ecranizarea aleasă nu este disponibilă!";
                        response.action = "ChooseTickets";
                        response.isValid = false;
                        return response;

                    }
                }
            }
            else
            {
                if (VerifyScreening(details.screeningId))
                {
                    response.message = "Ecranizarea aleasă nu este disponibilă!";
                    response.action = "ChooseTickets";
                    response.isValid = false;
                    return response;
                }
            }

            //se verifica daca exista bilete in cos
            SessionObject so = HttpContext.Session.Get<SessionObject>(Convert.ToString(details.screeningId));

            //se calculeaza nr total de bilete din cos
            int total = 0;
            foreach (var ticket in so.tickets)
            {
                total = total + ticket.Value;
            }
            if (!(total > 0))
            {

                response.message = "Nu ați ales suficiente bilete!";
                response.action = "ChooseTickets";
                response.isValid = false;
                return response;

            }

            //se verifica daca in sala mai exista suficiente locuri libere 
            var noOfSeats = await _context.Seats.Where(x => x.hallId == screening.hallId).Where(x => x.seatNo != null).CountAsync();
            var noOfReserved = await _context.ReservedSeats.Join(_context.Reservations, r => r.reservationId, s => s.id, (r, s) => new { r.Id, s.screeningId }).Where(x => x.screeningId == screening.Id).CountAsync();
            var noRemaining = noOfSeats - noOfReserved;
            if (total > noRemaining)
            {
                response.message = "Nu mai există suficiente locuri în sală! Mai există doar " + noRemaining + " locuri disponibile.";
                response.action = "ChooseTickets";
                response.isValid = false;
                return response;
            }

            //se verifica daca s-a trimis o lista de locuri selectate
            if (details.selectedseats == null)
            {
                response.message = "Nu ați ales suficiente locuri";
                response.action = "ChooseSeats";
                response.isValid = false;
                return response;

            }

            //se verifia daca locurile alese nu au fost intre timp rezervate de altcineva 
            List<string> selectedseats = details.selectedseats.Split(',').ToList();
            List<int> reservedseats = new List<int>();
            reservedseats = await _context.Seats.Join(_context.ReservedSeats, seat => seat.Id, ReservedSeats => ReservedSeats.seatId, (seat, ReservedSeats) => new { seat, ReservedSeats }).Join(_context.Reservations, x => x.ReservedSeats.reservationId, Reservations => Reservations.id, (x, Reservations) => new { x, Reservations }).Where(x => x.Reservations.screeningId == details.screeningId).Select(x => x.x.seat.Id).ToListAsync();

            foreach (var seat in selectedseats)
            {     //daca locul a fost ocupat intre timp se returneaza inapoi pagina de alegere de locuri updatata
                if (reservedseats.Contains(Convert.ToInt32(seat)))
                {

                    response.message = "Locurile alese au fost deja ocupate!";
                    response.action = "ChooseSeats";
                    response.isValid = false;
                    return response;

                }
            }


            //se verifica daca nr de locuri alese coincide cu nr de locrui din sesiune
            if (selectedseats.Count != total)
            {

                response.message = "Numărul de locuri alese nu coincide cu numărul de bilete selectate!";
                response.action = "ChooseSeats";
                response.isValid = false;
                return response;
            }


            //se verifica daca utilizatorul care a trimis request-ul e autentificat si daca id-ul trimis e al lui 
            if (details.userId != null)
            {
                var user = await _context.Users.Where(x => x.Id == details.userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    response.message = "Utilizator inexistent!";
                    response.action = "ChooseSeats";
                    response.isValid = false;
                    return response;
                }


                //se verifica daca utilizatorul logat are id-ul transmis    
                if (User.Identity.IsAuthenticated)
                {
                    if (_userManager.GetUserId(User) != details.userId)
                    {
                        response.message = "Incercare invalida!";
                        response.action = "ChooseSeats";
                        response.isValid = false;
                        return response;

                    }
                }
                else
                {
                    response.message = "Incercare invalida!";
                    response.action = "ChooseSeats";
                    response.isValid = false;
                    return response;
                }

            }
            if (details.action == null || (details.action != "Rezervă biletele" && details.action != "Cumpără bilete"))
            {
                response.message = "Incercare invalida!";
                response.action = "ChooseSeats";
                response.isValid = false;
                return response;
            }

            response.isValid = true;
            return response;
        }

        //pagina program
        public async Task<IActionResult> IndexAsync(DateTime? date)
        {    
            
            if (date <= DateTime.Now.AddDays(-1) || date > DateTime.Now.AddDays(6))
            {
                ViewBag.UserMessage = "Nu exista filme disponibile in data selectată";
                return View();
            }
                
            if (date == null)
            {
                ViewBag.UserMessage = "Selectați o zi";
                return View();
            }

        
              //se iau filmele si ecranizarile filmelor care ruleaza in data selectata
               var filme = await _context.Movies.Join(_context.Screenings, m => m.id, s => s.movieId, (m, s) => new { id = m.id, title = m.title,
             
               img = m.img,date = s.date, sid = s.Id,hour=s.s_hour ,is3d=s.is3D}).Where(x => x.date == date).OrderBy(x=>x.id).ToListAsync();


                List<ScreeningMovies> movies = new List<ScreeningMovies>();

                //se contruieste lista de obiecte ScreeningMovies care vor fi returnate la View
                var id = filme[0].id;
                //lista cu informatii despre ecranizari (o lista de ecranizari pt fiecare film)
                List<List<ScreeningInfo>> sil = new List<List<ScreeningInfo>>();
                int k = 0;
                List<ScreeningInfo> screenings = new List<ScreeningInfo>();
                sil.Add(screenings);
                for (var i = 0; i < filme.Count(); i++)
                {
                if (filme[i].id != id)
                {
                    //se adauga in lista doar filmele care au ecranizari disponibile
                    if (sil[k].Count > 0)
                    {
                        ScreeningMovies movie = new ScreeningMovies() { id = id, img = filme[i - 1].img, title = filme[i - 1].title, screenings = sil[k], date = (DateTime)date };
                        movies.Add(movie);
                        k++;
                        List<ScreeningInfo> screenings2 = new List<ScreeningInfo>();
                        sil.Add(screenings2);
                    }
                    id = filme[i].id;
                  

                           //se verifica daca data si ora curenta nu a trecut de ora ecranizarii                       
                            var nohours = filme[i].hour.Hour;
                            var nominutes = filme[i].hour.Minute;
                            var notoadd = nohours * 60 + nominutes - 30;
                            
                            if (DateTime.Now < ((DateTime)date).AddMinutes(notoadd))
                            {
                                ScreeningInfo s = new ScreeningInfo()
                                {
                                    screeningId = filme[i].sid,
                                    hour = filme[i].hour,
                                    is3D = filme[i].is3d
                                };

                                sil[k].Add(s);
                            }

                }



                else
                {
                    
                       
                    //se verifica daca data si ora curenta nu a trecut de ora ecranizarii     
                            var nohours = filme[i].hour.Hour;
                            var nominutes = filme[i].hour.Minute;
                            var notoadd = nohours * 60 + nominutes - 30;
                            
                            if (DateTime.Now < ((DateTime)date).AddMinutes(notoadd))
                            {
                                ScreeningInfo s = new ScreeningInfo()
                                {
                                    screeningId = filme[i].sid,

                                    hour = filme[i].hour,
                                    is3D = filme[i].is3d
                                };

                                sil[k].Add(s);
                            }
                       
                }

                }
                if (sil[k].Count > 0)
                {
                    ScreeningMovies movief = new ScreeningMovies() { id = id, img = filme[filme.Count() - 1].img, title = filme[filme.Count() - 1].title, screenings = sil[k], date = (DateTime)date };
                    movies.Add(movief);
                }

            if (movies.Count <= 0)
            {
                ViewBag.UserMessage = "Nu există filme disponibile în data selectată";
                return View();
            }
            return View(movies);
        }


       //pagina cu fiolme care ruleaza
        public async Task<IActionResult> MoviesAsync() {
            var movies =await  _context.Movies.Where(x => x.active == true).ToListAsync();
            return View(movies);
        }

        //pagina cu filme care urmeaza sa ruleze
        public async Task<IActionResult> ComingSoonAsync()
        {
            var movies =await  _context.Movies.Where(x => x.active == false).Where(x => x.release_date > DateTime.Now).ToListAsync();
            return View(movies);
        }



        [Route("Screening/ChooseTickets/{id}")]
        //prima pagina din pasul de rezervare: alegerea numarului si tipului de bilete
        public async Task<IActionResult> ChooseTickets(int id)
        {

            var types = await _context.TicketType.Where(x=>x.active==true).ToListAsync();
            var screening = await _context.Screenings.Where(x => x.Id == id).FirstOrDefaultAsync();
        
            //se verifica daca ecranizarea aleasa exista in bd
            if (screening == null)
            {
                ViewBag.ErrorMessage = "Ecranizarea aleasă nu este disponibilă!";
                return View();
            }

            //pt cei care nu sunt logati si pt clienti se verifica daca ecranizarea nu depaseste perioada in care se poate face rezervare
            if (User != null)
            {
                if (User.IsInRole("Client"))
                {
                    if (VerifyScreening(id))
                    {
                        ViewBag.ErrorMessage = "Ecranizarea aleasă nu este disponibilă!";
                        return View();
                    }
                }
            }
            else
            {
                if (VerifyScreening(id))
                {
                    ViewBag.ErrorMessage = "Ecranizarea aleasă nu este disponibilă!";
                    return View();
                }
            }


            if (TempData["ErrorMessage"] != null)
                            ViewBag.ErrorMessage = TempData["ErrorMessage"];


            ChooseTicketsViewModel model = new ChooseTicketsViewModel();
            model.id = id;
            model.price = screening.price;
            model.types = types;
           
            return View(model);
        }
        

        //metoda apelata de ajax cand se adauga un bilet la o ecranizare in "cos"
        [HttpPost]
        public void AddTicket(int key,int value,int screeningId)
        {
           //se verifica daca exista deja bilete adaugate in sesiune pentru ecranizarea aleasa
            if(HttpContext.Session.Get<SessionObject>(Convert.ToString(screeningId))==null)
            {//daca nu exista se creaza o noua lista de bilete si se adauga biletul selectat
                //se creaza o lista noua de KVTickets
                SessionObject so = new SessionObject();
                List<KVTickets> tickets = new List<KVTickets>();

                KVTickets ticket = new KVTickets() { Key = key, Value = value };
                tickets.Add(ticket);
                so.tickets = tickets;
                
                HttpContext.Session.Set<SessionObject>(Convert.ToString(screeningId),so);
            }
            //daca exista se adauga la lista existenta biletul selectat
            else
            {
                var so= HttpContext.Session.Get<SessionObject>(Convert.ToString(screeningId));
               
                //se verifica daca ptr ecranizarea selectata exista in "cos" un bilet de tipul selectat
                var ticketexists = false;
                foreach(var ticket in so.tickets)
                {
                    if (ticket.Key == key)
                    {
                        ticketexists = true;
                    }
                }
              
                //daca exista se schimba valoare numarului de bilete din "cos" altfel se adauga un tip de bilet nou
                if (ticketexists)
                {
                    foreach (var ticket in so.tickets)
                    {
                        if (ticket.Key == key)
                        {
                            ticket.Value = value;
                        }
                    }
                }
                else
                { 
                    KVTickets ticket = new KVTickets() { Key = key, Value = value };
                    so.tickets.Add(ticket);
                }
                HttpContext.Session.Set(Convert.ToString(screeningId), so);
            }
            return;
        }



        [Route("Screening/ChooseSeats/{id}")]
        //metoda apelata la afisarea locurilor din sala de cinema
        public async Task<IActionResult> ChooseSeatsAsync(int id) {
            //se verifica daca se poate face rezervare la ecranizarea aleasa
            //pt cei nelogati si useri pt admin sa nu verifice 
            if (User != null)
            {
                if (User.IsInRole("Client"))
                {
                    if (VerifyScreening(id))
                    {
                        TempData["ErrorMessage"] = "Ecranizarea aleasă nu este disponibilă!";
                        return View();
                    }
                }
            }
            else
            {
                if (VerifyScreening(id))
                {
                    TempData["ErrorMessage"] = "Ecranizarea aleasă nu este disponibilă!";
                    return View();
                }
            }

            var screening =await _context.Screenings.Where(x => x.Id == id).FirstOrDefaultAsync();

            SessionObject so = HttpContext.Session.Get<SessionObject>(Convert.ToString(id));
           
            if (so == null || so.tickets.Count==0)
            {
                TempData["ErrorMessage"]  = "Nu ați ales suficiente bilete!";
                return RedirectToAction("ChooseTickets", new { id = id });
            }
            
            //se verifica daca s-au selectat biletele
            int total = 0;
            foreach(var ticket in so.tickets)
            {
                total = total + ticket.Value;
            }
           
            if (!(total > 0))
            {
                TempData["ErrorMessage"] = "Nu ați ales suficiente bilete!";
                return RedirectToAction("ChooseTickets", new { id = id });
            }
                

            //se verifica daca in sala mai exista suficiente locuri libere 
            var noOfSeats = await _context.Seats.Where(x=>x.hallId==screening.hallId).Where(x=>x.seatNo!=null).CountAsync();
            var noOfReserved = await _context.ReservedSeats.Join(_context.Reservations, r => r.reservationId, s => s.id, (r, s) => new { r.Id, s.screeningId }).Where(x => x.screeningId == screening.Id).CountAsync();
            var noRemaining = noOfSeats - noOfReserved;
            if(total>noRemaining)
            {
                TempData["ErrorMessage"] = "Nu mai există suficiente locuri în sală! Mai există doar " +noRemaining+" locuri disponibile.";
                return RedirectToAction("ChooseTickets", new { id = id });
            }
            //dupa realizarea tuturor verificarilor se poate crea modelul pt pagina
          

            int cols = await _context.Halls.Where(x => x.id == screening.hallId).Select(x => x.columns).FirstOrDefaultAsync();

            //lista de id-uri care reprezinta scaune rezervate
            List<int> reserved = new List<int>();
            reserved = await _context.Seats.Join(_context.ReservedSeats, seat => seat.Id, ReservedSeats => ReservedSeats.seatId, (seat, ReservedSeats) => new { seat, ReservedSeats }).Join(_context.Reservations, x => x.ReservedSeats.reservationId, Reservations => Reservations.id, (x, Reservations) => new { x, Reservations }).Where(x => x.Reservations.screeningId == screening.Id).Select(x => x.x.seat.Id).ToListAsync();

            //lista de elemente ale matricii salii
            List<Seat> seats = new List<Seat>();
            seats = await _context.Seats.Where(x => x.hallId == screening.hallId).OrderBy(x=>x.order).ToListAsync();

            var notickets = total;
            float price = 0;
            //se calculeaza pretul total al biletelor din "cos"
            foreach (var ticket in so.tickets)
            {
                var type = await _context.TicketType.Where(x => x.id == ticket.Key).FirstOrDefaultAsync();
                price = price + (screening.price - (screening.price * type.discount))*ticket.Value;
            }

            ChooseSeatsViewModel model = new ChooseSeatsViewModel()
            {
                screeningId = screening.Id,
                seats= seats ,
                reserved=reserved,
                notickets=notickets,
                cols=cols,
                total=price,
            };
            if (so.details != null)
            {
                model.nume = so.details.nume;
                model.prenume = so.details.prenume;
                model.email = so.details.email;
                model.telefon = so.details.telefon;
                //model.userId = so.details.userId;
                model.selectedseats = so.details.selectedseats;
            }
            else 
            if (User.Identity.IsAuthenticated)
            { 
                var user = await _context.Users.FindAsync(_userManager.GetUserId(User));
                if (User.IsInRole("Client"))
                {
                    model.nume = user.FirstName;
                    model.prenume = user.LastName;
                    model.email = user.Email;
                    model.telefon = user.PhoneNumber;
                    model.userId = user.Id;

                }
                else
                {
                    model.nume = "Client";
                    model.prenume = "in cinema";
                    model.email = "cinemawebsitelicense@gmail.com";
                    model.telefon ="0746342020";

                }

            }

                if (TempData["ErrorMessage"] != null)
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
                return View(model);
        }







        //metoda apelata pentru rezervarea locurilor selectate
        [HttpPost]
        [Route("Screening/ChooseSeats/{id}")]
        public async Task<IActionResult> ChooseSeats(ChooseSeatsViewModel details)
        {
            //se gaseste ecranizarea
            var screening = await _context.Screenings.Where(x => x.Id == details.screeningId).Include(x => x.hall).FirstOrDefaultAsync();

            //biletele selectate salvate in sesiune- se preia obiectul salvat ibn sesiune 
            SessionObject so = HttpContext.Session.Get<SessionObject>(Convert.ToString(details.screeningId));

            
            List<int> ids = new List<int>();

            List<string> selectedseats = details.selectedseats.Split(',').ToList();
            int total = 0;
            foreach (var ticket in so.tickets)
            {
                total = total + ticket.Value;
            }

            if (!ModelState.IsValid)
            {
                int cols = await _context.Halls.Where(x => x.id == screening.hallId).Select(x => x.columns).FirstOrDefaultAsync();

                //lista de id-uri care reprezinta scaune rezervate
                List<int> reserved = new List<int>();
                reserved = await _context.Seats.Join(_context.ReservedSeats, seat => seat.Id, ReservedSeats => ReservedSeats.seatId, (seat, ReservedSeats) => new { seat, ReservedSeats }).Join(_context.Reservations, x => x.ReservedSeats.reservationId, Reservations => Reservations.id, (x, Reservations) => new { x, Reservations }).Where(x => x.Reservations.screeningId == screening.Id).Select(x => x.x.seat.Id).ToListAsync();

                //lista de SCAUNE DIN SALA ECRANIZARII
                List<Seat> seats = new List<Seat>();
                seats = await _context.Seats.Where(x => x.hallId == screening.hallId).OrderBy(x => x.Id).ToListAsync();

                var notickets = total;

                float price = 0;
                //se calculeaza pretul total al biletelor din "cos"
                foreach (var ticket in so.tickets)
                {
                    var type = await _context.TicketType.Where(x => x.id == ticket.Key).FirstOrDefaultAsync();
                    price = price + (screening.price - (screening.price * type.discount)) * ticket.Value;
                }


                details.cols = cols;
                details.notickets = notickets;
                details.reserved = reserved;
                details.seats = seats;
                details.total = price;

                if (User.Identity.IsAuthenticated)
                {
                    var user = await _context.Users.FindAsync(_userManager.GetUserId(User));
                    if (User.IsInRole("Client"))
                    {
                        details.userId = user.Id;
                    }
                }
                return View(details);
            }

            VerifyModelError verification = await VerifyModelAsync(details);
            if (!verification.isValid)
            {
                TempData["ErrorMessage"] = verification.message;
                return RedirectToAction(verification.action, new { id = details.screeningId });
            }

            if (details.action == "Cumpără bilete")
            {
                //se baga modelul in seiune....se verifica ecranizarea , nr de bilete si modelul daca exitsa-> se verifica modelul si se ace plata,,la choose seats sa se verifice daca exista model alvat in sesiune si daca da sa se returneze pagina cu modelul respectiv...
                so.details = details;
                HttpContext.Session.Set<SessionObject>(Convert.ToString(details.screeningId),so);
               
                return RedirectToAction("Buy", new { id = details.screeningId });
            }
            else
            {
                int k = 0;
                //valoarea de bilete de un tip deja adaugate in bd
                int val = 0;

                using var transaction = _context.Database.BeginTransaction();

                try
                {
                    //se creaza o rezervare noua
                    Reservation reservation = new Reservation()
                    {
                        screeningId = details.screeningId,
                        firstName = details.prenume,
                        lastName = details.nume,
                        email = details.email,
                        phone = details.telefon,
                        UserId = details.userId == null ? null : details.userId,
                        date = DateTime.Now,

                    };

                    if (User.Identity.IsAuthenticated)
                    {
                        if (User.IsInRole("Administrator"))
                        {
                            reservation.payed = true;
                            reservation.online = false;
                        }
                    }
                    else
                    {
                        reservation.payed = false;
                        reservation.online = true;
                    }

                    _context.Reservations.Add(reservation);
                    await _context.SaveChangesAsync();
                    var reservationId = reservation.id;
                    //se creaza rezervarile de locuri pentru fiecare loc din lista
                    foreach (var seat in selectedseats)
                    {
                        ReservedSeats rs = new ReservedSeats();
                        rs.seatId = Convert.ToInt32(seat);
                        rs.reservationId = reservationId;
                        rs.TicketTypeid = so.tickets[k].Key;
                        _context.ReservedSeats.Add(rs);

                        ids.Add(rs.Id);
                        val++;
                        if (val == so.tickets[k].Value)
                        {
                            k++;
                            val = 0;
                        }
                    }
                    await _context.SaveChangesAsync();
                    //se goleste sesiunea
                    HttpContext.Session.Set<SessionObject>(Convert.ToString(details.screeningId), null);

                    ViewBag.Succes = "Rezervarea a fost realizată cu succes! Puteți verifica daca ați primit email de confirmare.";

                    transaction.Commit();
                    if (User.Identity.IsAuthenticated)
                    {
                        if (User.IsInRole("Administrator"))
                        {
                            TempData["SuccesMessage"] = "Rezervarea a fost creată cu succes!";
                            TempData["ids"] = string.Join(",", ids);
                            return RedirectToAction("Reservations", new { id = details.screeningId });
                        }
                        else
                        {
                            var movie = await _context.Movies.Where(x => x.id == screening.movieId).FirstOrDefaultAsync();
                            await _emailSender.SendEmailAsync(details.email, "Confirmare rezervare",
                           "<h2>Mulțumim că ați realizat rezervare la cinematograful nostru!</h2> Ați rezervat " + total + " bilete la filmul " + movie.title + " care incepe la ora " + screening.s_hour.ToString("HH:mm") + ". <br/> Vă așteptăm cu 30 de minute îaninte de începerea filmului pentru a vă revendica biletele!");
                        }

                    }
                    else
                    {
                        //se trimite email de confirmare
                        var movie = await _context.Movies.Where(x => x.id == screening.movieId).FirstOrDefaultAsync();
                        await _emailSender.SendEmailAsync(details.email, "Confirmare rezervare",
                       "<h2>Multumim ca ati realizat rezervare la cinematograful nostru!</h2> Ati rezervat " + total + " bilete la filmul " + movie.title + " care incepe la ora " + screening.s_hour.ToString("HH:mm") + ". <br/> Va asteptam cu 30 de minute ianinte de inceperea filmului pentru a va revendica biletele!");

                    }
                    
                    return View("Reserve");  
                }

                catch (Exception)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "O eroare a intervenit în crearea sălii și procesul a eșuat! Mai încercați odată!";
                    return RedirectToAction("List");
                }
            }
        }

        [Route("Screening/Buy/{id}")]
        public async Task<IActionResult> BuyAsync(int id)
        {
               
                SessionObject so = HttpContext.Session.Get<SessionObject>(Convert.ToString(id));
                if(so==null || so.details==null || so.tickets == null)
                {
                     ViewBag.ErrorMessage = "Nu exista suficiente date pentru a putea realiza plata! Intoarceti-va la pagina de selectare bilete si la cea de alegere de locuri si completati toate datele necesare dupa care puteti realiza plata online!";
                    return View();
                }
                ChooseSeatsViewModel details = so.details;
                var screening = await _context.Screenings.Where(x => x.Id == details.screeningId).Include(x => x.hall).FirstOrDefaultAsync();
                int total = 0;
                foreach (var ticket in so.tickets)
                {
                    total = total + ticket.Value;
                }

                float totalSumToPay = 0;
                //se calculeaza totalul care trebuie platit
                foreach (var ticket in so.tickets)
                {
                    var t = await _context.TicketType.FindAsync(ticket.Key);
                    totalSumToPay = totalSumToPay + (screening.price - screening.price * t.discount) * ticket.Value;
                }

                //nr de bilete din sesiune si suma totala care trebuie platita
                details.notickets = total;
                details.total = totalSumToPay;
                details.screeningId = id;
                return View(details);                      
        }


        [HttpPost]
       
        public async Task<IActionResult> ChargeAsync(string Token, int id)
        {

            SessionObject so = HttpContext.Session.Get<SessionObject>(Convert.ToString(id));
            ChooseSeatsViewModel details = so.details;
            var screening = await _context.Screenings.Where(x => x.Id == details.screeningId).Include(x => x.hall).FirstOrDefaultAsync();

            List<int> ids = new List<int>();

            List<string> selectedseats = details.selectedseats.Split(',').ToList();
            int total = 0;
            foreach (var ticket in so.tickets)
            {
                total = total + ticket.Value;
            }


            float totalSumToPay = 0;
            //se calculeaza totalul care trebuie platit
            foreach (var ticket in so.tickets)
            {
                var t = await _context.TicketType.FindAsync(ticket.Key);
                totalSumToPay = totalSumToPay + (screening.price - screening.price * t.discount) * ticket.Value;
            }


            if (!ModelState.IsValid)
            {
                int cols = await _context.Halls.Where(x => x.id == screening.hallId).Select(x => x.columns).FirstOrDefaultAsync();

                //lista de id-uri care reprezinta scaune rezervate
                List<int> reserved = new List<int>();
                reserved = await _context.Seats.Join(_context.ReservedSeats, seat => seat.Id, ReservedSeats => ReservedSeats.seatId, (seat, ReservedSeats) => new { seat, ReservedSeats }).Join(_context.Reservations, x => x.ReservedSeats.reservationId, Reservations => Reservations.id, (x, Reservations) => new { x, Reservations }).Where(x => x.Reservations.screeningId == screening.Id).Select(x => x.x.seat.Id).ToListAsync();

                //lista de SCAUNE DIN SALA ECRANIZARII
                List<Seat> seats = new List<Seat>();
                seats = await _context.Seats.Where(x => x.hallId == screening.hallId).OrderBy(x => x.Id).ToListAsync();

                var notickets = total;

                float price = 0;
                //se calculeaza pretul total al biletelor din "cos"
                foreach (var ticket in so.tickets)
                {
                    var type = await _context.TicketType.Where(x => x.id == ticket.Key).FirstOrDefaultAsync();
                    price = price + (screening.price - (screening.price * type.discount)) * ticket.Value;
                }
             

                details.cols = cols;
                details.notickets = notickets;
                details.reserved = reserved;
                details.seats = seats;
                details.total = price;

                if (User.Identity.IsAuthenticated)
                {
                    var user = await _context.Users.FindAsync(_userManager.GetUserId(User));
                    if (User.IsInRole("Client"))
                    {
                        details.userId = user.Id;
                    }
                }
                return View(details);
            }

            VerifyModelError verification = await VerifyModelAsync(details);
            if (!verification.isValid)
            {
                TempData["ErrorMessage"] = verification.message;
                return RedirectToAction(verification.action, new { id = details.screeningId });
            }


            //aici s-au terminat verificarile

            //tranzactie
            using var transaction = _context.Database.BeginTransaction();
            try
            {


                //se incearca plata 
                var customers = new CustomerService();
                var charges = new ChargeService();

                var customer = customers.Create(new CustomerCreateOptions
                {
                    Email = details.email,
                    Source = Token,
                    Phone = details.telefon,
                    Name = details.nume + " " + details.prenume
                });

                var charge = charges.Create(new ChargeCreateOptions
                {
                    Amount = (long)(totalSumToPay * 100),
                    Description = "Plata bilete",
                    Currency = "ron",
                    Customer = customer.Id,
                    ReceiptEmail = details.email,

                });

                //se verifica daca plata s-a realizat cu succes
                if (charge.Status != "succeeded")
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Plata a esuat, va rugam sa incercati din nou!";
                    return RedirectToAction("ChooseSeats", new { id = details.screeningId });
                }

                //se salveaza rezervarea si se trimite email
                int k = 0;
                int val = 0;
                int i = 0;
             
                //se creaza o rezervare noua
                Reservation reservation = new Reservation()
                {
                    screeningId = details.screeningId,
                    firstName = details.prenume,
                    lastName = details.nume,
                    email = details.email,
                    phone = details.telefon,
                    UserId = details.userId == null ? null : details.userId,
                    date = DateTime.Now,

                };

                if (User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole("Administrator") || User.IsInRole("Angajat") || User.IsInRole("Manager"))
                    {
                        reservation.payed = true;
                        reservation.online = false;
                    }
                }
                else
                {
                    reservation.payed = false;
                    reservation.online = true;
                }

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();
                var reservationId = reservation.id;
                var movie = await _context.Movies.Where(x => x.id == screening.movieId).FirstOrDefaultAsync();
                string is3d = " ";


                if (screening.is3D == true)
                {
                    is3d = "3D";
                }
                else
                {
                    is3d = "2D";
                }


                var client = new SendGridClient(Options.SendGridKey);
                //compunere mesaj
                var mesage = "<h1>Confirmare tranzactie</h1> <br/><br/> <h3>Multumim ca ati achizitionat bilete de la noi!</h3><br/><br/><b>Te așteptăm la film!</b><br/> <br/> Pentru a avea acces la film puteti opta pentru una din cele doua variante: <br/> a) e-ticket ( bilet electronic): prezintă la intrarea în sală biletul atașat email-ului de confirmare și unul dintre plasatorii noștri îți va scana QR code-ul pentru a valida biletul. <br/><br/> b) ridicare din cinema: te rugăm să te prezinți la casierie pentru a - ți ridica biletele pe baza e-mailul de confirmare. <br/><br/>Înainte de film, te invităm să te bucuri de produsele noastre delicioase de la bar! <h2>Detalii</h2><p><b>Data si ora ecranizarii: </b>" + screening.date.ToString("dd-MM-yyyy") + " " + screening.s_hour.ToString("HH:mm") + "</p><p><b>Film: </b>" + movie.title + " " + is3d + "</p><p><b>Sala: </b>" + screening.hall.name + "</p><p><b>Numar bilete: </b>" + selectedseats.Count + "</p><p><b>Total: </b>" + totalSumToPay + " lei </p>";
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress("cinemawebsitelicense@gmail.com", "CinemaWebsite"),
                    Subject = "Confirmare plata",
                    PlainTextContent = mesage,
                    HtmlContent = mesage
                };

                msg.AddTo(new EmailAddress(details.email));

                msg.SetClickTracking(false, false);

                //bibleoteca CoreHtmlToImage , convertor de html-> img
                var converter = new HtmlConverter();


                //se initializeaza un byte array care va contine logo-ul cinematografului sub forma de bytes
                byte[] fileContent = null;

                //deschide fisierul png care contine logo ul cinematografului in modul read
                FileStream fs = new FileStream("wwwroot/cinema-logo.png", FileMode.Open, FileAccess.Read);

                //se initializeaza o clasa care citeste bitii fisierului primit ca argument
                BinaryReader binaryReader = new BinaryReader(fs);

                //se afla lungimea fisirului care contine logo-ul
                long byteLength = new FileInfo("wwwroot/cinema-logo.png").Length;

                //se citesc bitii fisierului
                fileContent = binaryReader.ReadBytes((Int32)byteLength);

                //se convertesc bitii to string cre poate fi primit ca argument la src
                var base64 = Convert.ToBase64String(fileContent);
                var imgSrc = String.Format("data:image/png;base64,{0}", base64);

                //pt fiecare scaun selectat se creaza scaunul in bd si se creaza cate o imagine si un qr pt fiecare bilet
                foreach (var seat in selectedseats)
                {
                    //sa se genereze biletele electronice si sa se adauge la email
                    ReservedSeats rs = new ReservedSeats();
                    rs.seatId = Convert.ToInt32(seat);
                    rs.reservationId = reservationId;
                    rs.TicketTypeid = so.tickets[k].Key;
                    _context.ReservedSeats.Add(rs);
                    await _context.SaveChangesAsync();
                    ids.Add(rs.Id);
                    //nr de bilete dintr-un tip de bilete 
                    val++;
                    //cate bilete s au creat deja 
                    i++;
                    var ticket = await _context.TicketType.FindAsync(so.tickets[k].Key);
                    var type = ticket.name;
                    if (val == so.tickets[k].Value)
                    {
                        //indexul de la lista de bilete 
                        k++;
                        val = 0;
                    }

                    var qrimg = CreateQR(rs.Id);

                    //randul si locul biletului
                    var s = await _context.Seats.FindAsync(Convert.ToInt32(seat));
                    int row = s.row;
                    var seatnumber = s.seatNo;




                    //se creaza biletul electronic
                    var html = "<div style='border:5px solid blue;width:450px;padding:5px;'>";
                    html = html + "<div style='border-bottom:2px solid blue;overflow:hidden;'>";
                    html = html + "<img src='" + imgSrc + "' height='60' style='margin: 5px ;display:inline;float:left;'/>";
                    html = html + "<div style='float:right;margin:5px;'>";
                    html = html + "<p style='color:blue;display:inline;'><b>Film:</b></p>";
                    html = html + "<p style='display:inline;'>" + movie.title + " " + is3d + "</p>";
                    html = html + "<br/>";
                    html = html + "<p style='color:blue;display:inline;'><b>Data:</b></p>";
                    html = html + "<p style='display:inline;'>" + screening.date.ToString("dd-MM-yyyy") + "</p>";
                    html = html + "<br/>";
                    html = html + "<p style='color:blue;display:inline;'><b>Ora:</b></p>";
                    html = html + "<p style='display:inline;'>" + screening.s_hour.ToString("HH:mm") + "</p>";
                    html = html + "</div>";
                    html = html + "</div>";
                    html = html + "<div style='padding: 10px 50px 10px 10px;border-bottom:2px solid blue;overflow:hidden;'>";
                    html = html + "<div style='display:inline;float:left;'>";
                    html = html + "<p style='color:blue;margin:0;'><b>Sala:</b></p>";
                    html = html + "<p style='color:blue;margin:0;'><b>Rand:</b></p>";
                    html = html + "<p style='color:blue;margin:0;'><b>Loc:</b></p>";
                    html = html + "<p style='color:blue;margin:0;'><b>Tip bilet:</b></p>";
                    html = html + " </div>";
                    html = html + " <div style='display:inline;float:left;padding:0 10 0 10'>";
                    html = html + "<p style='margin:0;'>" + screening.hall.name + "</p>";
                    html = html + "<p style='margin:0;'>" + row + "</p>";
                    html = html + "<p style='margin:0;'>" + seatnumber + "</p>";
                    html = html + "<p style='margin:0;'>" + type + "</p>";
                    html = html + " </div>";
                    html = html + "<img src='" + qrimg + "' height='120' style='margin: 0px 20px 0px 0px;display:inline;float:right;'/>";
                    html = html + "</div>";
                    html = html + "Biletele cu pret redus permit intrarea doar cu un act doveditor valid. Va rugam sa prezentati actul plasatorilor nostri.";
                    html = html + "</div>";


                    //se converteste html to biti o imagine cu latimea 440
                    var bytes = converter.FromHtmlString(html, 470);
                    //se convertesc bitii to string
                    var img = Convert.ToBase64String(bytes);
                    msg.AddAttachment("Bilet" + i + ".jpg", img);
                }

                reservation.payed = true;
                await _context.SaveChangesAsync();
                transaction.Commit(); 
                await client.SendEmailAsync(msg);
                //se goleste sesiunea
                HttpContext.Session.Set<SessionObject>(Convert.ToString(details.screeningId), null);
                ViewBag.Succes = "Rezervarea a fost realizată cu succes! Puteți verifica dacă ați primit email de confirmare cu biletele electronice.";
                //se tr emailuldwed
                return View("Reserve");
            }
            catch
            {
                transaction.Rollback();
                TempData["ErrorMessage"] = "Plata a esuat, va rugam incercati din nou cu alt card!";
                return RedirectToAction("ChooseSeats", new { id =details.screeningId});
            }
               

         
          
        }

        [Authorize(Roles = "Administrator,Manager,Angajat")]
        public async Task<IActionResult> List(DateTime? date, string q)
        {
            List<ScreeningAdminList> filme = new List<ScreeningAdminList>();
            if (q != null)
            {
                q = q.ToLower();
             
                var movies= await _context.Movies.Where(x => x.title.ToLower().Contains(q)).Select(x=>x.id).ToListAsync();
                filme = await _context.Movies.Join(_context.Screenings, m => m.id, s => s.movieId, (m, s) => new ScreeningAdminList
                {
                    mid = m.id,
                    title = m.title,
                    img = m.img,
                    date = s.date,
                    sid = s.Id,
                    shour = s.s_hour,
                    fhour = s.f_hour,
                    is3d = s.is3D,
                    hallid = s.hallId,
                    hall = s.hall
                }).Where(x => x.date == date).Where(x=>movies.Contains(x.mid)).OrderBy(x => x.hallid).ThenBy(x => x.shour.Hour).ToListAsync();
                if (filme.Count <= 0)
                {
                    ViewBag.Message = "Nu există filme programate cu acest titlu la data selectată";

                    return View();

                }
            }
            else
            {
                //se iau filmele si ecranizarile filmelor care ruleaza in data selectata
                filme = await _context.Movies.Join(_context.Screenings, m => m.id, s => s.movieId, (m, s) => new ScreeningAdminList
                {
                    mid = m.id,
                    title = m.title,
                    img = m.img,
                    date = s.date,
                    sid = s.Id,
                    shour = s.s_hour,
                    fhour = s.f_hour,
                    is3d = s.is3D,
                    hallid = s.hallId,
                    hall = s.hall
                }).Where(x => x.date == date).OrderBy(x => x.hallid).ThenBy(x => x.shour.Hour).ToListAsync();
             if (filme.Count <= 0)
            {
                ViewBag.Message = "Încă nu s-au programat filme în data selectată";
                   
                    return View();

            }
            
            
            }
            if (TempData["SuccesMessage"] != null)
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
            if (TempData["ErrorMessage"] != null)
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            return View(filme);

        }




        [Authorize(Roles = "Administrator,Manager,Angajat")]
        [HttpGet]
        public async Task<IActionResult> Create(DateTime? date)
        {
          
           var movies = await _context.Movies.Where(x => x.active == true).ToListAsync();
           var halls = await _context.Halls.Where(x=>x.active==true).ToListAsync();
            ViewBag.movies = movies;
            ViewBag.halls = halls;
            
            CreateScreeningViewModel model = new CreateScreeningViewModel();
            if(date!=null)
            model.date = (DateTime)date;
            return View(model);
        }




        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateScreeningViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var movies = await _context.Movies.Where(x => x.active == true).ToListAsync();
                var halls = await _context.Halls.Where(x => x.active == true).ToListAsync();
                ViewBag.movies = movies;
                ViewBag.halls = halls;
                return View();
            }
            else
            {
                var movie = await _context.Movies.FindAsync(model.movieId);
                if(movie==null)
                {
                    var movies = await _context.Movies.Where(x => x.active == true).ToListAsync();
                    var halls = await _context.Halls.Where(x => x.active == true).ToListAsync();
                    ViewBag.movies = movies;
                    ViewBag.halls = halls;
                    ViewBag.ErrorMessage = "Filmul selectat nu există în baza de date";
                    return View();
                }

                var hall = await _context.Halls.FindAsync(model.hallId);
                if (hall == null)
                {
                    var movies = await _context.Movies.Where(x => x.active == true).ToListAsync();
                    var halls = await _context.Halls.Where(x => x.active == true).ToListAsync();
                    ViewBag.movies = movies;
                    ViewBag.halls = halls;
                    ViewBag.ErrorMessage = "Sala selectată nu există în baza de date";
                    return View();
                }

                //se verifica daca ora selectata e buna
                var screenings = _context.Screenings.Where(x => x.hallId == model.hallId).Where(x => x.date.Date == model.date.Date).OrderBy(x => x.s_hour).ToList();
                DateTime f_hour = model.s_hour.AddMinutes(movie.duration);

                
                foreach(var screening in screenings)
                {   //se verifica daca ora de inceput aleasa nu se incadreaza intre doua ore de inceput-final ale unei ecranizari
                    //sau daca ora finala nu se incadreaza intre ora de inceput final ale unei ecranizari
                    //sau daca o ecranizare deja existenta nu este inclusa in timpul de rulare al ecranizarii care se doreste sa se creeze
                    if ((model.date.AddMinutes(model.s_hour.Minute).AddHours(model.s_hour.Hour) > model.date.AddMinutes(screening.s_hour.Minute).AddHours(screening.s_hour.Hour) && model.date.AddMinutes(model.s_hour.Minute).AddHours(model.s_hour.Hour) < model.date.AddMinutes(screening.f_hour.Minute).AddHours(screening.f_hour.Hour)) ||
                        (model.date.AddMinutes(f_hour.Minute).AddHours(f_hour.Hour) > model.date.AddMinutes(screening.s_hour.Minute).AddHours(screening.s_hour.Hour) && model.date.AddMinutes(f_hour.Minute).AddHours(f_hour.Hour) < model.date.AddMinutes(screening.f_hour.Minute).AddHours(screening.f_hour.Hour)) ||
                        (model.date.AddMinutes(screening.s_hour.Minute).AddHours(screening.s_hour.Hour) > model.date.AddMinutes(model.s_hour.Minute).AddHours(model.s_hour.Hour) && model.date.AddMinutes(screening.s_hour.Minute).AddHours(screening.s_hour.Hour) < model.date.AddMinutes(f_hour.Minute).AddHours(f_hour.Hour)) ||
                        (model.date.AddMinutes(screening.f_hour.Minute).AddHours(screening.f_hour.Hour) > model.date.AddMinutes(model.s_hour.Minute).AddHours(model.s_hour.Hour) && model.date.AddMinutes(screening.f_hour.Minute).AddHours(screening.f_hour.Hour) < model.date.AddMinutes(f_hour.Minute).AddHours(f_hour.Hour))
                       || model.date.AddHours(model.s_hour.Hour)> model.date.AddHours(23) || model.date.AddHours(model.s_hour.Hour) < model.date.AddHours(9)
                        )
                    {
                        var movies = await _context.Movies.Where(x => x.active == true).ToListAsync();
                        var halls = await _context.Halls.ToListAsync();
                        ViewBag.movies = movies;
                        ViewBag.halls = halls;
                        ViewBag.ErrorMessage = "Ora selectata se suprapune cu alte ecranizări sau nu se potrivește cu orarul de funcționare al cinematografului, alegeți altă oră!";
                        return View();

                    }
                }

                Screening s = new Screening();
                s.hallId = model.hallId;
                s.movieId = model.movieId;
                s.price = model.price;
                s.is3D = model.is3D;
                s.date = model.date;
                s.s_hour = model.s_hour;
                s.f_hour = f_hour;
                _context.Screenings.Add(s);
                await _context.SaveChangesAsync();
                TempData["SuccesMessage"] = "Ecranizarea a fost creata cu succes!";
                var date = model.date.ToString("yyyyy-MM-dd");
                return RedirectToAction("List", new { date = date });


            }
        }





        [Authorize(Roles = "Administrator,Manager,Angajat")]
        [HttpGet]
        //se returneaza intervalele orare la care poate fi creata o ecranizare noua
        public string GetHours(DateTime date, int hallid)
        {
            //select toate ecranizarile de genu ordonate dupa ora la care incep
            var screenings = _context.Screenings.Where(x => x.hallId == hallid).Where(x => x.date.Date == date.Date).OrderBy(x => x.s_hour.Hour).ToList();
            if (screenings.Count == 0)
            {
                return "09:00-23:00";

            }
            int i = 0;
            string[] sh = new string[screenings.Count];
            string[] fh = new string[screenings.Count];
            foreach (var screening in screenings)
            {

                sh[i] = screening.s_hour.ToString("HH:mm");
                fh[i] = screening.f_hour.ToString("HH:mm"); ;
                i++;
           }
            string result = "";
            if (date.AddHours(screenings[0].s_hour.Hour) > date.AddHours(9))
                result = result+"09:00-" + sh[0]+"</br>";
           
            for (var j = 0; j < screenings.Count-1; j++)
            {
             if(j== screenings.Count - 2)
                    result = result + fh[j] + "-" + sh[j + 1];
             else
                result = result + fh[j] + "-" + sh[j + 1] + "</br>";
            }
           
             if(date.AddHours(screenings[screenings.Count-1].f_hour.Hour).AddMinutes(screenings[screenings.Count - 1].f_hour.Minute) < date.AddHours(23))
                        {
                            result = result + "</br>"+fh[screenings.Count - 1] + "-" + "23:00";
                        }
            return result;
           
        }




        [Route("Screening/Reservations/{id}")]
        [Authorize(Roles = "Administrator,Manager,Angajat")]
        //rezervarile aferente unei ecranizari selectate
        public async Task<IActionResult> Reservations(int id, string q, int p)
        {

            if (p == null || p == 0)
            {
                p = 1;
            }

            //se selecteaza toate rezervarile de la screening-ul selectat
            List<Reservation> reservations =  _context.Reservations.Where(x => x.screeningId == id).Include(x=>x.reservedSeats).ThenInclude(x=>x.TicketType).ToList();
               if (q != null)
            {
                q = q.ToLower();
                reservations = reservations.Where(x => (x.firstName.ToLower().Contains(q) || x.lastName.ToLower().Contains(q)|| (x.firstName+" "+x.lastName).ToLower().Contains(q)|| (x.lastName + " " + x.firstName).ToLower().Contains(q) )).ToList();
            }
              
            var screening = await _context.Screenings.Include(x=>x.movie).Include(x=>x.hall).Where(x=>x.Id==id).FirstOrDefaultAsync(); 
            ScreeningReservations model = new ScreeningReservations();
            if (screening == null)
            {
                ViewBag.ErrorMessage = "Ecranizarea cautată nu există!";
               
               
                return View();
            }
                model.screeningid = id;
                model.title = screening.movie.title;
                model.s_hour = screening.s_hour;
                model.f_hour = screening.f_hour;
                model.price = screening.price;
           
            if (reservations.Count == 0)
            {
                ViewBag.Message = "Nu există rezervări la momentul de față!";
         
                if (TempData["SuccesMessage"] != null)
                    ViewBag.SuccesMessage = TempData["SuccesMessage"];
                return View(model);
            }

          
           
           //lista de rezervari
            List<ScreeningReservationInfo> modelreservations = new List<ScreeningReservationInfo>();

            //pentru fiecare rezervare de la ecranizarea respectiva
            foreach(var reservation in reservations)
            {   //se creaza o rezervare noua pt model
                ScreeningReservationInfo modelreservation = new ScreeningReservationInfo();
              
                modelreservation.id = reservation.id;
                modelreservation.payed = reservation.payed;
                modelreservation.FirstName = reservation.firstName;
                modelreservation.LastName = reservation.lastName;
                modelreservation.email = reservation.email;
                modelreservation.totaltickets = reservation.reservedSeats.Count;

                //se calculeaza pretul rezervarii si locurile
        
                float total = 0;
               
                //ipurile de bilete si nr lor
                 List<TypeNameNumber> ticketsTypes = new List<TypeNameNumber>();
                                ticketsTypes= reservation.reservedSeats.GroupBy(x=>x.TicketType.name).Select(g => new TypeNameNumber
                                {
                                    typeOfTicket = g.Key,
                                    noOfTickets = g.Count()
                                }).ToList();

                modelreservation.ticketTypes = ticketsTypes;


                Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
                List<int> unclaimedticketsids = new List<int>();
                List<Tickets> tickets = new List<Tickets>();

                //pt fiecare bilet -> se adauga pretul biletului la total
                foreach (var ticket in reservation.reservedSeats)
                {

                    total = total + (reservation.screening.price - (reservation.screening.price) * ticket.TicketType.discount);
                    var seat = await _context.Seats.Where(x => x.Id == ticket.seatId).FirstOrDefaultAsync();
                       Tickets t = new Tickets();
                                t.type = ticket.TicketType.name;
                                t.row = seat.row;
                                t.seatNo = (int)seat.seatNo;
                                t.id = ticket.Id;
                                t.claimed = ticket.claimed;
                    tickets.Add(t);
                    int  row = seat.row;
                    if (!dict.ContainsKey(row))
                    {
                            dict.Add(row, new List<int>());                      
                            dict[row].Add((int)seat.seatNo);               
                    }
                    else
                        dict[row].Add((int)seat.seatNo);

                    if(ticket.claimed==false)
                        unclaimedticketsids.Add(ticket.Id);
                }
              
                modelreservation.total = total;              
                modelreservation.seats = dict;
                modelreservation.tickets = tickets;
                modelreservation.ticketsids = unclaimedticketsids;
                modelreservations.Add(modelreservation);
            }
            model.reservations = modelreservations;


            //paginarea
            if (model.reservations.Count % 5 == 0)
                ViewBag.noPages = (model.reservations.Count / 5);
            else
                ViewBag.noPages = (model.reservations.Count / 5)+1;
            model.reservations = model.reservations.Skip((p - 1) * 5).Take(5).ToList();
            if (model.reservations.Count == 0)
            {
                ViewBag.ErrorMessage = "Nu au fost găsite rezervări!";
                return View();
            }
            ViewBag.currPage = p;


            if (TempData["SuccesMessage"] != null)
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
            if(TempData["ids"] !=null)
                ViewBag.ids= TempData["ids"];
            return View(model);

        }






        [Route("Screening/DeleteConfirmation/{id}")]
        [Authorize(Roles = "Administrator,Manager,Angajat")]
        [HttpGet]
        public async Task<IActionResult> DeleteConfirmation(int id)
        {
            var screening = await _context.Screenings.FindAsync(id);
            if (screening == null)
            {
                TempData["ErrorMessage"] = "Ecranizarea care se vrea să fie ștearsă nu a fost găsită!";
                return RedirectToAction("List", new { date = DateTime.Now.ToString("yyyy-MM-dd") });
            }
            return View(id);
        }





        [Route("Screening/Delete/{id}")]
        [Authorize(Roles = "Administrator,Manager,Angajat")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var screening = await _context.Screenings.Where(x=>x.Id==id).Include(x=>x.movie).FirstOrDefaultAsync();
           
            if (screening == null)
            {
                TempData["ErrorMessage"] = "Ecranizarea care se vrea să fie ștearsă nu a fost găsită!";
                return RedirectToAction("List", new { date = DateTime.Now.ToString("yyyy-MM-dd") });
            }
     
            //daca ecranizarea are o data viitoare atunci se trimite mail tuturor clientilor care au realizat deja rezervare la ecranizarea stearsa
            if (screening.date > DateTime.Now)
            {
                var emails = await _context.Reservations.Where(x => x.screeningId == id).Select(x => x.email).ToListAsync();
                List<EmailAddress> emaillist = new List<EmailAddress>();
                foreach (var email in emails)
                {
                    emaillist.Add(new EmailAddress(email));
                }

                var message = "<h4>Ecranizarea de la filmul " + screening.movie.title + ", din data de " + screening.date.ToString("dd-MM-yyyy") + ", ora " + screening.date.ToString("HH:mm") + " la care ați avut rezervare sau ați achizitionat bilete a fost anulată!</h4><br/>Ne pare rau pentru inconvenientele create dar din pacate, spectacolul nu mai poate avea loc din cauza unor motive exceptionale. <br/> In caz ca ati achizitionat deja bilete va rugam sa va prezentati la cinematograf cu acest email doveditor pentru a va restitui suma de bani pierduta.<br/>Cu mult respect, echipa CinemaWebsite";

                var client = new SendGridClient(Options.SendGridKey);
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress("cinemawebsitelicense@gmail.com", "CinemaWebsite"),
                    Subject = "Anulare ecranizare",
                    PlainTextContent = message,
                    HtmlContent = message

                }; 
                msg.SetClickTracking(false, false);
                foreach (var email in emaillist)
                    msg.AddTo(email);
                await client.SendEmailAsync(msg);
            }
            //se sterge ecranizarea
            _context.Screenings.Remove(screening);
            await _context.SaveChangesAsync();
            TempData["SuccesMessage"] = "Ecranizarea a fost ștearsă cu succes!";
            return RedirectToAction("List", new { date = screening.date.ToString("yyyy-MM-dd") });
        }



   
    }
}
