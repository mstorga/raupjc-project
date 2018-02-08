using System;
using LibraryData.Models;
using System.Collections.Generic;

namespace LibraryData
{
    public interface ICheckout
    {
        IEnumerable<Checkout> GetAll();
        Checkout GetById(int id);
        void Add(Checkout newCheckout);
        IEnumerable<CheckoutHistory> GetCheckoutHistory(int id);
        void CheckOutItem(int assetId, int libraryCardId);
        void CheckInItem(int assetId);
        Checkout GetLatestCheckout(int id);
        string GetCurrentCheckoutPatron(int id);


        void PlaceHold(int assetId, int libraryCardId);
        string GetCurrentHoldPatronName(int id);
        DateTime GetCurrentHoldPlaced(int id);
        IEnumerable<Hold> GetCurrentHolds(int id);

        void MarkLost(int id);
        void MarkFound(int id);
    }
}
