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
    public class TicketTypesController : Controller
    {
        private readonly ApplicationDbContext _context;
     
        public TicketTypesController( ApplicationDbContext context)
        {
          
            _context = context;

        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> List()
        {

            
            var model = await _context.TicketType.ToListAsync();


            if (model == null)
            {
                ViewBag.ErrorMessage = "Nu s-au gasit tipuri de bilete!";
                return View();
            }

            if (TempData["SuccesMessage"] != null)
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
           
            return View(model);
        }



        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [Route("/TicketTypes/ChangeStatus/{id}")]
        public async Task<string> ChangeStatus(int id)
        {
            var result = "";
            var ticket = await _context.TicketType.FindAsync(id);
            if (ticket == null)
            {
                result = "Sala nu a fost gasita!";
                return result;
            }
            if (ticket.active == false)
                ticket.active = true;
            else
                ticket.active = false;
            _context.TicketType.Update(ticket);
            await _context.SaveChangesAsync();
            result = "Statusul biletului a fost schimbat cu succes!";
            return result;
        }

        [Authorize(Roles = "Administrator")]
        [Route("/TicketTypes/Update/{id}")]
        public async Task<IActionResult> Update(int id)
        {
            var ticket = await _context.TicketType.FindAsync(id);

            if (ticket == null)
            {
                ViewBag.ErrorMessage = "Tipul de bilet selectat nu a fost gasit!";
                return View();
            }

            return View(ticket);
        }


        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [Route("/TicketTypes/Update/{id}")]
        public async Task<IActionResult> Update(int id, string name)
        {

            var ticket = await _context.TicketType.FindAsync(id);

            if (ticket == null)
            {
                ViewBag.ErrorMessage = "Tipul de bilet selectat nu a fost gasit!";
                return View();
            }
            if (ticket == null)
            {
                ViewBag.ErrorMessage = "Numele este obligatoriu!";
                return View();

            }
            ticket.name = name;
            _context.TicketType.Update(ticket);
            await _context.SaveChangesAsync();


            TempData["SuccesMessage"] = "Numele biletului a fost actualizat cu succes!";
            return RedirectToAction("List");

        }

        [Authorize(Roles = "Administrator")]
        [Route("/TicketTypes/Create")]
        public async Task<IActionResult> Create()
        {

            TicketTypesViewModel model = new TicketTypesViewModel();

            return View(model);
        }


        [Route("/TicketTypes/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(TicketTypesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            TicketType ticket = new TicketType();
            ticket.name = model.name;
            ticket.discount = (float)(model.discount / 100);
            _context.TicketType.Add(ticket);
            await _context.SaveChangesAsync();
          

            TempData["SuccesMessage"] = "Biletul a fost creat cu succes!";
            return RedirectToAction("List");
        }



    }
}
