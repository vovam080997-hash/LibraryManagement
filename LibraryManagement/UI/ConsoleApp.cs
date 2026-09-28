using System;
using System.Collections.Generic;
using System.Linq;
using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.Utils;

namespace LibraryManagement.UI;

public class ConsoleApp
{
    private readonly LibrarySystem _library;
    private User? _currentUser;

    public ConsoleApp(LibrarySystem library)
    {
        _library = library;
    }

    public void Run()
    {
        Console.WriteLine("=====================================");
        Console.WriteLine("     LIBRARY MANAGEMENT SYSTEM");
        Console.WriteLine("=====================================");

        bool running = true;
        while (running)
        {
            if (_currentUser == null)
                running = ShowStartMenu();
            else
                ShowUserMenu(_currentUser);
        }

        Console.WriteLine("Goodbye!");
    }

    private bool ShowStartMenu()
    {
        Console.WriteLine();
        Console.WriteLine("----- MAIN MENU -----");
        Console.WriteLine(" 1. Register");
        Console.WriteLine(" 2. Login");
        Console.WriteLine(" 0. Exit");
        string choice = InputHelper.ReadLine("Choose an option: ");

        try
        {
            switch (choice)
            {
                case "1":
                    Register();
                    break;
                case "2":
                    Login();
                    break;
                case "0":
                    return false;
                default:
                    InputHelper.ShowError("That option does not exist. Please choose 1, 2 or 0.");
                    break;
            }
        }
        catch (LibraryException ex)
        {
            InputHelper.ShowError(ex.Message);
        }

        return true;
    }

    private void Register()
    {
        Console.WriteLine();
        Console.WriteLine("--- Registration ---");
        string username = InputHelper.ReadNonEmpty("Username (3-20 letters/digits/_): ");
        string password = InputHelper.ReadPassword("Password (min 6 chars, letter + digit): ");
        string repeat = InputHelper.ReadPassword("Repeat password: ");
        if (password != repeat)
            throw new LibraryException("The two passwords do not match.");

        Console.WriteLine("Role:  1 = Client (reader)   2 = Admin (librarian)");
        string role = InputHelper.ReadLine("Choose role: ");
        if (role != "1" && role != "2")
            throw new LibraryException("Invalid role choice. Please type 1 or 2.");

        User user = _library.Register(username, password, role == "2");
        InputHelper.ShowSuccess($"Registered as {user.Role}! Your user ID is {user.Id}. You can log in now.");
    }

    private void Login()
    {
        Console.WriteLine();
        Console.WriteLine("--- Login ---");
        string username = InputHelper.ReadNonEmpty("Username: ");
        string password = InputHelper.ReadPassword("Password: ");

        _currentUser = _library.Login(username, password);
        InputHelper.ShowSuccess($"Welcome, {_currentUser.Username}! (role: {_currentUser.Role})");
    }

    private void ShowUserMenu(User user)
    {
        user.DisplayMenu();
        string choice = InputHelper.ReadLine("Choose an option: ");

        try
        {
            if (choice == "0")
            {
                _currentUser = null;
                InputHelper.ShowInfo("You have been logged out.");
            }
            else if (user is AdminUser)
            {
                HandleAdminChoice(choice);
            }
            else
            {
                HandleClientChoice(user, choice);
            }
        }
        catch (LibraryException ex)
        {
            InputHelper.ShowError(ex.Message);
        }
    }

    private void HandleClientChoice(User user, string choice)
    {
        switch (choice)
        {
            case "1":
                ShowCatalog();
                break;
            case "2":
                SearchBooks();
                break;
            case "3":
                RequestBorrow(user);
                break;
            case "4":
                ReturnBook(user);
                break;
            case "5":
                ShowMyBorrows(user);
                break;
            case "6":
                ManageFines(user);
                break;
            default:
                InputHelper.ShowError("That option does not exist. Please choose a number from the menu.");
                break;
        }
    }

    private void HandleAdminChoice(string choice)
    {
        switch (choice)
        {
            case "1":
                ShowCatalog();
                break;
            case "2":
                SearchBooks();
                break;
            case "3":
                ReviewPendingRequests();
                break;
            case "4":
                AddBook();
                break;
            case "5":
                RemoveBook();
                break;
            case "6":
                ChangeQuantity();
                break;
            case "7":
                SendNotifications();
                break;
            case "8":
                Console.WriteLine();
                PrintBorrows(_library.Borrows, true);
                break;
            case "9":
                PrintUsers();
                break;
            default:
                InputHelper.ShowError("That option does not exist. Please choose a number from the menu.");
                break;
        }
    }

    private void ShowCatalog()
    {
        PrintBooks(_library.Books);
    }

    private void SearchBooks()
    {
        string keyword = InputHelper.ReadNonEmpty("Search (title or author): ");
        List<Book> results = _library.SearchBooks(keyword);
        PrintBooks(results);
    }

    private void RequestBorrow(User user)
    {
        PrintBooks(_library.Books);
        string isbn = InputHelper.ReadNonEmpty("Enter the ISBN of the book you want: ");

        BorrowRecord record = _library.RequestBorrow(user, isbn);
        InputHelper.ShowSuccess($"Request {record.BorrowId} sent. Status: Pending - wait for an admin to approve it.");
    }

    private void ReturnBook(User user)
    {
        List<BorrowRecord> active = _library.GetBorrowsOfUser(user.Id)
            .Where(r => r.Status == BorrowStatus.Approved)
            .ToList();

        if (active.Count == 0)
        {
            InputHelper.ShowInfo("You have no borrowed books to return.");
            return;
        }

        PrintBorrows(active, false);
        string borrowId = InputHelper.ReadNonEmpty("Enter the Borrow ID to return: ");

        decimal fine = _library.ReturnBook(user, borrowId);
        InputHelper.ShowSuccess("Book returned. Thank you!");
        if (fine > 0)
            InputHelper.ShowError($"The book was late. A fine of {fine:F2} was added to your account.");
    }

    private void ShowMyBorrows(User user)
    {
        PrintBorrows(_library.GetBorrowsOfUser(user.Id), false);
    }

    private void ManageFines(User user)
    {
        decimal accruing = _library.CalculateAccruingFine(user.Id);
        Console.WriteLine();
        Console.WriteLine($"Unpaid fines: {user.Fines:F2}");
        if (accruing > 0)
            InputHelper.ShowInfo($"Still growing on overdue books: {accruing:F2} (charged when you return them).");

        if (!user.HasOutstandingFines)
            return;

        if (!InputHelper.Confirm("Do you want to pay now?"))
            return;

        decimal amount = InputHelper.ReadPositiveDecimal("Amount to pay: ");
        _library.PayFine(user, amount);
        InputHelper.ShowSuccess($"Payment accepted. Remaining fines: {user.Fines:F2}");
    }

    private void ReviewPendingRequests()
    {
        List<BorrowRecord> pending = _library.GetPendingRequests();
        if (pending.Count == 0)
        {
            InputHelper.ShowInfo("There are no pending requests.");
            return;
        }

        PrintBorrows(pending, true);
        string borrowId = InputHelper.ReadLine("Enter a Borrow ID to review (or just press Enter to go back): ");
        if (borrowId.Length == 0)
            return;

        string decision = InputHelper.ReadLine("A = Approve, R = Reject: ").ToUpperInvariant();
        if (decision == "A")
        {
            BorrowRecord record = _library.ApproveRequest(borrowId);
            InputHelper.ShowSuccess($"Approved. Stock reduced by 1. Due date: {record.ReturnDate:yyyy-MM-dd}");
        }
        else if (decision == "R")
        {
            _library.RejectRequest(borrowId);
            InputHelper.ShowSuccess("Request rejected.");
        }
        else
        {
            InputHelper.ShowError("Invalid choice. Please type A or R.");
        }
    }

    private void AddBook()
    {
        Console.WriteLine();
        Console.WriteLine("--- Add a new book ---");
        string isbn = InputHelper.ReadNonEmpty("ISBN: ");
        string title = InputHelper.ReadNonEmpty("Title: ");
        string author = InputHelper.ReadNonEmpty("Author: ");
        int quantity = InputHelper.ReadPositiveInt("Quantity: ");

        _library.AddBook(isbn, title, author, quantity);
        InputHelper.ShowSuccess("Book added to the catalog.");
    }

    private void RemoveBook()
    {
        PrintBooks(_library.Books);
        string isbn = InputHelper.ReadNonEmpty("ISBN of the book to remove: ");

        Book? book = _library.GetBook(isbn);
        if (book == null)
            throw new LibraryException("No book with that ISBN exists.");

        if (!InputHelper.Confirm($"Really delete \"{book.Title}\"?"))
        {
            InputHelper.ShowInfo("Cancelled.");
            return;
        }

        _library.RemoveBook(isbn);
        InputHelper.ShowSuccess("Book removed.");
    }

    private void ChangeQuantity()
    {
        PrintBooks(_library.Books);
        string isbn = InputHelper.ReadNonEmpty("ISBN of the book: ");

        Console.WriteLine("1 = Increase quantity   2 = Decrease quantity");
        string mode = InputHelper.ReadLine("Choose: ");
        if (mode != "1" && mode != "2")
            throw new LibraryException("Invalid choice. Please type 1 or 2.");

        int amount = InputHelper.ReadPositiveInt("By how many copies: ");

        if (mode == "1")
            _library.IncreaseBookQuantity(isbn, amount);
        else
            _library.DecreaseBookQuantity(isbn, amount);

        Book? book = _library.GetBook(isbn);
        InputHelper.ShowSuccess($"Done. New quantity: {book?.Quantity}");
    }

    private void SendNotifications()
    {
        List<EmailNotification> emails = _library.SendDueNotifications();
        if (emails.Count == 0)
        {
            InputHelper.ShowInfo("Nobody needs a notification today (no loans due tomorrow or overdue).");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Sending {emails.Count} simulated e-mail(s)...");
        foreach (EmailNotification email in emails)
        {
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine(email);
        }
        Console.WriteLine("-----------------------------------------");
        InputHelper.ShowSuccess("All notifications sent (also saved in Data/notifications.txt).");
    }


    private static string Fit(string text, int width)
    {
        if (text.Length > width)
            text = text.Substring(0, width - 3) + "...";
        return text.PadRight(width);
    }

    private void PrintBooks(IEnumerable<Book> books)
    {
        List<Book> list = books.ToList();
        if (list.Count == 0)
        {
            InputHelper.ShowInfo("No books found.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{Fit("ISBN", 12)} {Fit("Title", 32)} {Fit("Author", 22)} {"Qty",4}");
        Console.WriteLine(new string('-', 73));
        foreach (Book b in list)
        {
            Console.WriteLine($"{Fit(b.Isbn, 12)} {Fit(b.Title, 32)} {Fit(b.Author, 22)} {b.Quantity,4}");
        }
    }

    private void PrintBorrows(IEnumerable<BorrowRecord> records, bool showUser)
    {
        List<BorrowRecord> list = records.ToList();
        if (list.Count == 0)
        {
            InputHelper.ShowInfo("No borrow records found.");
            return;
        }

        string userHeader = showUser ? Fit("User", 14) + " " : "";
        Console.WriteLine($"{Fit("BorrowID", 9)} {userHeader}{Fit("Book", 28)} {Fit("Due date", 11)} Status");
        Console.WriteLine(new string('-', showUser ? 82 : 67));

        foreach (BorrowRecord r in list)
        {
            string userCell = "";
            if (showUser)
            {
                string name = _library.GetUser(r.UserId)?.Username ?? ("#" + r.UserId);
                userCell = Fit(name, 14) + " ";
            }

            string title = _library.GetBook(r.Isbn)?.Title ?? r.Isbn;
            string status = r.Status.ToString();
            if (r.IsOverdue(_library.Today))
                status += $" (OVERDUE {r.DaysOverdue(_library.Today)}d)";

            Console.WriteLine($"{Fit(r.BorrowId, 9)} {userCell}{Fit(title, 28)} {Fit(r.ReturnDate.ToString("yyyy-MM-dd"), 11)} {status}");
        }
    }

    private void PrintUsers()
    {
        Console.WriteLine();
        Console.WriteLine($"{Fit("ID", 6)} {Fit("Username", 20)} {Fit("Role", 8)} Fines");
        Console.WriteLine(new string('-', 45));
        foreach (User u in _library.Users)
        {
            Console.WriteLine($"{Fit(u.Id.ToString(), 6)} {Fit(u.Username, 20)} {Fit(u.Role, 8)} {u.Fines:F2}");
        }
    }
}
