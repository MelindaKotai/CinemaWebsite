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



        public async Task<IActionResult> IndexAsync(DateTime? date)
        {    

            
                    if (date <= DateTime.Now.AddDays(-1) || date > DateTime.Now.AddDays(6))
                    {
                        ViewBag.UserMessage = "Nu exista filme disponibile in data selectata";
                        return View();
                    }
                

            if (date == null)
            {
                ViewBag.UserMessage = "Selectati o zi";
                return View();
            }

        
              //se iau filmele si ecranizarile filmelor care ruleaza in data selectata
                var filme = await _context.Movies.Join(_context.Screenings, m => m.id, s => s.movieId, (m, s) => new { id = m.id, title = m.title,
             
                    img = m.img,date = s.date, sid = s.Id,hour=s.s_hour ,is3d=s.is3D}).Where(x => x.date == date).OrderBy(x=>x.id).ToListAsync();



       
               
                List<ScreeningMovies> movies = new List<ScreeningMovies>();
                //se contruieste lista de obiecte ScreeningMovies care vor fi returnate la View
                var id = filme[0].id;
                //lista cu informatii e ecranizari(o lista de ecranizari pt fiecare film)
                List<List<ScreeningInfo>> screenings = new List<List<ScreeningInfo>>();
                List<ScreeningInfo> screening1 = new List<ScreeningInfo>();
                screenings.Add(screening1);
                //indexul listei de ecranizari
                var k = 0;

                for (var i = 0; i < filme.Count(); i++)
                {
                if (filme[i].id != id)
                {
                    //se adauga in lista doar filmele care au ecranizari disponibile
                    if (screenings[k].Count > 0)
                    {
                        ScreeningMovies movie = new ScreeningMovies() { id = id, img = filme[i - 1].img, title = filme[i - 1].title, screenings = screenings[k], date = (DateTime)date };
                        movies.Add(movie);
                    }
                    id = filme[i].id;
                    k++;
                    List<ScreeningInfo> screening2 = new List<ScreeningInfo>();
                    screenings.Add(screening2);

                    //se verifica daca data si ora curenta nu a trecut de ora ecranizarii                       
                            var nohours = filme[i].hour.Hour;
                            var nominutes = filme[i].hour.Minute;
                            var notoadd = nohours * 60 + nominutes - 30;
                            DateTime date2 = (DateTime)date;
                            if (DateTime.Now < (DateTime)date2.AddMinutes(notoadd))
                            {
                                ScreeningInfo s = new ScreeningInfo()
                                {
                                    screeningId = filme[i].sid,

                                    hour = filme[i].hour,
                                    is3D = filme[i].is3d
                                };

                                screenings[k].Add(s);
                            }

                }



                else
                {
                    
                       
                            var nohours = filme[i].hour.Hour;
                            var nominutes = filme[i].hour.Minute;
                            var notoadd = nohours * 60 + nominutes - 30;
                            DateTime date2 = (DateTime)date;
                            if (DateTime.Now < (DateTime)date2.AddMinutes(notoadd))
                            {
                                ScreeningInfo s = new ScreeningInfo()
                                {
                                    screeningId = filme[i].sid,

                                    hour = filme[i].hour,
                                    is3D = filme[i].is3d
                                };

                                screenings[k].Add(s);
                            }
                       
                }

                }
                if (screenings[k].Count > 0)
                {
                    ScreeningMovies movief = new ScreeningMovies() { id = id, img = filme[filme.Count() - 1].img, title = filme[filme.Count() - 1].title, screenings = screenings[k], date = (DateTime)date };
                    movies.Add(movief);
                }

            if (movies.Count <= 0)
            {
                ViewBag.UserMessage = "Nu exista filme disponibile in data selectata";

                return View();

            }
            return View(movies);
        }


       
        public async Task<IActionResult> MoviesAsync() {
            var movies =await  _context.Movies.Where(x => x.active == 1).ToListAsync();
            return View(movies);
        }


        public async Task<IActionResult> ComingSoonAsync()
        {
            var movies =await  _context.Movies.Where(x => x.active == 0).Where(x => x.release_date > DateTime.Now).ToListAsync();
            return View(movies);
        }



        [Route("Screening/ChooseTickets/{id}")]
        //prima pagina din pasul de rezervare: alegerea numarului si tipului de bilete
        public async Task<IActionResult> ChooseTickets(int id)
        {

            var types = await _context.TicketType.Where(x=>x.active==1).ToListAsync();
            var screening = await _context.Screenings.Where(x => x.Id == id).FirstOrDefaultAsync();
            //se verifica daca ecranizarea aleasa exista in bd
            if (screening == null)
            {
                ViewBag.ErrorMessage = "Ecranizarea aleasa nu este disponibila!";
                return View();
            }

            //pt cei nelogati si useri se verifica daca ecranizarea este in saptamana curenta pt admin sa nu verifice 
            if (User != null)
            {
                if (User.IsInRole("Client"))
                {
                    if (VerifyScreening(id))
                    {
                        ViewBag.ErrorMessage = "Ecranizarea aleasa nu este disponibila!";
                        return View();
                    }
                }
            }
            else
            {
                if (VerifyScreening(id))
                {
                    ViewBag.ErrorMessage = "Ecranizarea aleasa nu este disponibila!";
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
            if(HttpContext.Session.Get<List<KVTickets>>(Convert.ToString(screeningId))==null)
            {//daca nu exista se creaza o noua lista de bilete si se adauga biletul selectat
                //se creaza o lista noua de KVTickets
                List<KVTickets> tickets = new List<KVTickets>();
                KVTickets ticket = new KVTickets() { Key = key, Value = value };
                tickets.Add(ticket);
                HttpContext.Session.Set(Convert.ToString(screeningId),tickets);
            }
            //daca exista se adauga la lista existenta biletul selectat
            else
            {
                var tickets= HttpContext.Session.Get<List<KVTickets>>(Convert.ToString(screeningId));
               
                //se verifica daca ptr ecranizarea selectata exista in "cos" un bilet de tipul selectat
                var ticketexists = false;
                foreach(var ticket in tickets)
                {
                    if (ticket.Key == key)
                    {
                        ticketexists = true;
                    }
                }
              
                //daca exista se schimba valoare numarului de bilete din "cos" altfel se adauga un tip de bilet nou
                if (ticketexists)
                {
                    foreach (var ticket in tickets)
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
                    tickets.Add(ticket);
                }
                HttpContext.Session.Set(Convert.ToString(screeningId), tickets);
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
                        TempData["ErrorMessage"] = "Ecranizarea aleasa nu este disponibila!";
                        return View();
                    }
                }
            }
            else
            {
                if (VerifyScreening(id))
                {
                    TempData["ErrorMessage"] = "Ecranizarea aleasa nu este disponibila!";
                    return View();
                }
            }

            var screening =await _context.Screenings.Where(x => x.Id == id).FirstOrDefaultAsync();

            List<KVTickets> tickets =HttpContext.Session.Get<List<KVTickets>>(Convert.ToString(id));
            if (tickets == null)
            {
                TempData["ErrorMessage"]  = "Nu ati ales suficiente bilete!";
                return RedirectToAction("ChooseTickets", new { id = id });
            }
            
            //se verifica daca s-au selectat biletele
            int total = 0;
            foreach(var ticket in tickets)
            {
                total = total + ticket.Value;
            }
           
            if (!(total > 0))
            {
                TempData["ErrorMessage"] = "Nu ati ales suficiente bilete!";
                return RedirectToAction("ChooseTickets", new { id = id });
            }
                

            //se verifica daca in sala mai exista suficiente locuri libere 
            var noOfSeats = await _context.Seats.Where(x=>x.hallId==screening.hallId).Where(x=>x.path==0).CountAsync();
            var noOfReserved = await _context.ReservedSeats.Join(_context.Reservations, r => r.reservationId, s => s.id, (r, s) => new { r.Id, s.screeningId }).Where(x => x.screeningId == screening.Id).CountAsync();
            var noRemaining = noOfSeats - noOfReserved;
            if(total>noRemaining)
            {
                TempData["ErrorMessage"] = "Nu mai exista suficiente locuri in sala! Mai exista doar " +noRemaining+" locuri disponibile.";
                return RedirectToAction("ChooseTickets", new { id = id });
            }
            //dupa realizarea tuturor verificarilor se poate crea modelul pt pagina

            //numarul de randuri si coloane 
            var hall = await _context.Halls.Where(x => x.id == screening.hallId).FirstOrDefaultAsync();
            int rows = hall.rows;
            int cols = hall.columns;

            //lista de numere care reprezinta culoar
            List<int> paths = new List<int>();
            paths = await _context.Seats.Where(x => x.hallId == screening.hallId).Where(x => x.path == 1).Select(x => x.number).ToListAsync();

            //lista de numere care reprezinta scaune rezervate
            List<int> reserved = new List<int>();
            reserved = await _context.Seats.Join(_context.ReservedSeats, seat => seat.Id, ReservedSeats => ReservedSeats.seatId, (seat, ReservedSeats) => new { seat, ReservedSeats }).Join(_context.Reservations, x => x.ReservedSeats.reservationId, Reservations => Reservations.id, (x, Reservations) => new { x, Reservations }).Where(x => x.Reservations.screeningId == screening.Id).Select(x => x.x.seat.number).ToListAsync();

            //lista de numere care reprezinta id-urile scaunelor din baza de date
            List<int> seatids = new List<int>();
            seatids = await _context.Seats.Where(x => x.hallId == screening.hallId).OrderBy(X=>X.number).Select(x => x.Id).ToListAsync();

            var notickets = total;

            float price = 0;
            //se calculeaza pretul total al biletelor din "cos"
            foreach (var ticket in tickets)
            {
                var type = await _context.TicketType.Where(x => x.id == ticket.Key).FirstOrDefaultAsync();
                price = price + (screening.price - (screening.price * type.discount))*ticket.Value;
            }

            ChooseSeatsViewModel model = new ChooseSeatsViewModel()
            {
                screeningId = screening.Id,
                rows=rows,
                cols=cols,
                paths=paths,
                reserved=reserved,
                seatids=seatids,
                notickets=notickets,
                total=price

            };

            if (TempData["ErrorMessage"] != null)
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
                return View(model);
        }



        //functi care creaza qr code culink catre schimbarea de status al unui bilet 
        public string CreateQR(int id)
        {
            var link = "https://localhost:44348/ReservedSeats/ChangeStatus/"+id;
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            var qrcode = BitmapToBytes(qrCodeImage);
            var base64qrcode = Convert.ToBase64String(qrcode);
            var imgSrcQrCode = String.Format("data:image/png;base64,{0}", base64qrcode);
            return imgSrcQrCode;
        }


        //metoda apelata pentru rezervarea locurilor selectate

        [HttpPost]
        public async Task<IActionResult> Reserve(ReservationDetails details)
        {
            //se verifica daca ecranizarea exista
            var screening = await _context.Screenings.Where(x => x.Id == details.screeningId).Include(x=>x.hall).FirstOrDefaultAsync();
            if (screening == null)
            {
                return RedirectToAction("ChooseTickets", new { id = details.screeningId });
            }


            //pt useri si utilizatori normali se verifica daca ecranizarea este in saptamana curenta
            if (User != null)
            {
                if (User.IsInRole("Client"))
                {
                    if (VerifyScreening(details.screeningId))
                    {
                        TempData["ErrorMessage"] = "Ecranizarea aleasa nu este disponibila!";
                        return RedirectToAction("ChooseTickets", new { id = details.screeningId });
                    }
                }
            }
            else
            {
                if (VerifyScreening(details.screeningId))
                {
                    TempData["ErrorMessage"] = "Ecranizarea aleasa nu este disponibila!";
                    return RedirectToAction("ChooseTickets", new { id = details.screeningId });
                }
            }


            //la incercarea realizarii unei rezervari se refac verificarile necesare 
            //se verifica daca exista bilete in cos
            List<KVTickets> tickets = HttpContext.Session.Get<List<KVTickets>>(Convert.ToString(details.screeningId));
            if (tickets == null)
            {
                TempData["ErrorMessage"] = "Nu ati ales suficiente bilete!";
                return RedirectToAction("ChooseTickets", new { id = details.screeningId });
            }


            //se verifica daca sa trimis o lista de locuri selectate
            if (details.seats == null)
            {
                TempData["ErrorMessage"] = "Nu ati ales suficiente locuri";
                return RedirectToAction("ChooseSeats", new { id = details.screeningId });
            }

            //se verifia daca locurile alese nu au fost intre timp rezervate de altcineva 
            List<string> seats = details.seats.Split(',').ToList();
            //se ia din baza de date lista de locuri ocupate
            List<int> reserved = new List<int>();
            reserved = await _context.Seats.Join(_context.ReservedSeats, seat => seat.Id, ReservedSeats => ReservedSeats.seatId, (seat, ReservedSeats) => new { seat, ReservedSeats }).Join(_context.Reservations, x => x.ReservedSeats.reservationId, Reservations => Reservations.id, (x, Reservations) => new { x, Reservations }).Where(x => x.Reservations.screeningId == details.screeningId).Select(x => x.x.seat.Id).ToListAsync();
            foreach (var seat in seats)
            {     //daca locul a fost ocupat intre timp se returneaza inapoi pagina de alegere de locuri updatata
                if (reserved.Contains(Convert.ToInt32(seat)))
                {
                    TempData["ErrorMessage"] = "Locurile alese au fost deja ocupate!";
                    return RedirectToAction("ChooseSeats", details.screeningId);
                }
            }


            //se verifica daca nr de locuri alese coincide cu nr de locrui din sesiune
            int total = 0;
            foreach (var ticket in tickets)
            {
                total = total + ticket.Value;
            }
            if (seats.Count != total)
            {
                TempData["ErrorMessage"] = "Numarul de locuri alese nu coincide cu numarul de bilete selectate!";
                return RedirectToAction("ChooseSeats", new { id = details.screeningId });
            }


            //se verifica daca utilizatorul care a trimis request-ul e autentificat si daca id-ul trimis e al lui sau incearca sa fraudeze sistemul.
            if (details.userId != null) {
                var user = await _context.Users.Where(x => x.Id == details.userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Utilizator inexistent!";
                    return RedirectToAction("ChooseSeats", new { id = details.screeningId });
                }


                //se verifica daca utilizatorul logat are id-ul transmis    
                if (User.Identity.IsAuthenticated)
                {
                    if (_userManager.GetUserId(User) != details.userId)
                    {
                        TempData["ErrorMessage"] = "Incercare invalida!";
                        return RedirectToAction("ChooseSeats", new { id = details.screeningId });
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Incercare invalida!";
                    return RedirectToAction("ChooseSeats", new { id = details.screeningId });
                }
            }

            //se verifica daca sunt completate campurile in caaz ca utilizatorul nu e autentificat
            if (details.userId == null)
            {
                if (details.nume==null || details.prenume==null || details.email==null)
                {
                    TempData["ErrorMessage"] = "Numele, prenumele si email-ul sunt obligatorii!";
                    return RedirectToAction("ChooseSeats", new { id = details.screeningId });
                }
            }
        

            //se creaza o rezervare noua
            Reservation reservation = new Reservation()
            {
                screeningId = details.screeningId,
                firstName = details.prenume==null?null:details.prenume,
                lastName = details.nume==null?null:details.prenume,
                email = details.email==null?null:details.email,
                UserId = details.userId==null?null:details.userId,

            };

            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Administrator"))
                {
                    reservation.payed = 1;
                }
            } else
                reservation.payed = 0;


            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            var reservationId = reservation.id;

            int k = 0;
            int val = 0;
            int i = 0;
            //daca buy are alta valoare inafara de 0 sau 1
            if (details.action == null || (details.action != "Rezerva biletele" && details.action != "Cumpara bilete"))
            {
                TempData["ErrorMessage"] = "Incercare invalida!";
                return RedirectToAction("ChooseSeats", new { id = details.screeningId });
            }

            if (details.action == "Cumpara bilete")
            {
                var movie = await _context.Movies.Where(x => x.id == screening.movieId).FirstOrDefaultAsync();
                string is3d = " ";
                float totalSumToPay = 0;
                //se calculeaza totalul care trebuie platit
                foreach(var ticket in tickets)
                {
                    var t = await _context.TicketType.FindAsync(ticket.Key);
                    totalSumToPay = totalSumToPay + (screening.price - screening.price * t.discount) * ticket.Value;
                }

                if (screening.is3D == 1)
                {
                    is3d = "3D";
                }
                else
                {
                    is3d = "2D";
                }
                var client = new SendGridClient(Options.SendGridKey);
                //compunere mesaj
                var mesage = "<h1>Confirmare tranzactie</h1> <br/><br/> <h3>Multumim ca ati achizitionat bilete de la noi!</h3><br/><br/><b>Te așteptăm la film!</b><br/> <br/> Pentru a avea acces la film puteti opta pentru una din cele doua variante: <br/> a) e-ticket ( bilet electronic): prezintă la intrarea în sală biletul atașat email-ului de confirmare și unul dintre plasatorii noștri îți va scana QR code-ul pentru a valida biletul. <br/><br/> b) ridicare din cinema: te rugăm să te prezinți la casierie pentru a - ți ridica biletele pe baza e-mailul de confirmare. <br/><br/>Înainte de film, te invităm să te bucuri de produsele noastre delicioase de la bar! <h2>Detalii</h2><p><b>Data si ora ecranizarii: </b>"+screening.date.ToString("dd-MM-yyyy")+" "+screening.s_hour.ToString("HH:mm")+"</p><p><b>Film: </b>"+movie.title+" "+is3d+"</p><p><b>Sala: </b>"+screening.hall.name+"</p><p><b>Numar bilete: </b>"+seats.Count+"</p><p><b>Total: </b>"+totalSumToPay+" lei </p>";
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress("cinemawebsitelicense@gmail.com", "CinemaWebsite"),
                    Subject = "Confirmare plata",
                    PlainTextContent = mesage,
                    HtmlContent = mesage
                };

                if (User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole("Client"))
                    {
                        var user = await _context.Users.FindAsync(details.userId);
                        msg.AddTo(new EmailAddress(user.Email));
                    }
                }
                else
                {
                    msg.AddTo(new EmailAddress(details.email));
                }
               

              
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
                foreach (var seat in seats)
                {
                    //sa se genereze biletele electronice si sa se adauge la email
                    ReservedSeats rs = new ReservedSeats();
                    rs.seatId = Convert.ToInt32(seat);
                    rs.reservationId = reservationId;
                    rs.TicketTypeid = tickets[k].Key;
                    _context.ReservedSeats.Add(rs);
                    await _context.SaveChangesAsync();
                    val++;
                    i++;
                    var ticket = await _context.TicketType.FindAsync(tickets[k].Key);
                    var type = ticket.name;
                    if (val == tickets[k].Value)
                    { k++;
                     val = 0; }

                    var qrimg = CreateQR(rs.Id);
                    //se creaza biletul electronic
                    int row = 0;
                    var s = await _context.Seats.FindAsync(Convert.ToInt32(seat));

                    if ((s.number % screening.hall.columns) == 0)
                    {
                        row = s.number / screening.hall.columns;
                    }
                    else
                    {
                        row = (s.number / screening.hall.columns) + 1;
                    }


                    //trb sa se scada nr de paths din acel rand pana la acel scaun
                    var seatnumber = 0;
                    if ((s.number) % (screening.hall.columns) == 0)
                        seatnumber = screening.hall.columns;
                    else
                        seatnumber=(s.number) % (screening.hall.columns);


                  
                

                    var html = "<div style='border:5px solid blue;width:450px;padding:5px;'>";
                    html = html + "<div style='border-bottom:2px solid blue;overflow:hidden;'>";
                    html = html + "<img src='" + imgSrc + "' height='60' style='margin: 5px ;display:inline;float:left;'/>";
                    html = html + "<div style='float:right;margin:5px;'>";
                    html = html + "<p style='color:blue;display:inline;'><b>Film:</b></p>";
                    html = html + "<p style='display:inline;'>"+movie.title+" "+is3d+"</p>";
                    html = html + "<br/>";
                    html = html + "<p style='color:blue;display:inline;'><b>Data:</b></p>";
                    html = html + "<p style='display:inline;'>"+screening.date.ToString("dd-MM-yyyy")+"</p>";
                    html = html + "<br/>";
                    html = html + "<p style='color:blue;display:inline;'><b>Ora:</b></p>";
                    html = html + "<p style='display:inline;'>"+screening.s_hour.ToString("HH:mm")+"</p>";
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
                    html = html + "<p style='margin:0;'>"+screening.hall.name+"</p>";
                    html = html + "<p style='margin:0;'>"+row+"</p>";
                    html = html + "<p style='margin:0;'>"+seatnumber+"</p>";
                    html = html + "<p style='margin:0;'>"+type+"</p>";
                    html = html + " </div>";
                    html = html + "<img src='" + qrimg + "' height='120' style='margin: 0px 20px 0px 0px;display:inline;float:right;'/>";
                    html = html + "</div>";
                    html = html + "Biletele cu pret redus permit intrarea doar cu un act doveditor valid. Va rugam sa prezentati actul plasatorilor nostri.";
                    html = html + "</div>";


                    //se converteste html to biti o imagine cu latimea 440
                    var bytes = converter.FromHtmlString(html, 470);
                    //se convertesc bitii to string
                    var img = Convert.ToBase64String(bytes);
                    msg.AddAttachment("Bilet"+i+".jpg", img);
                }

                //se realizeaza plata->daca merge ->totalsumtopay


                reservation.payed = 1;
                await _context.SaveChangesAsync();

                await client.SendEmailAsync(msg);
                //se goleste sesiunea
                HttpContext.Session.Set<List<KVTickets>>(Convert.ToString(details.screeningId), null);
                ViewBag.Succes = "Rezervarea a fost realizata cu succes! Puteti verifica daca ati primit email de confirmare.";
                //se tr emailul
                return View();

            }
            else
            {
                //se creaza rezervarile de locuri pentru fiecare loc din lista
                foreach (var seat in seats)
                {
                    ReservedSeats rs = new ReservedSeats();
                    rs.seatId = Convert.ToInt32(seat);
                    rs.reservationId = reservationId;
                    rs.TicketTypeid = tickets[k].Key;
                    _context.ReservedSeats.Add(rs);
                    await _context.SaveChangesAsync();
                    val++;
                    if (val == tickets[k].Value)
                    {
                        k++;
                        val = 0;
                    }
                }
                //se goleste sesiunea
                HttpContext.Session.Set<List<KVTickets>>(Convert.ToString(details.screeningId), null);

                ViewBag.Succes = "Rezervarea a fost realizata cu succes! Puteti verifica daca ati primit email de confirmare.";


                if (User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole("Administrator"))
                    {
                        TempData["SuccesMessage"] = "Rezervarea a fost creata cu succes!";
                        return RedirectToAction("Reservations", new { id = details.screeningId });
                    }
                    if (User.IsInRole("Client"))
                    {
                        var user = await _context.Users.FindAsync(details.userId);
                        var movie = await _context.Movies.Where(x => x.id == screening.movieId).FirstOrDefaultAsync();
                        await _emailSender.SendEmailAsync(user.Email, "Confirmare rezervare",
                        "<h2>Multumim ca ati realizat rezervare la cinematograful nostru!</h2> Ati rezervat " + total + " bilete la filmul " + movie.title + " care incepe la ora" + screening.s_hour.ToString("HH:mm") + ". <br/> Va asteptam cu 30 de minute ianinte de inceperea filmului pentru a va revendica biletele!");

                    }
                }
                else
                {
                    //se trimite email de confirmare
                    var movie = await _context.Movies.Where(x => x.id == screening.movieId).FirstOrDefaultAsync();
                    await _emailSender.SendEmailAsync(details.email, "Confirmare rezervare",
                   "<h2>Multumim ca ati realizat rezervare la cinematograful nostru!</h2> Ati rezervat " + total + " bilete la filmul " + movie.title + " care incepe la ora" + screening.s_hour.ToString("HH:mm") + ". <br/> Va asteptam cu 30 de minute ianinte de inceperea filmului pentru a va revendica biletele!");

                }

                return View();
                
            }
        }



        [Authorize(Roles = "Administrator")]
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
                    ViewBag.Message = "Nu exista filme programate cu acest titlu la data selectata";

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
                ViewBag.Message = "Inca nu s-au programat filme in data selectata";
                   
                    return View();

            }
            
            
            }
            if (TempData["SuccesMessage"] != null)
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
            if (TempData["ErrorMessage"] != null)
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            return View(filme);

        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> Create(DateTime? date)
        {
          
           var movies = await _context.Movies.Where(x => x.active == 1).ToListAsync();
           var halls = await _context.Halls.Where(x=>x.active==1).ToListAsync();
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
                var movies = await _context.Movies.Where(x => x.active == 1).ToListAsync();
                var halls = await _context.Halls.Where(x => x.active == 1).ToListAsync();
                ViewBag.movies = movies;
                ViewBag.halls = halls;
                return View();
            }
            else
            {
                var movie = await _context.Movies.FindAsync(model.movieId);
                if(movie==null)
                {
                    var movies = await _context.Movies.Where(x => x.active == 1).ToListAsync();
                    var halls = await _context.Halls.Where(x => x.active == 1).ToListAsync();
                    ViewBag.movies = movies;
                    ViewBag.halls = halls;
                    ViewBag.ErrorMessage = "Filmul selectat nu exista in baza de date";
                    return View();
                }

                var hall = await _context.Halls.FindAsync(model.hallId);
                if (hall == null)
                {
                    var movies = await _context.Movies.Where(x => x.active == 1).ToListAsync();
                    var halls = await _context.Halls.Where(x => x.active == 1).ToListAsync();
                    ViewBag.movies = movies;
                    ViewBag.halls = halls;
                    ViewBag.ErrorMessage = "Sala selectata nu exista in baza de date";
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
                        var movies = await _context.Movies.Where(x => x.active == 1).ToListAsync();
                        var halls = await _context.Halls.ToListAsync();
                        ViewBag.movies = movies;
                        ViewBag.halls = halls;
                        ViewBag.ErrorMessage = "Ora selectata se suprapune cu alte ecranizari sau nu se potriveste cu orarul de functionare al cinematografului, alegeti alta ora!";
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




        [HttpGet]
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
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Reservations(int id, string q)
        {

  
            //se selecteaza toate rezervarile de la screening-ul selectat
            List<Reservation> reservations =  _context.Reservations.Where(x => x.screeningId == id).Include(x=>x.User).Include(x=>x.reservedSeats).ThenInclude(x=>x.TicketType).ToList();
               if (q != null)
            {
                q = q.ToLower();
                reservations = reservations.Where(x => x.firstName!=null? x.firstName.ToLower().Contains(q):false || x.lastName!=null ?x.lastName.ToLower().Contains(q) :false|| (x.firstName!=null && x.lastName!=null)?(x.firstName+" "+x.lastName).ToLower().Contains(q) :false|| (x.firstName != null && x.lastName != null) ? (x.lastName + " " + x.firstName).ToLower().Contains(q) :false || x.User!=null ? x.User.FirstName.ToLower().Contains(q) : false || x.User!=null? x.User.LastName.ToLower().Contains(q) : false || x.User != null ? (x.User.FirstName + " " + x.User.LastName).ToLower().Contains(q) : false ||  x.User != null ? (x.User.LastName + " " + x.User.FirstName).ToLower().Contains(q) : false).ToList();
            }
              
            var screening = await _context.Screenings.Include(x=>x.movie).Include(x=>x.hall).Where(x=>x.Id==id).FirstOrDefaultAsync(); 
            ScreeningReservations model = new ScreeningReservations();
            if (screening == null)
            {
                ViewBag.ErrorMessage = "Ecranizarea cautata nu exista!";
               
               
                return View();
            }

            if (reservations.Count == 0)
            {
                ViewBag.Message = "Nu exista rezervari la momentul de fata!";
                model.screeningid = id;
                model.title = screening.movie.title;
                model.s_hour = screening.s_hour;
                model.f_hour = screening.f_hour;

                if (TempData["SuccesMessage"] != null)
                    ViewBag.SuccesMessage = TempData["SuccesMessage"];


                return View(model);
            }

          
            model.screeningid = id;
            model.title = screening.movie.title;
            model.s_hour = screening.s_hour;
            model.f_hour = screening.f_hour;

            //lista de rezervari
            List<ScreeningReservationInfo> modelreservations = new List<ScreeningReservationInfo>();

            //pentru fiecare rezervare de la ecranizarea respectiva
            foreach(var reservation in reservations)
            {   //se creaza o rezervare noua pt model
                ScreeningReservationInfo modelreservation = new ScreeningReservationInfo();
              
                //se seteaza id-ul si daca e platit
                modelreservation.id = reservation.id;
                modelreservation.payed = reservation.payed;
               //dac are userid atunci se completeaza cu numele prenumle emailul userului altfel cele din formular
                if (reservation.UserId == null)
                {
                    modelreservation.FirstName = reservation.firstName;
                    modelreservation.LastName = reservation.lastName;
                    modelreservation.email = reservation.email;
                }
                else
                {
                    modelreservation.FirstName = reservation.User.FirstName;
                    modelreservation.LastName = reservation.User.LastName;
                    modelreservation.email = reservation.User.Email;
                }
               
                //se calculeaza pretul rezervarii si locurile
                float total = 0;
               
             
                 List<TypeNameNumber> tickets = new List<TypeNameNumber>();
                                tickets= reservation.reservedSeats.GroupBy(x=>x.TicketType.name).Select(g => new TypeNameNumber
                                {
                                    typeOfTicket = g.Key,
                                    noOfTickets = g.Count()
                                }).ToList();
                modelreservation.tickets = tickets;


                Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
              
      
                //pt fiecare bilet/loc din rezervare -> se adauga pretul biletului la total
                foreach (var ticket in reservation.reservedSeats)
                {
                    total = total + (reservation.screening.price - (reservation.screening.price) * ticket.TicketType.discount);
                    var seat = await _context.Seats.Where(x => x.Id == ticket.seatId).FirstOrDefaultAsync();
                    int row=0;
                    if ((seat.number % screening.hall.columns) == 0)
                    {
                         row = seat.number / screening.hall.columns;
                    }
                    else
                    {
                        row = (seat.number / screening.hall.columns) + 1;
                    }
                    
                    
                    if (!dict.ContainsKey(row))
                    {
                        dict.Add(row, new List<int>());
                        if((seat.number) % (screening.hall.columns)==0)
                            dict[row].Add(screening.hall.columns);
                        else
                            dict[row].Add((seat.number) % (screening.hall.columns));
                    }
                    else
                      if ((seat.number) % (screening.hall.columns) == 0)
                          dict[row].Add(screening.hall.columns);
                      else
                          dict[row].Add((seat.number) % (screening.hall.columns));
                }
              
                modelreservation.total = total;
               
                modelreservation.seats = dict;

                modelreservations.Add(modelreservation);
            }
            model.reservations = modelreservations;
            if (TempData["SuccesMessage"] != null)
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
            return View(model);

        }



        [Route("Screening/DeleteConfirmation/{id}")]
        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> DeleteConfirmation(int id)
        {
            var screening = await _context.Screenings.FindAsync(id);
            if (screening == null)
            {
                TempData["ErrorMessage"] = "Ecranizarea care se vrea sa fie stearsa nu a fost gasita!";
                return RedirectToAction("List", new { date = DateTime.Now.ToString("yyyy-MM-dd") });
            }
            return View(id);
        }

        [Route("Screening/Delete/{id}")]
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var screening = await _context.Screenings.FindAsync(id);
            if (screening == null)
            {
                TempData["ErrorMessage"] = "Ecranizarea care se vrea sa fie stearsa nu a fost gasita!";
                return RedirectToAction("List", new { date = DateTime.Now.ToString("yyyy-MM-dd") });
            }
            _context.Screenings.Remove(screening);
            await _context.SaveChangesAsync();

            TempData["SuccesMessage"] = "Ecranizarea a fost stearsa cu succes!";
            return RedirectToAction("List", new { date = screening.date.ToString("yyyy-MM-dd") });
        }



        private static Byte[] BitmapToBytes(Bitmap img)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

   
    }
}
