using Oikos.Application.Services.Invoice.Models;

namespace Oikos.Application.Services.Invoice;

public interface IPowerOfAttorneyPdfGenerator
{
    byte[] Generate(PowerOfAttorneyPdfDetails details, string templatePath);
}
