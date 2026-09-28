using System;

namespace LibraryManagement.Models;

public class ClientUser : User
{
    public ClientUser(int id, string username, string passwordHash, decimal fines = 0m)
        : base(id, username, passwordHash, fines)
    {
    }

    public override string Role => "Client";

    public override void DisplayMenu()
    {
        PrintHeader("CLIENT MENU");
        Console.WriteLine(" 1. Browse catalog");
        Console.WriteLine(" 2. Search books (title / author)");
        Console.WriteLine(" 3. Request to borrow a book");
        Console.WriteLine(" 4. Return a book");
        Console.WriteLine(" 5. My borrows");
        Console.WriteLine(" 6. My fines (view / pay)");
        Console.WriteLine(" 0. Logout");
    }
}
