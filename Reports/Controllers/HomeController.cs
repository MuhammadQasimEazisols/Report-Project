using Microsoft.AspNetCore.Mvc;
using Reports.Models;
using Reports.ViewModel;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using System.Net;

namespace Reports.Controllers
{
    public class HomeController : Controller
    {
        private readonly ReportsManagementDbContext _dbContext;
        private readonly EmailSettings _emailSettings;
        public HomeController(ReportsManagementDbContext dbContext, IOptions<EmailSettings> emailSettings)
        {
            _dbContext = dbContext;
            _emailSettings = emailSettings.Value;
        }
        public IActionResult Index()
        {
            var reports = _dbContext.TblReports.Include(a => a.User).ToList();
            var userReportsViewModels = reports.Select(u => new UserReportsViewModel
            {
                ReportId = u.ReportId,
                Name = u.Name,
                CreatedDate = u.CreatedDate,
                UserName = u.User.UserName,

            }).OrderByDescending(a => a.CreatedDate).ToList();

            return View(userReportsViewModels);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _dbContext.TblApplicationUsers.FirstOrDefault(a => a.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }
            if (user.Password != password)
            {
                ViewBag.Error = "Invalid password";
                return View();
            }
            if (user.EmailVerified == false)
            {
                ViewBag.Error = "Email is not verified. Please check your email for verfication link";
                return View();
            }
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            return RedirectToAction("Index");
        }
        public IActionResult SignUp()
        {
            return View();
        }
        [HttpPost]
        public IActionResult SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _dbContext.TblApplicationUsers.FirstOrDefault(a => a.Email.ToLower() == model.Email.ToLower());
            if (user != null)
            {
                ViewBag.Error = "Email already exists";
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.Error = "Password and Confirm Password did not match";
                return View(model);
            }

            // Generate a token
            string verificationToken = Guid.NewGuid().ToString();

            // Create user
            TblApplicationUser newUser = new TblApplicationUser
            {
                UserName = model.UserName,
                Password = model.Password,
                Email = model.Email,
                UserId = Guid.NewGuid(),
                EmailVerified = false,
                VerificationToken = verificationToken
            };

            // Send verification email
            SendVerificationEmail(newUser.Email, verificationToken);
            _dbContext.TblApplicationUsers.Add(newUser);
            _dbContext.SaveChanges();

            TempData["Success"] = "Verification email has been sent. Please check your inbox to activate your account.";
            return RedirectToAction("Login");
        }

        public void SendVerificationEmail(string toEmail, string token)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.Name, _emailSettings.FromEmailId));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Verify Your Email";

            var verificationLink = $"http://localhost:5284/Home/VerifyEmail?token={token}";

            message.Body = new TextPart("html")
            {
                Text = $"<h3>Email Verification</h3>" +
                       $"<p>Please click the link below to verify your email address:</p>" +
                       $"<a href='{verificationLink}'>Verify Email</a>"
            };
            // Trust all SSL certificates (INSECURE - for testing only!)
            ServicePointManager.ServerCertificateValidationCallback = (s, c, h, e) => true;

            using (var client = new SmtpClient())
            {
                client.Connect(_emailSettings.Host, _emailSettings.Port, _emailSettings.UseSSL);
                client.Authenticate(_emailSettings.FromEmailId, _emailSettings.Password);
                client.Send(message);
                client.Disconnect(true);
            }
        }

        public IActionResult VerifyEmail(string token)
        {
            var user = _dbContext.TblApplicationUsers.FirstOrDefault(u => u.VerificationToken == token);

            if (user == null)
            {
                ViewBag.Error = "Invalid or expired verification link.";
                return View("Error");
            }

            user.EmailVerified = true;
            user.VerificationToken = null; // Optional: clear token
            _dbContext.SaveChanges();

            ViewBag.Message = "Email verified successfully!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }

        [HttpPost]
        public IActionResult UploadFile(List<IFormFile> fileInput)
        {
            if (fileInput == null || fileInput.Count == 0)
            {
                TempData["Error"] = "No file selected or file is empty.";
                return RedirectToAction("Index");
            }

            List<string> reportData = new List<string>();
            foreach (var file in fileInput)
            {
                ExcelFileViewModel viewModel = new ExcelFileViewModel();
                try
                {
                    using (var stream = new MemoryStream())
                    {
                        file.CopyTo(stream);
                        stream.Position = 0;

                        using (var workbook = new XLWorkbook(stream))
                        {
                            var worksheet = workbook.Worksheet(1);
                            var expectedHeaders = new List<string> { "TrackingNumber", "CarrierName", "Price", "ShippingAdress", "DeliveryStatus" };
                            var headerRow = worksheet.Row(1);
                            var actualHeaders = headerRow.Cells(1, expectedHeaders.Count)
                                                         .Select(c => c.Value.ToString().Trim())
                                                         .ToList();

                            bool headersValid = expectedHeaders.SequenceEqual(actualHeaders, StringComparer.OrdinalIgnoreCase);
                            if (!headersValid)
                            {
                                TempData["Error"] = "Excel file format is incorrect. Please ensure the column headers are: TrackingNumber, CarrierName, Price, ShippingAdress, DeliveryStatus. File Name=" + file.FileName;
                                return RedirectToAction("Index");
                            }

                            var rows = worksheet.RangeUsed().RowsUsed().Skip(1).ToList();

                            if (!rows.Any())
                            {
                                TempData["Error"] = "No data inside file.  File Name=" + file.FileName;
                                return RedirectToAction("Index");
                            }

                            var fileName = Path.GetFileName(file.FileName);
                            var userId = HttpContext.Session.GetString("UserId");

                            TblReport tblReport = new()
                            {
                                ReportId = Guid.NewGuid(),
                                CreatedDate = DateTime.Now,
                                Name = fileName,
                                UserId = Guid.Parse(userId)
                            };
                            _dbContext.TblReports.Add(tblReport);

                            foreach (var row in rows)
                            {
                                viewModel.TrackingNumber = row.Cell(1).Value.ToString();
                                viewModel.CarrierName = row.Cell(2).Value.ToString();
                                viewModel.Price = (int)row.Cell(3).Value;
                                viewModel.ShippingAdress = row.Cell(4).Value.ToString();
                                viewModel.DeliveryStatus = row.Cell(5).Value.ToString();

                                TblReportDetail newReportDetails = new()
                                {
                                    ReportDetailId = Guid.NewGuid(),
                                    ReportId = tblReport.ReportId,
                                    TrackingNumber = viewModel.TrackingNumber,
                                    ShippingAdress = viewModel.ShippingAdress,
                                    Price = viewModel.Price,
                                    CarrierName = viewModel.CarrierName,
                                    DeliveryStatus = viewModel.DeliveryStatus
                                };
                                _dbContext.TblReportDetails.Add(newReportDetails);
                            }
                            _dbContext.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Failed to read the Excel file. File Name=" + file.FileName;
                    return RedirectToAction("Index");
                }
            }

            TempData["Success"] = "All files uploaded successfully.";
            return RedirectToAction("Index");
        }

        public IActionResult ForgetPassword()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ForgetPassword(string Email)
        {
            var user = _dbContext.TblApplicationUsers.FirstOrDefault(a => a.Email.ToLower() == Email.ToLower());
            if(user == null)
            {
                ViewBag.Error = "Email not found. Please check your email address.";
                return View();
            }
            string verificationToken = Guid.NewGuid().ToString();
            SendVerificationEmailForForgetPassword(Email, verificationToken);
            user.VerificationToken = verificationToken;
            _dbContext.TblApplicationUsers.Update(user);
            _dbContext.SaveChanges();
            TempData["Success"] = "Verification email has been sent. Please check your inbox to forget password.";
            return RedirectToAction("Login");
        }

        public void SendVerificationEmailForForgetPassword(string toEmail, string token)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.Name, _emailSettings.FromEmailId));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Verify Your Email";

            var verificationLink = $"http://localhost:5284/Home/VerifyEmail?token={token}";

            message.Body = new TextPart("html")
            {
                Text = $"<h3>Email Verification</h3>" +
                       $"<p>Please click the link below to verify your email address:</p>" +
                       $"<a href='{verificationLink}'>Verify Email</a>"
            };
            // Trust all SSL certificates (INSECURE - for testing only!)
            ServicePointManager.ServerCertificateValidationCallback = (s, c, h, e) => true;

            using (var client = new SmtpClient())
            {
                client.Connect(_emailSettings.Host, _emailSettings.Port, _emailSettings.UseSSL);
                client.Authenticate(_emailSettings.FromEmailId, _emailSettings.Password);
                client.Send(message);
                client.Disconnect(true);
            }
        }

        public IActionResult VerifyForgetPassword(string token)
        {
            var user = _dbContext.TblApplicationUsers.FirstOrDefault(u => u.VerificationToken == token);

            if (user == null)
            {
                ViewBag.Error = "Invalid or expired verification link.";
                return View("Error");
            }
            user.VerificationToken = null; // Optional: clear token
            _dbContext.SaveChanges();

            ViewBag.Message = "Email verified successfully!";
            return View();
        }
    }
}