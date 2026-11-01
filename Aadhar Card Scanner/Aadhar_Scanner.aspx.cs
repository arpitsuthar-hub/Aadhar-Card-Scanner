using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Xml;

namespace Aadhar_Card_Scanner
{
    public partial class Aadhar_Scanner : Page
    {
        protected async void BtnScan_Click(object sender, EventArgs e)
        {
            if (!fuFront.HasFile || !fuBack.HasFile)
            {
                lblMessage.Text = "Please upload both images.";
                return;
            }

            string[] allowedExtension= { ".jpg", ".jpeg", ".png" };

            string frontExtension = Path.GetExtension(fuFront.FileName).ToLowerInvariant();
            string backExtension = Path.GetExtension(fuBack.FileName).ToLowerInvariant();

            if(!allowedExtension.Contains(frontExtension) || !allowedExtension.Contains(backExtension))
            {
                lblMessage.Text = "Only JPG, JPEG, and PNG images are allowed.";
                return;
            }

            string apiKey = "K8802186477295388957";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(90);

                    string frontText = await ScanImage(client, fuFront.PostedFile, apiKey);

                    string backText = await ScanImage(client, fuBack.PostedFile, apiKey);

                    string allText = frontText + "\n" + backText;

                    pnlResults.Visible = true;

                    txtName.Text = GetName(allText);
                    txtDOB.Text = GetDOB(allText);
                    txtAddress.Text = GetAddress(allText);

                    //txtOCRDebug.Text = allText;

                    pnlResults.Visible= true;

                    if(string.IsNullOrEmpty(frontText) &&  string.IsNullOrEmpty(backText))
                    {
                        lblMessage.Text = "No text was recognized. Check the image quality and OCR API response.";
                    }
                    else
                    {
                        lblMessage.Text = "OCR completed. Front text length: " + frontText.Length + 
                                          ", Back Text Length: " + backText.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                pnlResults.Visible=false;
                lblMessage.Text = "Error: " + Server.HtmlEncode(ex.Message);
            }
        }

        private async Task<string> ScanImage(
            HttpClient client, HttpPostedFile file, string apiKey)
        {
            byte[] imageBytes;

            using (Image original = Image.FromStream(file.InputStream))
            using (Bitmap bitmap = new Bitmap(original))
            {
                int width = bitmap.Width;
                int height = bitmap.Height;

                if (width > 2000 || height > 2000)
                {
                    double ratio = Math.Min(
                        2000.0 / width,
                        2000.0 / height);

                    width =Math.Max(1,(int)(width * ratio));
                    height =Math.Max(1,(int)(height * ratio));
                }

                using (Bitmap resized = new Bitmap(bitmap, width, height))
                using (MemoryStream memory = new MemoryStream())
                {
                    ImageCodecInfo encoder = ImageCodecInfo
                        .GetImageEncoders()
                        .FirstOrDefault(x => x.MimeType == "image/jpeg");

                    if(encoder == null)
                    {
                        throw new Exception("JPG image encoder was not found.");
                    }

                    using (EncoderParameters settings = new EncoderParameters(1))
                    {
                        settings.Param[0] = new EncoderParameter(
                        System.Drawing.Imaging.Encoder.Quality, 88L);

                        resized.Save(memory, encoder, settings);
                    }
                    
                    imageBytes = memory.ToArray();
                }
            }

            if (imageBytes.Length > 1400000)
            {
                throw new Exception("Image is too large. Please upload a smaller image.");
            }

            using (MultipartFormDataContent form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(apiKey), "apikey");
                form.Add(new StringContent("eng"), "language");
                form.Add(new StringContent("true"), "detectOrientation");
                form.Add(new StringContent("1"), "OCREngine");

                form.Add(new ByteArrayContent(imageBytes),"file","scan.jpg");

                HttpResponseMessage response = await client.PostAsync(
                    "https://api.ocr.space/parse/image",
                    form);

                string json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "OCR API Error " +
                        (int)response.StatusCode + ": " + json);
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();

                Dictionary<string, object> result = serializer.Deserialize<Dictionary<string, object>>(json);

                if (result == null)
                {
                    throw new Exception("OCR returned an invalid response.");
                }
                if (result.ContainsKey("IsErroredOnProcessing") &&
                    Convert.ToString(result["IsErroredOnProcessing"])
                    .Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    string errorMessage = result.ContainsKey("ErrorMessage") ? Convert.ToString(result["ErrorMessage"]) : "Unknown OCR processing error.";

                    throw new Exception("OCR processing failed: " + errorMessage);
                }

                if (result.ContainsKey("ParsedResults") && 
                    result["ParsedResults"] is System.Collections.ArrayList parsed &&
                    parsed.Count > 0 && 
                    parsed[0] is Dictionary<string, object> data)
                {
                    if(data.ContainsKey("ParsedText"))
                    {
                        return Convert.ToString(data["ParsedText"]) ?? "";
                    }
                    
                    throw new Exception("ParsedText was not found in the OCR response.");
                }

                throw new Exception("ParsedResults was not found or contained no results.");
            }
        }

        private string GetName(string text) 
        {
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines) 
            { 
                string value = line.Trim(); 
                if (value.StartsWith("Government Of India", StringComparison.OrdinalIgnoreCase)) 
                {
                    continue; 
                }
                if (System.Text.RegularExpressions.Regex.IsMatch(
            value,
            @"^[A-Za-z]+(?:[ ]+[A-Za-z]+){1,4}$"))

                {
                    if (value.IndexOf("Government",StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("India",StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    } 
                    return value;
                } 
            } 
            return ""; 
        }

        private string GetDOB(string text) 
        {
            string[] lines = text.Split(
                             new[] { '\r', '\n' },
                             StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines) 
            { 
                string value = line.Trim();

                bool hasDOBLabel = value.IndexOf("DOB", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   value.IndexOf("Date of Birth", StringComparison.OrdinalIgnoreCase) >= 0;

                if(hasDOBLabel) 
                { 
                    System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match
                                                                 (value, @"\b\d{2}[/-]\d{2}[/-]\d{4}\b");

                    if (match.Success)
                    {
                        return match.Value;
                    }
                } 
                if (value.IndexOf("Year of Birth", StringComparison.OrdinalIgnoreCase) >= 0) 
                { 
                    System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match
                                                                 (value, @"\b(?:19|20)\d{2}\b");

                    if (match.Success)
                    {
                        return match.Value;
                    }
                } 
            } 
            return ""; 
        }

        private string GetAddress(string text)
        {
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> addressLines = new List<string>();

            bool addressStarted = false;

            foreach (string line in lines)
            {
                string value = line.Trim();

                int addressIndex = value.IndexOf("Address", StringComparison.OrdinalIgnoreCase);
                if(addressIndex >= 0)
                {
                    addressStarted = true;

                    string afterLabel = value.Substring(addressIndex + 7).Trim(' ', ':', '-');

                    if (!string.IsNullOrEmpty(afterLabel))
                    {
                        addressLines.Add(afterLabel);
                    }
                    continue;
                }

                if (addressStarted)
                {
                    if (value.IndexOf("VID", StringComparison.OrdinalIgnoreCase) >= 0 || 
                        System.Text.RegularExpressions.Regex.IsMatch(value, @"\b\d{4}\s+\d{4}\s+\d{4}\b"))
                    {
                        break;
                    }

                    addressLines.Add(value);
                }
            }

            return string.Join(", ", addressLines);
        }
    }
}