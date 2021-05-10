using Licenta.Data;

using Licenta.Models;
using Licenta.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    public class ReservationController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        public ReservationController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
           
        }


        [Authorize(Roles = "Client")]
        //pagina de rezervari ale clientului
        public async Task<IActionResult> Index()
        {
            var allreservations = await _context.Reservations.Join(_context.Screenings,r=>r.screeningId,s=>s.Id,(r,s)=>new {r,s}).
                Join(_context.Movies,res=>res.s.movieId,m=>m.id,(res,m)=>new { res, m })
                .Where(x=>x.res.r.UserId== _userManager.GetUserId(HttpContext.User)).ToListAsync();

            List<ReservationPageViewModel> past = new List<ReservationPageViewModel>();
            List<ReservationPageViewModel> future = new List<ReservationPageViewModel>();
            ReservationPageList model = new ReservationPageList();


            if (allreservations!=null)
            {
                foreach (var reservation in allreservations)
                {
                    ReservationPageViewModel rp = new ReservationPageViewModel();
                    rp.id = reservation.res.r.id;
                    rp.movieImg = reservation.m.img;
                    rp.movieTitle = reservation.m.title;
                    rp.payed = reservation.res.r.payed;
                    rp.screeningDate = reservation.res.s.date;
                    rp.screeningTime = reservation.res.s.s_hour;
                    rp.is3D = reservation.res.s.is3D;
                    float total = 0;
                    var ticketsq = await _context.ReservedSeats.Join(_context.TicketType, r => r.TicketTypeid, t => t.id, (r, t) => new { r, t }).Where(x => x.r.reservationId == reservation.res.r.id).OrderBy(x => x.r.TicketTypeid).ToListAsync();

                    List<TypeNameNumber> tickets = new List<TypeNameNumber>();
                    Dictionary<int, int> t = new Dictionary<int, int>();

                    foreach (var ticket in ticketsq)
                    {
                        if (t.ContainsKey(ticket.r.TicketTypeid))
                            t[ticket.r.TicketTypeid]++;
                        else
                            t[ticket.r.TicketTypeid] = 1;

                    }

                    foreach (var type in t)
                    {
                        TypeNameNumber tn = new TypeNameNumber();
                        var typename = await _context.TicketType.Where(x => x.id == type.Key).FirstOrDefaultAsync();
                        if (typename != null)
                        {
                            tn.typeOfTicket = typename.name;
                            tn.noOfTickets = type.Value;
                            tickets.Add(tn);
                            total = total + (reservation.res.s.price - (reservation.res.s.price * typename.discount)) * type.Value;
                        }
                    }

                    rp.total = total;
                    rp.tickets = tickets;
                    var nohours = reservation.res.s.s_hour.Hour;
                    var nominutes = reservation.res.s.s_hour.Minute;
                    var notoadd = nohours * 60 + nominutes;
                    if (DateTime.Now > reservation.res.s.date.AddMinutes(notoadd))
                    {
                        past.Add(rp);
                    }
                    else
                        future.Add(rp);
                    

                }
            }

            model.past = past;
            model.future = future;
            return View(model);
        }


        [Authorize]
        [HttpPost]
        [Authorize(Roles = "Client")]
        [Route("/Reservation/Cancel/{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.Where(x => x.id == id).FirstOrDefaultAsync();
            if (reservation == null)
            {
                TempData["errormessage"] = "Rezervarea nu a fost găsita";
                return RedirectToAction("Index");

            }
            else
            {
                var reservedseats = await _context.ReservedSeats.Where(x => x.reservationId == reservation.id).ToListAsync();

                foreach (var reservedseat in reservedseats)
                {
                    _context.ReservedSeats.Remove(reservedseat);
                }
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();

                TempData["succesmessage"] = "Rezervarea a fost anulată cu succes!";
                return RedirectToAction("Index");

            }
        }


        [HttpPost]
        [Authorize(Roles = "Administrator,Manager,Angajat")]
        [Route("/Reservation/ChangeStatus/{id}")]
        public async Task<string> ChangeStatus(int id)
        {
            var result = "";
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation==null)
            {
                 result = "Rezervarea nu a fost gasita!";
                 return result;
            }
            if (reservation.payed == false)
                reservation.payed = true;
            else
                reservation.payed = false;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            result = "Statusul a fost schimbat cu succes!";
            return result;
        }




        [Authorize(Roles = "Administrator,Manager,Angajat")]
        [Route("/Reservation/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                TempData["ErrorMessage"]= "Rezervarea nu a fost găsită!";
                return RedirectToAction("List","Screening" ,new { date = DateTime.Now });
            }
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            TempData["SuccesMessage"] = "Rezervarea a fost ștearsă cu succes!";
            return RedirectToAction("Reservations","Screening",new { id = reservation.screeningId });

        }
    
    
    
    }
}
