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
        //actiune care returneaza pagina cu lista de sali
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
                var noseats = await _context.Seats.Where(x => x.seatNo != null).Where(x => x.hallId == hall.id).CountAsync();
                h.noseats = noseats;
                model.Add(h);
            }

            if (model == null)
            {
                ViewBag.ErrorMessage = "Nu s-au găsit săli!";
                return View();
            }

            if (TempData["SuccesMessage"] != null)
                ViewBag.SuccesMessage = TempData["SuccesMessage"];

            if (TempData["ErrorMessage"] != null)
                ViewBag.ErrorMessage = TempData["ErrorMessage"];

            return View(model);
        }




        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [Route("/Hall/ChangeStatus/{id}")]
        //se schimba statusul unei sali
        public async Task<string> ChangeStatus(int id)
        {
            var result = "";
            var hall = await _context.Halls.FindAsync(id);
            if (hall == null)
            {
                result = "Sala nu a fost găsită!";
                return result;
            }
            if (hall.active == false)
                hall.active = true;
            else
                hall.active = false;
            _context.Halls.Update(hall);
            await _context.SaveChangesAsync();
            result = "Statusul sălii a fost schimbat cu succes!";
            return result;
        }



        [Authorize(Roles = "Administrator")]
        [Route("/Hall/Structure/{id}")]
        //returneaza pagina cu structura salii
        public async Task<IActionResult> Structure(int id)
        {

            var hall = await _context.Halls.FindAsync(id);
            if (hall == null)
            {
                ViewBag.ErrorMessage = "Sala nu a fost găsită!";
                return View();
            }

            HallStructureViewModel model = new HallStructureViewModel();
            model.hallid = hall.id;
            model.cols = hall.columns;
            model.name = hall.name;

            //locurile sunt returnate in ordinea lor din matrice
            List<Seat> seats = await _context.Seats.Where(x => x.hallId == id).OrderBy(x=>x.order).ToListAsync();

            model.seats = seats;
            return View(model);
        }


        [Authorize(Roles = "Administrator")]
        [Route("/Hall/Update/{id}")]
        //crearea paginii de update
        public async Task<IActionResult> Update(int id)
        {
            var hall = await _context.Halls.FindAsync(id);
          
            if (hall == null)
            {
                ViewBag.ErrorMessage = "Sala nu a fost găsită!";
                return View();
            }

            return View(hall);
        }



        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [Route("/Hall/Update/{id}")]
        //se actualizeaza sala
        public async Task<IActionResult> Update(int id, string name)
        {

            var hall = await _context.Halls.FindAsync(id);

            if (hall == null)
            {
                ViewBag.ErrorMessage = "Sala nu a fost găsită!";
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


            TempData["SuccesMessage"] = "Numele sălii a fost actualizat cu succes!";
            return RedirectToAction("List");

        }


        [HttpGet]
        [Authorize(Roles = "Administrator")]
        [Route("/Hall/Create")]
        //returneaza pagina de creare sala
        public IActionResult Create()
        {
            CreateHallViewModel model = new CreateHallViewModel();
            return View(model);
        }



        [Authorize(Roles = "Administrator")]
        [Route("/Hall/Create")]
        [HttpPost]
        //se creaza sala
        public async Task<IActionResult> Create(CreateHallViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            //tranzactie->ori se creaza sala cu toate locurile ori nimic
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                Hall hall = new Hall();
                hall.name = model.name;
                hall.rows = model.rows;
                hall.columns = model.cols;
                hall.active = true;

                _context.Halls.Add(hall);
                await _context.SaveChangesAsync();

                //lista de culoare-: se transforma string-ul trimis intr-o lista de ints
                List<int> paths = model.paths.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

                //primul rand de scaune
                int row = 1;
                for (int i = 1; i <= model.rows; i++)
                {
                    //numarul de ordine al primului element al matricii de pe randul curent
                    int firstRowElement = i * model.cols - model.cols + 1;

                    //lista numerelor de ordine a elementelor din matrice de pe randul curent
                    List<int> rowElementsNumbers = Enumerable.Range(firstRowElement, model.cols).ToList();

                    //se verifica daca in lista de elemenete exista doar culoare
                    if (rowElementsNumbers.Except(paths).ToList().Count == 0)
                    {
                        //se creaza un rand intreg de culoare cu row=0 si seat.no=null
                        for (int j = 1; j <= model.cols; j++)
                        {
                            Seat seat = new Seat();
                            //numarul elementului din matrice
                            var order = j + (i - 1) * model.cols;
                            seat.hallId = hall.id;

                            //randurile complet goale nu sunt considerate randuri de scaune
                            seat.row = 0;
                            seat.seatNo = null;
                            seat.order = order;
                            _context.Seats.Add(seat);
                        }
                    }
                    else
                    {   //numarul de scaune pe randul respectiv
                        int noseats = model.cols - rowElementsNumbers.AsQueryable().Intersect(paths).Count();
                        for (int j = 1; j <= model.cols; j++)
                        {
                            Seat seat = new Seat();

                            //numarul elementului din matrice
                            var order = j + (i - 1) * model.cols;
                            //se verifica daca elementul curent e culoar
                            if (paths.Contains(firstRowElement + j - 1))
                            {
                                seat.hallId = hall.id;
                                seat.row = row;
                                seat.seatNo = null;
                                seat.order = order;
                                _context.Seats.Add(seat);


                            }
                            else
                            {
                                seat.hallId = hall.id;
                                seat.row = row;
                                seat.seatNo = noseats;
                                seat.order = order;
                                _context.Seats.Add(seat);
                                noseats--;
                            }
                        }
                        //se incrementeaza randurile de scaune doar daca randul contine cel putin un scaun
                        row++;
                    }
                }

                //se incheie tranzactia
                
                await _context.SaveChangesAsync();
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                TempData["ErrorMessage"] = "O eroare a intervenit în crearea sălii și procesul a eșuat! Mai încercați odată!";
                return RedirectToAction("List");
            }

            TempData["SuccesMessage"] = "Sala a fost creată cu succes!";
            return RedirectToAction("List");
        }




    }
}
