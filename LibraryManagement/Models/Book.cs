using System.Globalization;

namespace LibraryManagement.Models;

public class Book
{
    private readonly string _isbn;
    private readonly string _title;
    private readonly string _author;
    private int _quantity;

    public Book(string isbn, string title, string author, int quantity)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new LibraryException("ISBN cannot be empty.");
        if (string.IsNullOrWhiteSpace(title))
            throw new LibraryException("Title cannot be empty.");
        if (string.IsNullOrWhiteSpace(author))
            throw new LibraryException("Author cannot be empty.");
        if (quantity < 0)
            throw new LibraryException("Quantity cannot be negative.");

        _isbn = isbn.Trim();
        _title = title.Trim();
        _author = author.Trim();
        _quantity = quantity;
    }

    public string Isbn => _isbn;
    public string Title => _title;
    public string Author => _author;
    public int Quantity => _quantity;
    public bool IsAvailable => _quantity > 0;

    public void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new LibraryException("Amount must be a positive whole number.");
        _quantity += amount;
    }

    public void DecreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new LibraryException("Amount must be a positive whole number.");
        if (amount > _quantity)
            throw new LibraryException($"Cannot remove {amount} copies - only {_quantity} in stock.");
        _quantity -= amount;
    }

    public string ToFileLine()
    {
        return $"{_isbn} | {_title} | {_author} | {_quantity.ToString(CultureInfo.InvariantCulture)}";
    }
}
