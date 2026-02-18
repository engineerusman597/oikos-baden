using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.IO;
using Oikos.Infrastructure.Constants;

namespace Oikos.Infrastructure.CompanyCheck;

public class CompanyCheckReportFormatter
{
    public byte[] ApplyBranding(byte[]? pdfData)
    {
        if (pdfData == null || pdfData.Length == 0)
            return Array.Empty<byte>();

        try
        {
            using var inputStream = new MemoryStream(pdfData);
            using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

            if (document.PageCount == 0)
                return pdfData;

            using var firstLogo = LoadImage(CompanyCheckReportConstants.FirstPageLogoFile);
            using var otherLogo = LoadImage(CompanyCheckReportConstants.OtherPagesLogoFile);
            using var creditsafeLogo = LoadImage(CompanyCheckReportConstants.CreditsafeLogoFile);

            for (int i = 0; i < document.PageCount; i++)
            {
                var page = document.Pages[i];
                using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

                var logo = i == 0 ? firstLogo : otherLogo;
                if (logo != null)
                {
                    double horizontalPadding = CompanyCheckReportConstants.Padding;
                    double verticalPadding = CompanyCheckReportConstants.Padding / 2;

                    double targetWidth = CompanyCheckReportConstants.FirstLogoTargetWidth;
                    double ratio = logo.PixelHeight / (double)logo.PixelWidth;
                    double logoWidth = targetWidth;
                    double logoHeight = targetWidth * ratio;

                    double overlayWidth = logoWidth + (horizontalPadding * 2);
                    double overlayHeight = verticalPadding + logoHeight + verticalPadding;

                    double x = CompanyCheckReportConstants.Padding;
                    double y = 0;

                    var bgRect = new XRect(x, y, overlayWidth, overlayHeight);
                    graphics.DrawRectangle(XBrushes.White, bgRect);

                    var imgRect = new XRect(
                        x + horizontalPadding,
                        y + verticalPadding,
                        logoWidth,
                        logoHeight);

                    graphics.DrawImage(logo, imgRect);
                }

                if (creditsafeLogo != null)
                {
                    double horizontalPadding = CompanyCheckReportConstants.Padding / 2;
                    double verticalPadding = CompanyCheckReportConstants.Padding / 2;

                    double targetWidth = CompanyCheckReportConstants.CreditsafeLogoTargetWidth;
                    double ratio = creditsafeLogo.PixelHeight / (double)creditsafeLogo.PixelWidth;
                    double logoWidth = targetWidth;
                    double logoHeight = targetWidth * ratio;

                    double overlayWidth = logoWidth + (horizontalPadding * 2);
                    double overlayHeight = verticalPadding + logoHeight + verticalPadding;

                    double x = CompanyCheckReportConstants.Padding;
                    double y = page.Height.Point - overlayHeight - CompanyCheckReportConstants.Padding;

                    var bgRect = new XRect(x, y, overlayWidth, overlayHeight);
                    graphics.DrawRectangle(XBrushes.White, bgRect);

                    var imgRect = new XRect(
                        x + horizontalPadding,
                        y + verticalPadding,
                        logoWidth,
                        logoHeight);

                    graphics.DrawImage(creditsafeLogo, imgRect);
                }
            }

            using var outputStream = new MemoryStream();
            document.Save(outputStream, false);
            return outputStream.ToArray();
        }
        catch
        {
            return pdfData;
        }
    }

    private XImage? LoadImage(string fileName)
    {
        try
        {
            var baseDir = AppContext.BaseDirectory ?? string.Empty;
            var path = Path.Combine(baseDir, "wwwroot", "images", fileName);
            if (!File.Exists(path)) return null;
            return XImage.FromFile(path);
        }
        catch
        {
            return null;
        }
    }
}
