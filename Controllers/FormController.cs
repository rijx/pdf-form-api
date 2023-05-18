using Microsoft.AspNetCore.Mvc;
using iText.Forms;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace PDFAPI.Controllers;

[ApiController]
public class FormController : ControllerBase
{
    public static readonly String FONT_BASE_DIR = "resources/fonts/";

    public class FormFillRequest
    {
        [Required]
        public IFormFile Pdf { get; set; } = null!;
        
        [Required]
        public string Values { get; set; } = string.Empty;
        
        public string? Font { get; set; }
        
        public string? AutoSizeFields { get; set; }
    }

    [HttpPost("fill")]
    public IActionResult Fill([FromForm] FormFillRequest request)
    {
        if (request.Pdf.ContentType != "application/pdf")
        {
            return BadRequest($"Invalid file type: {request.Pdf.ContentType}. Only PDF files are accepted.");
        }

        if (request.Pdf.Length > 100 * 1024 * 1024)
        {
            return BadRequest("File size exceeds the 100MB limit.");
        }

        var fontFamily = request.Font ?? "freesans/FreeSans-LrmZ.ttf";

        Dictionary<string, string> values;

        try
        {
            values = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.Values)!;
            
            if (values == null)
            {
                return BadRequest("Values cannot be null");
            }
        }
        catch (JsonReaderException ex)
        {
            return BadRequest($"Could not parse values: {ex.Message}");
        }

        List<string> autoSizeFields;

        try
        {
            autoSizeFields = JsonConvert.DeserializeObject<List<string>>(request.AutoSizeFields ?? "[]")!;
            
            if (autoSizeFields == null)
            {
                autoSizeFields = new List<string>();
            }
        }
        catch (JsonReaderException ex)
        {
            return BadRequest($"Could not parse autoSizeFields: {ex.Message}");
        }

        using (var outputStream = new MemoryStream())
        {
            using (var stream = request.Pdf.OpenReadStream())
            {
                var pdfDoc = new PdfDocument(new PdfReader(stream), new PdfWriter(outputStream));
                var pdfForm = PdfAcroForm.GetAcroForm(pdfDoc, true);

                // Being set as true, this parameter is responsible to generate an appearance Stream
                // while flattening for all form fields that don't have one. Generating appearances will
                // slow down form flattening, but otherwise Acrobat might render the pdf on its own rules.
                pdfForm.SetGenerateAppearance(true);

                var font = PdfFontFactory.CreateFont(FONT_BASE_DIR + fontFamily, PdfEncodings.IDENTITY_H);

                foreach (var pair in values)
                {
                    var field = pdfForm.GetField(pair.Key);
                    
                    if (field == null)
                    {
                        continue;
                    }

                    field.SetValue(pair.Value, font, !autoSizeFields.Contains(pair.Key) ? 11f : 0);
                }

                pdfDoc.Close();

                return File(outputStream.ToArray(), "application/pdf");
            }
        }
    }

    public record FieldProperties(string name, string? type, List<string> options);

    public class PdfAnalyzeRequest
    {
        [Required]
        public IFormFile Pdf { get; set; } = null!;
    }

    [HttpPost("analyze")]
    public ActionResult<List<FieldProperties>> Analyze([FromForm] PdfAnalyzeRequest request)
    {
        if (request.Pdf.ContentType != "application/pdf")
        {
            return BadRequest($"Invalid file type: {request.Pdf.ContentType}. Only PDF files are accepted.");
        }
        
        if (request.Pdf.Length > 100 * 1024 * 1024)
        {
            return BadRequest("File size exceeds the 100MB limit.");
        }
        
        using (var stream = request.Pdf.OpenReadStream())
        {
            var pdfDoc = new PdfDocument(new PdfReader(stream));
            var pdfForm = PdfAcroForm.GetAcroForm(pdfDoc, true);

            var result = pdfForm.GetFormFields()
                .Select(x => new FieldProperties(
                        x.Key,
                        x.Value.GetFormType()?.GetValue(),
                        x.Value.GetAppearanceStates().ToList()
                    ))
                .ToList();
                
            pdfDoc.Close();
            return result;
        }
    }
}
