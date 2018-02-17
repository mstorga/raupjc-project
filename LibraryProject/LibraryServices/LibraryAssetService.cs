using System.Collections.Generic;
using LibraryData.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;
using LibraryData;

namespace LibraryServices
{
    public class LibraryAssetService : ILibraryAsset
    {
        private LibraryContext _context; // private field to store the context.

        public LibraryAssetService(LibraryContext context)
        {
            _context = context;
        }

        public void Add(LibraryAsset newAsset)
        {
            _context.Add(newAsset);
            _context.SaveChanges();
        }

        public IEnumerable<LibraryAsset> GetAll()
        {
            return _context.LibraryAssets
                .Include(a=>a.Status)
                .Include(a=>a.Location);
        }

        public LibraryAsset GetById(int id)
        {
            return _context.LibraryAssets
                .Include(a => a.Status)
                .Include(a => a.Location)
                .FirstOrDefault(a => a.Id == id);
        }

        public LibraryBranch GetCurrentLocation(int id)
        {
            return GetById(id).Location;
        }

        public string GetDeweyIndex(int id)
        {
            if (_context.Books.Any(a => a.Id == id))
            {
                return _context.Books
                    .FirstOrDefault(a => a.Id == id).DeweyIndex;
            }

            else return "";
        }

        public string GetIsbn(int id)
        {
            if (_context.Books.Any(a => a.Id == id))
            {
                return _context.Books
                    .FirstOrDefault(a => a.Id == id).ISBN;
            }

            else return "";
        }
        
        public string GetTitle(int id)
        {
            return _context.LibraryAssets
                .FirstOrDefault(a => a.Id == id)
                .Title;
        }

        public string GetType(int id)
        {
            var books = _context.LibraryAssets
                .OfType<Book>().Where(a => a.Id == id);

            return books.Any() ? "Book" : "Video";
        }

        public string GetAuthorOrDirector(int id)
        {
            var isBook = _context.LibraryAssets.OfType<Book>()
                .Where(a => a.Id == id).Any();

            return isBook ?
                _context.Books.FirstOrDefault(a => a.Id == id).Author :
                _context.Videos.FirstOrDefault(a => a.Id == id).Director
                ?? "Unknown";
        }
    }
}
