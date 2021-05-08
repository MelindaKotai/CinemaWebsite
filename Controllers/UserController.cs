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
    public class UserController : Controller
    {

        private readonly ApplicationDbContext _context;

        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<User> userManager, ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
            _context = context;
            _userManager = userManager;
        }
        [Authorize(Roles = "Administrator,Manager,Angajat")]
        public async Task<IActionResult> List(string q,int p)
        {

            if (p == null || p == 0)
            {
                p = 1;
            }
            List<UserViewModel> model = new List<UserViewModel>();

            var users = await _context.Users.ToListAsync();

            if (q != null)
            {
                q = q.ToLower();
                users = users.Where(x => x.FirstName.ToLower().Contains(q) || x.LastName.ToLower().Contains(q) || (x.LastName.ToLower() + " " + x.FirstName.ToLower()).Contains(q) || (x.FirstName.ToLower() + " " + x.LastName.ToLower()).Contains(q)).ToList();
            }
            foreach (var user in users)
            {
                UserViewModel u = new UserViewModel()
                {
                    id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email
                };
                model.Add(u);

            }
            if (model.Count % 5 == 0)
                ViewBag.noPages = (model.Count / 5);
            else
                ViewBag.noPages = (model.Count / 5) + 1;
            model = model.Skip((p - 1) * 5).Take(5).ToList();
          


            ViewBag.currPage = p;
            if (model.Count == 0)
            {
                ViewBag.ErrorMessage = "Nu a fost gasit nici un utilizator!";
                return View();
            }
            if (TempData["SuccesMessage"] != null)
            {
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
            }

            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }
            return View(model);
        }


        [Route("/User/Detail/{id}")]
        [Authorize(Roles = "Administrator,Manager,Angajat")]
        public async Task<IActionResult> Detail(string id)
        {
            UserViewModel model = new UserViewModel();

            var user = await _context.Users.Include(x => x.UserPrizes).ThenInclude(p => p.prize).FirstOrDefaultAsync(x => x.Id == id);




            model.id = user.Id;
            model.FirstName = user.FirstName;
            model.LastName = user.LastName;
            model.PhoneNumber = user.PhoneNumber;
            model.Email = user.Email;
                
                List<PrizesViewModel> plist = new List<PrizesViewModel>();

                if (user.UserPrizes != null)
                {
                    foreach (var prize in user.UserPrizes)
                    {
                      
                            PrizesViewModel p = new PrizesViewModel();
                            p.id = prize.Id;
                            p.code = prize.code;
                            p.name = prize.prize.name;
                            p.claimed = prize.claimed;
                            plist.Add(p);
                        

                    }

                }
                model.userprizes = plist;
               

            
            if (user == null)
            {
                ViewBag.ErrorMessage = "Nu a fost gasit utilizatorul!";
                return View();
            }
            if (TempData["SuccesMessage"] != null)
            {
                ViewBag.SuccesMessage = TempData["SuccesMessage"];
            }

            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }
            return View(model);
        }
           

    


       [Route("/User/ClaimPrize/{id}")]
        [Authorize(Roles = "Administrator,Manager,Angajat")]
        public async Task<IActionResult> ClaimPrize(int id)
        {



            var prize = await _context.UserPrizes.FindAsync(id);
            if (prize == null)
            {
                TempData["ErrorMessage"] = "premiul nu a fost gasit !";
                return RedirectToAction("Detail");

            }
            prize.claimed = true;
            _context.UserPrizes.Update(prize);
            await _context.SaveChangesAsync();
            TempData["SuccesMessage"] = "premiul a fost revendicat cu succes !";
            return RedirectToAction("Detail", new { id=prize.userId});

        }



        [Route("/User/ConfirmDelete/{id}")]
        [Authorize(Roles = "Administrator")]
        public IActionResult ConfirmDelete(string id)
        {
            var user = _context.Users.Find(id);
           
            if (user == null)
            {
                TempData["ErrorMessage"] = "premiul nu a fost gasit !";
                return RedirectToAction("List");

            }
            UserViewModel model = new UserViewModel(){
                id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

             return View(model);
        }



        [HttpPost]
        [Route("/User/Delete/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string id)
        {

            var user = _context.Users.Find(id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilizatorul nu a fost gasit !";
                return RedirectToAction("List");

            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccesMessage"] = "Utilizatorul a fost sters cu succes! !";
            return RedirectToAction("List");



        }

        [HttpGet]
        [Route("/User/EditRole/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> EditRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilizatorul nu a fost gasit !";
                return RedirectToAction("List");
            }
            var userroles = await _userManager.GetRolesAsync(user);
            var role = userroles.First();
          

            var dbroles =_roleManager.Roles.ToList();
            List<string> roles = new List<string>();
            foreach(var r in dbroles)
            {
                roles.Add(r.Name);
            }
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }
            EditRoleViewModel model = new EditRoleViewModel();
            model.userId = id;
            model.userRole = role;
            model.roles = roles;
            model.email = user.Email;
            model.FirstName = user.FirstName;
            model.LastName = user.LastName;
            return View(model);
        }



        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> EditRole(string id,string role)
        {


          var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilizatorul nu a fost gasit !";
                return RedirectToAction("List");
            }

            if(!(await _roleManager.RoleExistsAsync(role)))
            {
                TempData["ErrorMessage"] = "Rolul selectat nu exista !";
                return RedirectToAction("EditRole");
            }
            var userroles = await _userManager.GetRolesAsync(user);
            var oldrole = userroles.First();
            if (oldrole != role)
            {
                await _userManager.RemoveFromRoleAsync(user, oldrole);
                await _userManager.AddToRoleAsync(user, role);
            }

            TempData["SuccesMessage"] = "Rolul a fost schimbat cu succes!";
            return RedirectToAction("List");


        }


    }
}
