using System;
using System.Collections.Generic;
using System.Linq;
using LibraryData;
using LibraryData.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryServices
{
    public class CheckoutService : ICheckout
    {
        private LibraryContext _context; // private field to store the context.

        public CheckoutService(LibraryContext context)
        {
            _context = context;
        }

        public void Add(Checkout newCheckout)
        {
            _context.Add(newCheckout);
            _context.SaveChanges();
        }

        public Checkout GetById(int id)
        {
            return _context.Checkouts.FirstOrDefault(p => p.Id == id);
        }

        public IEnumerable<Checkout> GetAll()
        {
            return _context.Checkouts;
        }

        public IEnumerable<CheckoutHistory> GetCheckoutHistory(int id)
        {
            return _context.CheckoutHistories
                .Include(a => a.LibraryAsset)
                .Include(a => a.LibraryCard)
                .Where(a => a.LibraryAsset.Id == id);
        }

        public IEnumerable<Hold> GetCurrentHolds(int id)
        {
            return _context.Holds
                .Include(h => h.LibraryAsset)
                .Where(a => a.LibraryAsset.Id == id);
        }

        public Checkout GetLatestCheckout(int id)
        {
            return _context.Checkouts
                .Where(c => c.LibraryAsset.Id == id)
                .OrderByDescending(c => c.Since)
                .FirstOrDefault();
        }

        public void MarkLost(int assetId)
        {
            var item = _context.LibraryAssets
                .FirstOrDefault(a => a.Id == assetId);

            _context.Update(item);

            item.Status = _context.Statuses.FirstOrDefault(a => a.Name == "Lost");

            _context.SaveChanges();
        }

        public void MarkFound(int assetId)
        {
            var item = _context.LibraryAssets
                .FirstOrDefault(a => a.Id == assetId);

            _context.Update(item);
            item.Status = _context.Statuses.FirstOrDefault(a => a.Name == "Available");
            var now = DateTime.Now;

            // remove any existing checkouts on the item
            var checkout = _context.Checkouts
                .FirstOrDefault(a => a.LibraryAsset.Id == assetId);
            if (checkout != null)
            {
                _context.Remove(checkout);
            }

            // close any existing checkout history
            var history = _context.CheckoutHistories
                .FirstOrDefault(h =>
                    h.LibraryAsset.Id == assetId
                    && h.CheckedIn == null);
            if (history != null)
            {
                _context.Update(history);
                history.CheckedIn = now;
            }

            _context.SaveChanges();
        }

        public void CheckOutItem(int assetId, int libraryCardId)
        {
            if (IsCheckedOut(assetId))
            {
                return;
            }

            var item = _context.LibraryAssets
                .Include(a=>a.Status)
                .FirstOrDefault(a=> a.Id == assetId);

            _context.Update(item);

            item.Status = _context.Statuses
                .FirstOrDefault(a => a.Name == "Checked Out");

            var now = DateTime.Now;

            var libraryCard = _context.LibraryCards
                .Include(c=>c.Checkouts)
                .FirstOrDefault(a => a.Id == libraryCardId);

            var checkout = new Checkout
            {
                LibraryAsset = item,
                LibraryCard = libraryCard,
                Since = now,
                Until = GetDefaultCheckoutTime(now)
            };

            _context.Add(checkout);

            var checkoutHistory = new CheckoutHistory
            {
                CheckedOut = now,
                LibraryAsset = item,
                LibraryCard = libraryCard
            };

            _context.Add(checkoutHistory);
            _context.SaveChanges();
        }

        public bool IsCheckedOut(int id)
        {
            return _context.Checkouts.Any(a => a.LibraryAsset.Id == id);
        }

        private DateTime GetDefaultCheckoutTime(DateTime now)
        {
            return now.AddDays(30);
        }

        public void PlaceHold(int assetId, int libraryCardId)
        {
            var now = DateTime.Now;

            var asset = _context.LibraryAssets
                .Include(a=>a.Status)
                .FirstOrDefault(a=> a.Id == assetId);

            var card = _context.LibraryCards
                .FirstOrDefault(a=>a.Id == libraryCardId);

            _context.Update(asset);

            if(asset.Status.Name == "Available")
            {
                asset.Status = _context.Statuses.FirstOrDefault(a => a.Name == "On Hold");
            }

            var hold = new Hold
            {
                HoldPlaced = now,
                LibraryAsset = asset,
                LibraryCard = card
            };

            _context.Add(hold);
            _context.SaveChanges();
        }

        public void CheckInItem(int assetId)
        {
            var now = DateTime.Now;

            var item = _context.LibraryAssets
                .FirstOrDefault(a=> a.Id == assetId);

            _context.Update(item);

            // remove any existing checkouts on the item
            var checkout = _context.Checkouts
                .Include(c=>c.LibraryAsset)
                .Include(c=>c.LibraryCard)
                .FirstOrDefault(a => a.LibraryAsset.Id == assetId);
            if(checkout != null)
            {
                _context.Remove(checkout);
            }

            // close any existing checkout history
            var history = _context.CheckoutHistories
                .Include(h=>h.LibraryAsset)
                .Include(h=>h.LibraryCard)
                .FirstOrDefault(h =>
                h.LibraryAsset.Id == assetId 
                && h.CheckedIn == null);
            if (history != null)
            {
                _context.Update(history);
                history.CheckedIn = now;
            }

            // look for current holds
            var currentHolds = _context.Holds
                .Include(a=>a.LibraryAsset)
                .Include(a=>a.LibraryCard)
                .Where(a => a.LibraryAsset.Id == assetId);

            // if there are current holds, check out the item to the earliest
            if (currentHolds.Any())
            {
                CheckoutToEarliestHold(assetId, currentHolds);
                return;
            }

            // otherwise, set item status to available
            item.Status = _context.Statuses.FirstOrDefault(a => a.Name == "Available");

            _context.SaveChanges();
        }

        private void CheckoutToEarliestHold(int assetId, IQueryable<Hold> currentHolds)
        {
            var earliestHold = currentHolds.OrderBy(a => a.HoldPlaced).FirstOrDefault();
            var card = earliestHold.LibraryCard;
            _context.Remove(earliestHold);
            _context.SaveChanges();

            CheckOutItem(assetId, card.Id);
        }

        public string GetCurrentHoldPatronName(int holdId)
        {
            var hold = _context.Holds
                .Include(a => a.LibraryAsset)
                .Include(a => a.LibraryCard)
                .FirstOrDefault(v => v.Id == holdId);

            var cardId = hold?.LibraryCard.Id;

            var patron = _context.Patrons
                .Include(p => p.LibraryCard)
                .FirstOrDefault(p => p.LibraryCard.Id == cardId);

            return patron?.FirstName + " " + patron?.LastName;
        }

        public DateTime GetCurrentHoldPlaced(int holdId)
        {
            return _context.Holds
                .Include(a => a.LibraryAsset)
                .Include(a => a.LibraryCard)
                .FirstOrDefault(v => v.Id == holdId)
                .HoldPlaced;
        }

        public string GetCurrentCheckoutPatron(int id)
        {
            var checkout = _context.Checkouts
                .Include(a => a.LibraryAsset)
                .Include(a => a.LibraryCard)
                .FirstOrDefault(a => a.LibraryAsset.Id == id);

            if (checkout == null)
            {
                return "Not checked out.";
            }

            var cardId = checkout.LibraryCard.Id;

            var patron = _context.Patrons
                .Include(p => p.LibraryCard)
                .FirstOrDefault(c => c.LibraryCard.Id == cardId);

            return patron?.FirstName + " " + patron?.LastName;
        }


    }
}
