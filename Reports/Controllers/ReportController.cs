using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reports.Models;
using Reports.ViewModel;

namespace Reports.Controllers
{
     public class ReportController : Controller
    {
        private readonly ReportsManagementDbContext _dbContext;

        public ReportController(ReportsManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult ReportDetail(Guid id)
        {
            var reports = _dbContext.TblReportDetails.Where(a => a.ReportId == id).Include(a => a.Report);
            var userReportsViewModels = reports.Select(u => new ReportDetailsViewModel
            {
                 ReportDetailId = u.ReportDetailId,
                 ReportName = u.Report.Name,
                 ReportId = u.ReportId,
                 DeliveryStatus  = u.DeliveryStatus,
                 CarrierName = u.CarrierName,
                 TrackingNumber = u.TrackingNumber,
                 ShippingAdress = u.ShippingAdress,
                 Price  = u.Price,

            }).ToList();

            return View(userReportsViewModels);
        }
        
        
        public List<string> GetReportsForUser(string UserName)
        {
            var reportNames = _dbContext.TblReports
                .Where(r => r.User.UserName == UserName)
                .Select(r => r.Name)
                .Distinct()
                .ToList();
            return reportNames;
        }

        public IActionResult SearchReport(string selectedUser, string selectedReport)
        {
            ViewBag.Users = _dbContext.TblApplicationUsers.Select(r => r.UserName).ToList();

            
            if (!string.IsNullOrEmpty(selectedUser)&& !string.IsNullOrEmpty(selectedReport))
            {
                var report = _dbContext.TblReports.Where(r => r.User.UserName == selectedUser && r.Name == selectedReport).FirstOrDefault();
                var details = _dbContext.TblReportDetails.Where(a=>a.ReportId == report.ReportId).Include(a => a.Report).Include(a => a.Report.User).OrderBy(r => r.ReportId).ThenBy(r => r.ReportDetailId);
                var userReportsViewModels = details.Select(u => new AllReportDetailsViewModel
                {
                    ReportDetailId = u.ReportDetailId,
                    ReportName = u.Report.Name,
                    UserName = u.Report.User.UserName,
                    ReportId = u.ReportId,
                    DeliveryStatus = u.DeliveryStatus,
                    CarrierName = u.CarrierName,
                    TrackingNumber = u.TrackingNumber,
                    ShippingAdress = u.ShippingAdress,
                    Price = u.Price,
                }).ToList();
                ViewBag.SelectedUser = selectedUser;
                ViewBag.SelectedReport = selectedReport;
                return View(userReportsViewModels);
            }
            if (!string.IsNullOrEmpty(selectedUser) && string.IsNullOrEmpty(selectedReport))
            {
                var details = _dbContext.TblReportDetails.Where(a=>a.Report.User.UserName == selectedUser).Include(a => a.Report).Include(a => a.Report.User).OrderBy(r => r.ReportId).ThenBy(r => r.ReportDetailId);
                var userReportsViewModels = details.Select(u => new AllReportDetailsViewModel
                {
                    ReportDetailId = u.ReportDetailId,
                    ReportName = u.Report.Name,
                    UserName = u.Report.User.UserName,
                    ReportId = u.ReportId,
                    DeliveryStatus = u.DeliveryStatus,
                    CarrierName = u.CarrierName,
                    TrackingNumber = u.TrackingNumber,
                    ShippingAdress = u.ShippingAdress,
                    Price = u.Price,

                }).ToList();
                ViewBag.SelectedUser = selectedUser;
                ViewBag.SelectedReport = selectedReport;
                return View(userReportsViewModels);
            }
            if (string.IsNullOrEmpty(selectedUser) && string.IsNullOrEmpty(selectedReport))
            {
                var reports = _dbContext.TblReportDetails.Include(a => a.Report).Include(a => a.Report.User).OrderBy(r => r.ReportId).ThenBy(r => r.ReportDetailId);
                var userReportsViewModels = reports.Select(u => new AllReportDetailsViewModel
                {
                    ReportDetailId = u.ReportDetailId,
                    ReportName = u.Report.Name,
                    UserName = u.Report.User.UserName,
                    ReportId = u.ReportId,
                    DeliveryStatus = u.DeliveryStatus,
                    CarrierName = u.CarrierName,
                    TrackingNumber = u.TrackingNumber,
                    ShippingAdress = u.ShippingAdress,
                    Price = u.Price,

                }).ToList();
                ViewBag.Users = _dbContext.TblApplicationUsers.Select(r => r.UserName).ToList();


                return View(userReportsViewModels);
            }

            return View();
        }



    }
}
