using CoreHtmlToImage;
using Licenta.Data;
using Licenta.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    public class ReservedSeatsController : Controller
    {

        private readonly ApplicationDbContext _context;

        public ReservedSeatsController(ApplicationDbContext context)
        {
            _context = context;

        }

        //functia care schimba statusul cliamed al biletelor si returneaza File
        [HttpPost]
        public async Task<FileContentResult> Download(string ids)
        {

            List<string> listofIDs = ids.Split(',').ToList();
            List<int> idlist = listofIDs.Select(int.Parse).ToList();


            //lista de tickettypes care sa aiba incluse ->seats, tickettypes, reservation,screening,movie

            var tickets = await _context.ReservedSeats.Where(x => idlist.Contains(x.Id)).Include(x => x.TicketType).Include(x => x.seat).Include(x => x.reservation).ThenInclude(x => x.screening).Include(x => x.reservation.screening.movie).Include(x => x.reservation.screening.hall).ToListAsync();
            //id-ruile reserved seats-urilor->titlu,daca e 3d,data,ora,sala,randul,numarul,tipul
            var converter = new HtmlConverter();
            string is3d;

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
            string html = "";


            foreach (var ticket in tickets)
            {
                ticket.claimed = 1;
                _context.ReservedSeats.Update(ticket);
                if (ticket.reservation.screening.is3D == 1)
                    is3d = "3D";
                else
                    is3d = " ";

                html = html + "<div style='border:5px solid blue;padding:5px;'>";
                html = html + "<div style='border-bottom:2px solid blue;overflow:hidden;'>";
                html = html + "<img src='" + imgSrc + "' height='60' style='margin: 5px ;display:inline;float:left;'/>";
                html = html + "<div style='float:right;margin:5px;'>";
                html = html + "<p style='color:blue;display:inline;'><b>Film:</b></p>";
                html = html + "<p style='display:inline;'>" + ticket.reservation.screening.movie.title + " " + is3d + "</p>";
                html = html + "<br/>";
                html = html + "<p style='color:blue;display:inline;'><b>Data:</b></p>";
                html = html + "<p style='display:inline;'>" + ticket.reservation.screening.date.ToString("dd-MM-yyyy") + "</p>";
                html = html + "<br/>";
                html = html + "<p style='color:blue;display:inline;'><b>Ora:</b></p>";
                html = html + "<p style='display:inline;'>" + ticket.reservation.screening.s_hour.ToString("HH:mm") + "</p>";
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
                html = html + "<p style='margin:0;'>" + ticket.reservation.screening.hall.name + "</p>";
                html = html + "<p style='margin:0;'>" + ticket.seat.row + "</p>";
                html = html + "<p style='margin:0;'>" + ticket.seat.seatNo + "</p>";
                html = html + "<p style='margin:0;'>" + ticket.TicketType.name + "</p>";
                html = html + " </div>";
                html = html + "</div>";
                html = html + "Biletele cu pret redus permit intrarea doar cu un act doveditor valid. Va rugam sa prezentati actul plasatorilor nostri.";
                html = html + "</div>";


            }

            var bytes = converter.FromHtmlString(html);

            await _context.SaveChangesAsync();


            return File(bytes, "image/*", "ticket.jpg");
        }

        [Route("ReservedSeats/ChangeStatus/{id}")]
        public async Task<IActionResult> ChangeStatus(int id)
        {
            ChangeStatusViewModel model = new ChangeStatusViewModel();

            var details = await _context.ReservedSeats.Where(x => x.Id == id).Include(x => x.seat).Include(x => x.TicketType).Include(x => x.seat.hall).Include(x => x.reservation.screening).Include(x => x.reservation.screening.movie).FirstOrDefaultAsync();

            if (details == null)
            {
                ViewBag.ErrorMessage = "Acest bilet a fost deja revendicat!!";
                return View();
            }


            model.title = details.reservation.screening.movie.title;
            model.date = details.reservation.screening.date;
            model.hour = details.reservation.screening.s_hour;
            model.hallname = details.seat.hall.name;
            model.row = details.seat.row;
            model.seatNo = (int)details.seat.seatNo;
            model.type = details.TicketType.name;


            if (details.claimed == 1)
                ViewBag.ErrorMessage = "Acest bilet a fost deja revendicat!!";
            else
            {
                var ticket = await _context.ReservedSeats.FindAsync(id);
                ticket.claimed = 1;
                _context.ReservedSeats.Update(ticket);
                await _context.SaveChangesAsync();
                ViewBag.SuccesMessage = "Biletul a fost revendicat cu succes!!";
            }
            
           
            return View(model);
        }

    }
}
