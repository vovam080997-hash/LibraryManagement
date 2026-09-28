using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibraryManagement.Models;
using LibraryManagement.Utils;

namespace LibraryManagement.Services;



public class LibrarySystem
{
    public const int LoanDays = 14;      
    public const decimal FinePerDay = 0.50m; 

    private static readonly Regex UsernameRegex = new Regex("^[A-Za-z0-9_]{3,20}$");

    private readonly FileManager _fileManager;
    private readonly List<User> _users;
    private readonly List<Book> _books;
    private readonly List<BorrowRecord> _borrows;

    public LibrarySystem(FileManager fileManager)
    {
        _fileManager = fileManager;
        _users = _fileManager.LoadUsers();
        _books = _fileManager.LoadBooks();
        _borrows = _fileManager.LoadBorrows();
    }

    public IReadOnlyList<Book> Books => _books;
    public IReadOnlyList<User> Users => _users;
    public IReadOnlyList<BorrowRecord> Borrows => _borrows;
    public DateTime Today => DateTime.Today;

    public Book? GetBook(string isbn)
    {
        return _books.FirstOrDefault(b => b.Isbn.Equals(isbn.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public User? GetUser(int id)
    {
        return _users.FirstOrDefault(u => u.Id == id);
    }

    private BorrowRecord? GetBorrow(string borrowId)
    {
        return _borrows.FirstOrDefault(r => r.BorrowId.Equals(borrowId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public List<BorrowRecord> GetBorrowsOfUser(int userId)
    {
        return _borrows.Where(r => r.UserId == userId).ToList();
    }

    public List<BorrowRecord> GetPendingRequests()
    {
        return _borrows.Where(r => r.Status == BorrowStatus.Pending).ToList();
    }

    public User Register(string username, string password, bool asAdmin)
    {
        username = username.Trim();

        if (!UsernameRegex.IsMatch(username))
            throw new LibraryException("Username must be 3-20 characters: letters, digits or underscore only.");

        if (password.Length < 6 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit) || password.Any(char.IsWhiteSpace))
            throw new LibraryException("Password must be at least 6 characters, contain a letter and a digit, and have no spaces.");

        if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            throw new LibraryException("That username is already taken.");

        int newId = _users.Count == 0 ? 1001 : _users.Max(u => u.Id) + 1;
        string hash = PasswordHasher.Hash(password);

        User user;
        if (asAdmin)
            user = new AdminUser(newId, username, hash);
        else
            user = new ClientUser(newId, username, hash);

        _users.Add(user);
        _fileManager.SaveUsers(_users);
        return user;
    }

    public User Login(string username, string password)
    {
        User? user = _users.FirstOrDefault(u => u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user == null)
            throw new LibraryException("No user with that username exists.");
        if (!user.CheckPassword(password))
            throw new LibraryException("Incorrect password.");
        return user;
    }

    public List<Book> SearchBooks(string keyword)
    {
        keyword = keyword.Trim();
        return _books
            .Where(b => b.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                     || b.Author.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }


    public BorrowRecord RequestBorrow(User user, string isbn)
    {
        Book? book = GetBook(isbn);
        if (book == null)
            throw new LibraryException("No book with that ISBN exists in the catalog.");

        if (user.HasOutstandingFines)
            throw new LibraryException($"You have unpaid fines ({user.Fines:F2}). Pay them before borrowing a new book.");

        if (HasOverdueLoans(user.Id))
            throw new LibraryException("You have an overdue book. Return it (and pay any fine) before borrowing again.");

        if (!book.IsAvailable)
            throw new LibraryException($"\"{book.Title}\" is not available right now (0 copies in stock).");

        bool alreadyRequested = _borrows.Any(r => r.UserId == user.Id
                                               && r.Isbn.Equals(book.Isbn, StringComparison.OrdinalIgnoreCase)
                                               && (r.Status == BorrowStatus.Pending || r.Status == BorrowStatus.Approved));
        if (alreadyRequested)
            throw new LibraryException("You already have a pending request or an active loan for this book.");

        var record = new BorrowRecord(NextBorrowId(), user.Id, book.Isbn, Today.AddDays(LoanDays), BorrowStatus.Pending);
        _borrows.Add(record);
        _fileManager.SaveBorrows(_borrows);
        return record;
    }


    public decimal ReturnBook(User user, string borrowId)
    {
        BorrowRecord? record = GetBorrow(borrowId);
        if (record == null || record.UserId != user.Id)
            throw new LibraryException("No borrow with that ID was found on your account.");
        if (record.Status != BorrowStatus.Approved)
            throw new LibraryException($"This borrow cannot be returned (status: {record.Status}).");

        decimal fine = record.DaysOverdue(Today) * FinePerDay;

        record.MarkReturned();
        GetBook(record.Isbn)?.IncreaseQuantity(1);
        if (fine > 0)
            user.AddFine(fine);

        SaveAll();
        return fine;
    }

    public bool HasOverdueLoans(int userId)
    {
        return _borrows.Any(r => r.UserId == userId && r.IsOverdue(Today));
    }


    public decimal CalculateAccruingFine(int userId)
    {
        return _borrows.Where(r => r.UserId == userId).Sum(r => r.DaysOverdue(Today) * FinePerDay);
    }

    public void PayFine(User user, decimal amount)
    {
        user.PayFine(amount);
        _fileManager.SaveUsers(_users);
    }


    public BorrowRecord ApproveRequest(string borrowId)
    {
        BorrowRecord? record = GetBorrow(borrowId);
        if (record == null)
            throw new LibraryException("No borrow request with that ID exists.");
        if (record.Status != BorrowStatus.Pending)
            throw new LibraryException($"This request is not pending (status: {record.Status}).");

        Book? book = GetBook(record.Isbn);
        if (book == null)
            throw new LibraryException("The requested book no longer exists in the catalog.");
        if (!book.IsAvailable)
            throw new LibraryException($"Cannot approve: \"{book.Title}\" has 0 copies in stock. You can reject the request instead.");

        User? user = GetUser(record.UserId);
        if (user != null && user.HasOutstandingFines)
            throw new LibraryException($"Cannot approve: {user.Username} has unpaid fines ({user.Fines:F2}).");

        book.DecreaseQuantity(1);
        record.Approve(Today.AddDays(LoanDays));
        SaveAll();
        return record;
    }

    public void RejectRequest(string borrowId)
    {
        BorrowRecord? record = GetBorrow(borrowId);
        if (record == null)
            throw new LibraryException("No borrow request with that ID exists.");

        record.Reject();
        _fileManager.SaveBorrows(_borrows);
    }

    public void AddBook(string isbn, string title, string author, int quantity)
    {
        if (GetBook(isbn) != null)
            throw new LibraryException("A book with that ISBN already exists. Use \"change quantity\" to add copies.");

        _books.Add(new Book(isbn, title, author, quantity));
        _fileManager.SaveBooks(_books);
    }

    public void RemoveBook(string isbn)
    {
        Book? book = GetBook(isbn);
        if (book == null)
            throw new LibraryException("No book with that ISBN exists.");

        bool inUse = _borrows.Any(r => r.Isbn.Equals(book.Isbn, StringComparison.OrdinalIgnoreCase)
                                    && (r.Status == BorrowStatus.Pending || r.Status == BorrowStatus.Approved));
        if (inUse)
            throw new LibraryException("Cannot remove this book: it has pending requests or is currently borrowed.");

        _books.Remove(book);
        _fileManager.SaveBooks(_books);
    }

    public void IncreaseBookQuantity(string isbn, int amount)
    {
        Book? book = GetBook(isbn);
        if (book == null)
            throw new LibraryException("No book with that ISBN exists.");

        book.IncreaseQuantity(amount);
        _fileManager.SaveBooks(_books);
    }

    public void DecreaseBookQuantity(string isbn, int amount)
    {
        Book? book = GetBook(isbn);
        if (book == null)
            throw new LibraryException("No book with that ISBN exists.");

        book.DecreaseQuantity(amount);
        _fileManager.SaveBooks(_books);
    }

    public List<EmailNotification> SendDueNotifications()
    {
        var result = new List<EmailNotification>();

        foreach (BorrowRecord record in _borrows.Where(r => r.Status == BorrowStatus.Approved))
        {
            User? user = GetUser(record.UserId);
            if (user == null)
                continue;

            string title = GetBook(record.Isbn)?.Title ?? record.Isbn;
            string dueText = record.ReturnDate.ToString("yyyy-MM-dd");
            int daysLeft = (record.ReturnDate.Date - Today).Days;

            if (daysLeft == 1)
            {
                result.Add(new EmailNotification(
                    user.Email,
                    "Reminder: book due tomorrow",
                    $"Hello {user.Username}, \"{title}\" is due tomorrow ({dueText}). Please return it on time."));
            }
            else if (daysLeft < 0)
            {
                int late = -daysLeft;
                decimal fineSoFar = late * FinePerDay;
                result.Add(new EmailNotification(
                    user.Email,
                    "OVERDUE: please return your book",
                    $"Hello {user.Username}, \"{title}\" was due on {dueText} and is {late} day(s) late. " +
                    $"Fine so far: {fineSoFar:F2} ({FinePerDay:F2} per day). Please return it as soon as possible."));
            }
        }

        if (result.Count > 0)
            _fileManager.LogNotifications(result);

        return result;
    }

    private string NextBorrowId()
    {
        int max = 200; // first ID will be B201
        foreach (BorrowRecord r in _borrows)
        {
            if (r.BorrowId.Length > 1 && int.TryParse(r.BorrowId.Substring(1), out int number) && number > max)
                max = number;
        }
        return "B" + (max + 1);
    }

    private void SaveAll()
    {
        _fileManager.SaveUsers(_users);
        _fileManager.SaveBooks(_books);
        _fileManager.SaveBorrows(_borrows);
    }
}
