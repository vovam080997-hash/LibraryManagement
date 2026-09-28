using System;

namespace LibraryManagement.Models;

public class EmailNotification
{
    public EmailNotification(string to, string subject, string body)
    {
        To = to;
        Subject = subject;
        Body = body;
    }

    public string To { get; }
    public string Subject { get; }
    public string Body { get; }

    public override string ToString()
    {
        return $"To: {To}{Environment.NewLine}Subject: {Subject}{Environment.NewLine}{Body}";
    }
}
