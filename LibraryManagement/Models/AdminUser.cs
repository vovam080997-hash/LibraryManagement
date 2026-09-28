using System;

namespace LibraryManagement.Models;

public class AdminUser : User
{
    public AdminUser(int id, string username, string passwordHash, decimal fines = 0m)
        : base(id, username, passwordHash, fines)
    {
    }

    public override string Role => "Admin";

    public override void DisplayMenu()
    {
        PrintHeader("ADMIN MENU");
        Console.WriteLine(" 1. Browse catalog");
        Console.WriteLine(" 2. Search books (title / author)");
        Console.WriteLine(" 3. Pending borrow requests (approve / reject)");
        Console.WriteLine(" 4. Add a new book");
        Console.WriteLine(" 5. Remove a book");
        Console.WriteLine(" 6. Increase / decrease book quantity");
        Console.WriteLine(" 7. Send due-date notifications");
        Console.WriteLine(" 8. View all borrow records");
        Console.WriteLine(" 9. View all users and fines");
        Console.WriteLine(" 0. Logout");
    }
}
