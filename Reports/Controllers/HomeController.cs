using Microsoft.AspNetCore.Mvc;
using Reports.Models;
using Reports.ViewModel;
using ClosedXML.Excel;

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
            return View();
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

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }

        [HttpPost]
        public IActionResult UploadFile(IFormFile fileInput)
        {
            if (fileInput == null || fileInput.Length == 0)
            {
                TempData["Error"] = "No file selected or file is empty.";
                return RedirectToAction("Index");
            }
            List<string> reportData = new List<string>();
            ExcelFileViewModel viewModel = new ExcelFileViewModel();
            try
            {
                using (var stream = new MemoryStream())
                {
                    fileInput.CopyTo(stream);
                    stream.Position = 0; 

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1).ToList();

                        if (!rows.Any())
                        {
                            TempData["Error"] = "No data inside file";
                            return RedirectToAction("Index");
                        }
                        var fileName = Path.GetFileName(fileInput.FileName);
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
                TempData["Success"] = "File uploaded successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to read the Excel file.";
                return RedirectToAction("Index");
            }
        }
    }
} 