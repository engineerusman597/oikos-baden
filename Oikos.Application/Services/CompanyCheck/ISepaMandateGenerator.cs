using Oikos.Application.Services.CompanyCheck.Models;

namespace Oikos.Application.Services.CompanyCheck;

public interface ISepaMandateGenerator
{
    byte[] Generate(SepaMandateDetails details);
}
