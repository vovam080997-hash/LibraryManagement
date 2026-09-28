using System;
using System.Globalization;
using LibraryManagement.Utils;

namespace LibraryManagement.Models;

public abstract class User
{
    private readonly int _id;
    private readonly string _username;
    private readonly string _passwordHash;
    private decimal _fines;

    protected User(int id, string username, string passwordHash, decimal fines)
    {
        if (id <= 0)
            throw new LibraryException("User ID must be positive.");
        if (string.IsNullOrWhiteSpace(username))
            throw new LibraryException("Username cannot be empty.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new LibraryException("Password cannot be empty.");
        if (fines < 0)
            throw new LibraryException("Fines cannot be negative.");

        _id = id;
        _username = username.Trim();
        _passwordHash = passwordHash.Trim();
        _fines = fines;
    }

    public int Id => _id;
    public string Username => _username;
    public decimal Fines => _fines;
    public bool HasOutstandingFines => _fines > 0;
    public string PasswordHash => _passwordHash;

    public string Email => _username.ToLowerInvariant() + "@library-sim.com";


    public abstract string Role { get; }
    public abstract void DisplayMenu();

    public bool CheckPassword(string plainPassword)
    {
        return string.Equals(_passwordHash, PasswordHasher.Hash(plainPassword), StringComparison.OrdinalIgnoreCase);
    }

    public void AddFine(decimal amount)
    {
        if (amount <= 0)
            throw new LibraryException("Fine amount must be positive.");
        _fines += amount;
    }

    public void PayFine(decimal amount)
    {
        if (amount <= 0)
            throw new LibraryException("Payment must be greater than zero.");
        if (amount > _fines)
            throw new LibraryException($"You only owe {_fines:F2}. You cannot pay more than that.");
        _fines -= amount;
    }

    public string ToFileLine()
    {
        string fines = _fines.ToString("F2", CultureInfo.InvariantCulture);
        return $"{_id.ToString(CultureInfo.InvariantCulture)} | {_username} | {_passwordHash} | {Role.ToLowerInvariant()} | {fines}";
    }

    protected void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"===== {title} =====");
        Console.WriteLine($"User: {_username} ({Role}) | Unpaid fines: {_fines:F2}");
        Console.WriteLine("-----------------------------");
    }
}
