namespace Oikos.Application.Services.Email.Templates;

public static class InvoiceSubmittedEmailTemplate
{
    public const string Subject = "Rechnung erfolgreich eingereicht";

    public static string Build(string userName, string ticketNumber, string debtorCompany)
    {
        var body = new List<string>
        {
            $"Ihre Rechnung bezüglich **{debtorCompany}** wurde erfolgreich in unser System eingereicht.",
            $"**Ticket-Nummer:** {ticketNumber}",
            "Wir prüfen Ihre Einreichung derzeit und werden in Kürze mit den nächsten Schritten fortfahren.",
            "Sie können den Status Ihrer Rechnung in Ihrem Dashboard verfolgen."
        };

        return StandardEmailTemplate.Render(
            Subject,
            userName,
            body,
            footerText: "Vielen Dank, dass Sie Rechtfix nutzen.");
    }
}
