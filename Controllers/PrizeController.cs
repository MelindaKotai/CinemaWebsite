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
using System.Text;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    public class PrizeController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        public PrizeController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;

        }



        private static char[] _base36chars ="0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        private static Random _random = new Random();
        public static string GetRandomCode(int length)
        {
            var sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
                sb.Append(_base36chars[_random.Next(36)]);

            return sb.ToString();
        }


        //calculeaza cate premii disponibile are utilizatorul logat
        public  int numberOfPrizes()
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            //numarul de bilete cumparate de utilizator
            var payedtickets = _context.ReservedSeats.Join(_context.Reservations, s => s.reservationId, r => r.id, (s, r) => new { s, r }).Where(x => x.r.UserId == userId).Where(x => x.r.payed == 1).Count();
            var userprizes =  _context.UserPrizes.Where(x => x.userId == userId).Count();
            var numberofprizes = payedtickets / 5;
            var numberofavailableprizes = numberofprizes - userprizes;
          
            return numberofavailableprizes;
        }

        public int numberOfTicketsLeft()
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            //numarul de bilete cumparate de utilizator
            var payedtickets = _context.ReservedSeats.Join(_context.Reservations, s => s.reservationId, r => r.id, (s, r) => new { s, r }).Where(x => x.r.UserId == userId).Where(x => x.r.payed == 1).Count();
            var userprizes = _context.UserPrizes.Where(x => x.userId == userId).Count();
           
            int numberofticketsleft= payedtickets%5;
            numberofticketsleft = 5 - numberofticketsleft;
            return numberofticketsleft;
        }


        [Authorize(Roles = "Client")]
        public async Task<IActionResult> IndexAsync()
        {
            var userId = _userManager.GetUserId(HttpContext.User);

            var noofprizes = numberOfPrizes();
            var noofticketsleft = numberOfTicketsLeft();
            var prizes = await _context.Prizes.Where(x=>x.active==1).ToListAsync();
            var userprizes =await  _context.UserPrizes.Where(x => x.userId == userId).Where(x=>x.claimed==0).ToListAsync();

            //din user prizes se face o lista de obiecte tip ->nume premiu, cod premiue
            List<PrizesViewModel> unclaimedprizes = new List<PrizesViewModel>();
            foreach(var prize in userprizes)
            {
                PrizesViewModel uc = new PrizesViewModel();
                var name = await _context.Prizes.Where(x => x.Id == prize.prizeId).Select(x => x.name).FirstOrDefaultAsync();
                uc.name = name;
                uc.code = prize.code;
                unclaimedprizes.Add(uc);

            }

            PrizePageViewModel model = new PrizePageViewModel();
            model.prizes = prizes;
            model.noofticketsleft = noofticketsleft;
            model.noofprizes = noofprizes;
            model.unclaimedprizes = unclaimedprizes;



            return View(model);
        
        }




        //metoda apelata de ajax la rotirea tombolei
        [HttpPost]
        [Authorize(Roles = "Client")]
        public async Task CreatePrize(string prize, string user)
        {
           
        var noofprizes = numberOfPrizes();

            if (noofprizes > 0) { 
                var prizeid = await _context.Prizes.Where(x => x.name == prize).FirstOrDefaultAsync();
                if (prizeid != null)
                {
                    string obj = GetRandomCode(7);
                    UserPrizes up = new UserPrizes();
                    up.prizeId = prizeid.Id;
                    up.userId = user;
                    up.claimed = 0;
                    up.code= obj;
                    _context.UserPrizes.Add(up);
                    await _context.SaveChangesAsync();
                
                }
            
            }
           
           
        }



        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> List()
        {

           
            var model = await _context.Prizes.ToListAsync();

            if (model == null)
            {
                ViewBag.ErrorMessage = "Nu s-au gasit premii!";
                return View();
            }

            if (TempData["SuccesMessage"] != null)
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
           
            
            return View(model);
        }


        [HttpPost]

        [Route("/Prize/ChangeStatus/{id}")]
        public async Task<string> ChangeStatus(int id)
        {
            var result = "";
            var prize = await _context.Prizes.FindAsync(id);
            if (prize == null)
            {
                result = "Premiul nu a fost gasit!";
                return result;
            }
            if (prize.active == 0)
                prize.active = 1;
            else
                prize.active = 0;
            _context.Prizes.Update(prize);
            await _context.SaveChangesAsync();
            result = "Statusul premiului a fost schimbat cu succes!";
            return result;
        }


        [Route("/Prize/Update/{id}")]
        public async Task<IActionResult> Update(int id)
        {
            var prize = await _context.Prizes.FindAsync(id);

            if (prize == null)
            {
                ViewBag.ErrorMessage = "Premiul nu a fost gasit!";
                return View();
            }

            return View(prize);
        }


        [HttpPost]
        [Route("/Prize/Update/{id}")]
        public async Task<IActionResult> Update(int id, string name)
        {

            var prize = await _context.Prizes.FindAsync(id);

            if (prize == null)
            {
                ViewBag.ErrorMessage = "Premiul nu a fost gasita!";
                return View();
            }
            if (name == null)
            {
                ViewBag.ErrorMessage = "Numele este obligatoriu!";
                return View();

            }
            prize.name = name;
            _context.Prizes.Update(prize);
            await _context.SaveChangesAsync();


            TempData["SuccesMessage"] = "Numele premiului a fost actualizat cu succes!";
            return RedirectToAction("List");

        }


        [Route("/Prize/Create")]
        public IActionResult Create()
        {

            

            return View();
        }


        [Route("/Prize/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (name==null)
            {
                ViewBag.ErrorMessage = "Numele este obligatoriu!";
                return View();
            }
            if (name.Length > 25)
            {
                ViewBag.ErrorMessage = "Numele nu poate fi mai lung de 25 de caractere!";
                return View();
            }
            Prize prize = new Prize();
           prize.name = name;
            prize.active = 1;
           
            _context.Prizes.Add(prize);
            await _context.SaveChangesAsync();
           

            TempData["SuccesMessage"] = "Premiul a fost adaugat cu succes!";
            return RedirectToAction("List");
        }

    }
}
