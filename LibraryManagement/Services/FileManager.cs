using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LibraryManagement.Models;
using LibraryManagement.Utils;

namespace LibraryManagement.Services;


public class FileManager
{
    private readonly string _dataDir;

    public FileManager(string? dataDir = null)
    {
        _dataDir = dataDir ?? Path.Combine(FindProjectDirectory(), "Data");
    }

    private string UsersPath => Path.Combine(_dataDir, "users.txt");
    private string BooksPath => Path.Combine(_dataDir, "books.txt");
    private string BorrowsPath => Path.Combine(_dataDir, "borrows.txt");
    private string NotificationsPath => Path.Combine(_dataDir, "notifications.txt");

    public string DataDirectory => _dataDir;

    private static string FindProjectDirectory()
    {
        try
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (dir.GetFiles("*.csproj").Length > 0)
                    return dir.FullName;
                dir = dir.Parent;
            }
        }
        catch (Exception)
        {
            // ignore and use the fallback below
        }
        return AppContext.BaseDirectory;
    }


    public void EnsureDataFiles()
    {
        try
        {
            Directory.CreateDirectory(_dataDir);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new LibraryException($"Could not create data folder: {ex.Message}");
        }

        if (!File.Exists(UsersPath))
            SaveUsers(DefaultUsers());
        if (!File.Exists(BooksPath))
            SaveBooks(DefaultBooks());
        if (!File.Exists(BorrowsPath))
            SaveBorrows(DefaultBorrows());
    }

    private static List<User> DefaultUsers()
    {
        return new List<User>
        {
            new ClientUser(1001, "alex99", PasswordHasher.Hash("pass123"), 0.00m),
            new AdminUser(1002, "admin_giorgi", PasswordHasher.Hash("secureAdmin"), 0.00m),
            new ClientUser(1003, "nino_k", PasswordHasher.Hash("koba777"), 5.50m)
        };
    }

    private static List<Book> DefaultBooks()
    {
        return new List<Book>
        {
            new Book("978-1", "Clean Code", "Robert Martin", 5),
            new Book("978-2", "Pro Angular", "Adam Freeman", 0),
            new Book("978-3", "The Pragmatic Programmer", "Andrew Hunt", 3),
            new Book("978-4", "C# in Depth", "Jon Skeet", 4),
            new Book("978-5", "Design Patterns", "Erich Gamma", 2),
            new Book("978-6", "Introduction to Algorithms", "Thomas Cormen", 3),
            new Book("978-7", "Head First C#", "Andrew Stellman", 6)
        };
    }

    private static List<BorrowRecord> DefaultBorrows()
    {
        DateTime today = DateTime.Today;
        return new List<BorrowRecord>
        {
            // due tomorrow -> shows up as a "1 day left" warning
            new BorrowRecord("B201", 1001, "978-1", today.AddDays(1), BorrowStatus.Approved),
            // waiting for the admin (this book has 0 copies -> stock check demo)
            new BorrowRecord("B202", 1003, "978-2", today.AddDays(14), BorrowStatus.Pending),
            // overdue by 3 days -> shows up as a "late" notification
            new BorrowRecord("B203", 1003, "978-3", today.AddDays(-3), BorrowStatus.Approved)
        };
    }

    // ------------------------------------------------------------------
    // Loading
    // ------------------------------------------------------------------
    public List<User> LoadUsers()
    {
        return LoadFile(UsersPath, "users", ParseUser);
    }

    public List<Book> LoadBooks()
    {
        return LoadFile(BooksPath, "books", ParseBook);
    }

    public List<BorrowRecord> LoadBorrows()
    {
        return LoadFile(BorrowsPath, "borrows", ParseBorrow);
    }

    private static List<T> LoadFile<T>(string path, string label, Func<string, T> parser)
    {
        var items = new List<T>();
        try
        {
            int lineNumber = 0;
            foreach (string rawLine in File.ReadLines(path))
            {
                lineNumber++;
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//"))
                    continue;

                try
                {
                    items.Add(parser(line));
                }
                catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is LibraryException)
                {
                    // one broken line must not kill the whole program
                    Console.WriteLine($"[warning] {label}.txt line {lineNumber} skipped: {ex.Message}");
                }
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new LibraryException($"Could not read {label}.txt: {ex.Message}");
        }
        return items;
    }

    private static string[] SplitLine(string line, int expectedParts)
    {
        string[] parts = line.Split('|').Select(p => p.Trim()).ToArray();
        if (parts.Length != expectedParts)
            throw new FormatException($"expected {expectedParts} values separated by '|' but found {parts.Length}.");
        return parts;
    }

    private static User ParseUser(string line)
    {
        string[] p = SplitLine(line, 5);

        int id = int.Parse(p[0], CultureInfo.InvariantCulture);
        string username = p[1];
        // If someone typed a plain password into the file (like the assignment example), hash it now.
        string passwordHash = PasswordHasher.IsHash(p[2]) ? p[2] : PasswordHasher.Hash(p[2]);
        string role = p[3].ToLowerInvariant();
        decimal fines = decimal.Parse(p[4], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

        if (role == "admin")
            return new AdminUser(id, username, passwordHash, fines);
        if (role == "client")
            return new ClientUser(id, username, passwordHash, fines);

        throw new FormatException($"unknown role '{p[3]}'.");
    }

    private static Book ParseBook(string line)
    {
        string[] p = SplitLine(line, 4);
        int quantity = int.Parse(p[3], CultureInfo.InvariantCulture);
        return new Book(p[0], p[1], p[2], quantity);
    }

    private static BorrowRecord ParseBorrow(string line)
    {
        string[] p = SplitLine(line, 5);

        int userId = int.Parse(p[1], CultureInfo.InvariantCulture);
        DateTime returnDate = DateTime.ParseExact(p[3], "yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (!Enum.TryParse(p[4], true, out BorrowStatus status))
            throw new FormatException($"unknown status '{p[4]}'.");

        return new BorrowRecord(p[0], userId, p[2], returnDate, status);
    }

    // ------------------------------------------------------------------
    // Saving
    // ------------------------------------------------------------------
    public void SaveUsers(IEnumerable<User> users)
    {
        var lines = new List<string> { "# ID | Username | PasswordHash | Role | Fines" };
        lines.AddRange(users.Select(u => u.ToFileLine()));
        WriteLines(UsersPath, lines);
    }

    public void SaveBooks(IEnumerable<Book> books)
    {
        var lines = new List<string> { "# ISBN | Title | Author | Quantity" };
        lines.AddRange(books.Select(b => b.ToFileLine()));
        WriteLines(BooksPath, lines);
    }

    public void SaveBorrows(IEnumerable<BorrowRecord> borrows)
    {
        var lines = new List<string> { "# BorrowID | UserID | ISBN | ReturnDate | Status" };
        lines.AddRange(borrows.Select(r => r.ToFileLine()));
        WriteLines(BorrowsPath, lines);
    }

    /// <summary>Keeps a log of every simulated e-mail that was "sent".</summary>
    public void LogNotifications(IEnumerable<EmailNotification> notifications)
    {
        try
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var lines = new List<string>();
            foreach (EmailNotification n in notifications)
            {
                lines.Add($"[{stamp}] To: {n.To} | Subject: {n.Subject} | {n.Body.Replace(Environment.NewLine, " ")}");
            }
            File.AppendAllLines(NotificationsPath, lines);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new LibraryException($"Could not write notifications log: {ex.Message}");
        }
    }

    private void WriteLines(string path, IEnumerable<string> lines)
    {
        try
        {
            Directory.CreateDirectory(_dataDir);
            File.WriteAllLines(path, lines);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new LibraryException($"Could not save {Path.GetFileName(path)}: {ex.Message}");
        }
    }
}
