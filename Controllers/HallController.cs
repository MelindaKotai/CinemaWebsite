using Licenta.Data;

using Licenta.Models;
using Licenta.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    public class HallController : Controller
    {

        private readonly ApplicationDbContext _context;
       
        public HallController( ApplicationDbContext context)
        { 
            _context = context;

        }


        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> List()
        {
            List<HallListViewModel> model = new List<HallListViewModel>();
            var halls = await _context.Halls.ToListAsync();


            foreach(var hall in halls)
            {
                HallListViewModel h = new HallListViewModel();
                h.id = hall.id;
                h.name = hall.name;
                h.active = hall.active;

                var noseats = await _context.Seats.Where(x => x.path == 0).Where(x => x.hallId == hall.id).CountAsync();
                h.noseats = noseats;
                model.Add(h);

            }

            if (model == null)
            {
                ViewBag.ErrorMessage = "Nu s-au gasit sali!";
                return View();
            }

            if (TempData["SuccesMessage"] != null)
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
                return View(model);
        }

        [HttpPost]

        [Route("/Hall/ChangeStatus/{id}")]
        public async Task<string> ChangeStatus(int id)
        {
            var result = "";
            var hall = await _context.Halls.FindAsync(id);
            if (hall == null)
            {
                result = "Sala nu a fost gasita!";
                return result;
            }
            if (hall.active == 0)
                hall.active = 1;
            else
                hall.active = 0;
            _context.Halls.Update(hall);
            await _context.SaveChangesAsync();
            result = "Statusul salii a fost schimbat cu succes!";
            return result;
        }




        [Route("/Hall/Structure/{id}")]
        public async Task<IActionResult> Structure(int id)
        {

            var hall = await _context.Halls.FindAsync(id);
            if (hall == null)
            {
                ViewBag.ErrorMessage = "Sala nu a fost gasita!";
                return View();
            }

            HallStructureViewModel model = new HallStructureViewModel();
            model.hallid = hall.id;
            model.cols = hall.columns;
            model.rows = hall.rows;
            model.name = hall.name;

            List<int> paths = await _context.Seats.Where(x => x.path == 1).Where(x => x.hallId == id).Select(x => x.number).ToListAsync();

            model.paths = paths;
            return View(model);
        }


        [Route("/Hall/Update/{id}")]
        public async Task<IActionResult> Update(int id)
        {
            var hall = await _context.Halls.FindAsync(id);
          
            if (hall == null)
            {
                ViewBag.ErrorMessage = "Sala nu a fost gasita!";
                return View();
            }

            return View(hall);
        }


        [HttpPost]
        [Route("/Hall/Update/{id}")]
        public async Task<IActionResult> Update(int id, string name)
        {

            var hall = await _context.Halls.FindAsync(id);

            if (hall == null)
            {
                ViewBag.ErrorMessage = "Sala nu a fost gasita!";
                return View();
            }
            if (name == null)
            {
                ViewBag.ErrorMessage = "Numele este obligatoriu!";
                return View();

            }
            hall.name = name;
            _context.Halls.Update(hall);
            await _context.SaveChangesAsync();


            TempData["SuccesMessage"] = "Numele salii a fost actualizat cu succes!";
            return RedirectToAction("List");

        }


        [Route("/Hall/Create")]
        public async Task<IActionResult> Create()
        {
          
            CreateHallViewModel model = new CreateHallViewModel();

            return View(model);
        }


        [Route("/Hall/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateHallViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            Hall  hall = new Hall();
            hall.name= model.name;
            hall.rows=model.rows;
            hall.columns=model.cols;
            hall.active = 1;
            _context.Halls.Add(hall);
            await _context.SaveChangesAsync();
            List<string> paths = model.paths.Split(',').ToList();

            for(int i = 1; i <= model.rows * model.cols; i++)
            {
                Seat seat = new Seat();
                if (paths.Contains(i.ToString()))
                {
                    seat.path = 1;

                }
                else
                {
                    seat.path = 0;
                }

                seat.hallId = hall.id;
                seat.number = i;
                _context.Seats.Add(seat);

            }

            await _context.SaveChangesAsync();

            TempData["SuccesMessage"] = "Sala a fost creata cu succes!";
            return RedirectToAction("List");
        }




    }
}
