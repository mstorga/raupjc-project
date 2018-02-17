using System.Collections.Generic;
using System.Linq;
using LibraryData;
using LibraryData.Models;
using LibraryProject.Models.Patron;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryProject.Controllers
{
    public class PatronController : Controller
    {
        private IPatron _patron;

        // create a constructor takes branchservice
        public PatronController(IPatron patron)
        {
            // save branchService param off into a private field 
            // to have access in the rest of the controller
            _patron = patron;
        }
        [Authorize]
        public IActionResult Index()
        {
            var allPatrons = _patron.GetAll();

            var patronModels = allPatrons 
                .Select(p => new PatronDetailModel
                {
                    Id = p.Id,
                    LastName = p.LastName,
                    FirstName = p.FirstName,
                    LibraryCardId = p.LibraryCard.Id,
                    OverdueFees = p.LibraryCard.Fees,
                    HomeLibraryBranch = p.HomeLibraryBranch.Name
                }).ToList();

            var model = new PatronIndexModel()
            {
                Patrons = patronModels 
            };

            return View(model);
        }
        [Authorize]
        public IActionResult Detail(int id)
        {
            var patron = _patron.Get(id);

            var model = new PatronDetailModel
            {
                LastName = patron.LastName,
                FirstName = patron.FirstName,
                Address = patron.Adress,
                HomeLibraryBranch = patron.HomeLibraryBranch.Name,
                MemberSince = patron.LibraryCard.Created,
                OverdueFees = patron.LibraryCard.Fees,
                LibraryCardId = patron.LibraryCard.Id,
                Telephone = patron.TelephoneNumber,
                AssetsCheckedOut = _patron.GetCheckouts(id).ToList() ?? new List<Checkout>(),
                CheckoutHistory = _patron.GetCheckoutHistory(id),
                Holds = _patron.GetHolds(id)
            };

            return View(model);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
