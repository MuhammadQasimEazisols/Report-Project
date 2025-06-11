using Microsoft.AspNetCore.Mvc;
using Reports.Models;
using Reports.ViewModel;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;

namespace Reports.Controllers
{
    public class HomeController : Controller
    {
        private readonly ReportsManagementDbContext _dbContext;

        public HomeController(ReportsManagementDbContext dbContext)
        {
            _dbContext = dbContext;

        }
        public IActionResult Index()
        {
            var reports = _dbContext.TblReports.Include(a=>a.User).ToList();
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
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            return RedirectToAction("Index");
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
            foreach(var file in fileInput)
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
    }
} 