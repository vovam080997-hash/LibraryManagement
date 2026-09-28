using System;
using System.Globalization;

namespace LibraryManagement.Models;

public class BorrowRecord
{
    private readonly string _borrowId;
    private readonly int _userId;
    private readonly string _isbn;
    private DateTime _returnDate;
    private BorrowStatus _status;

    public BorrowRecord(string borrowId, int userId, string isbn, DateTime returnDate, BorrowStatus status)
    {
        if (string.IsNullOrWhiteSpace(borrowId))
            throw new LibraryException("Borrow ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(isbn))
            throw new LibraryException("ISBN cannot be empty.");

        _borrowId = borrowId.Trim();
        _userId = userId;
        _isbn = isbn.Trim();
        _returnDate = returnDate.Date;
        _status = status;
    }

    public string BorrowId => _borrowId;
    public int UserId => _userId;
    public string Isbn => _isbn;
    public DateTime ReturnDate => _returnDate;
    public BorrowStatus Status => _status;

    public bool IsOverdue(DateTime today)
    {
        return _status == BorrowStatus.Approved && today.Date > _returnDate;
    }

    public int DaysOverdue(DateTime today)
    {
        return IsOverdue(today) ? (today.Date - _returnDate).Days : 0;
    }

    public void Approve(DateTime newReturnDate)
    {
        if (_status != BorrowStatus.Pending)
            throw new LibraryException("Only pending requests can be approved.");
        _status = BorrowStatus.Approved;
        _returnDate = newReturnDate.Date;
    }

    public void Reject()
    {
        if (_status != BorrowStatus.Pending)
            throw new LibraryException("Only pending requests can be rejected.");
        _status = BorrowStatus.Rejected;
    }

    public void MarkReturned()
    {
        if (_status != BorrowStatus.Approved)
            throw new LibraryException("Only approved (active) borrows can be returned.");
        _status = BorrowStatus.Returned;
    }

    public string ToFileLine()
    {
        string date = _returnDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{_borrowId} | {_userId.ToString(CultureInfo.InvariantCulture)} | {_isbn} | {date} | {_status}";
    }
}
