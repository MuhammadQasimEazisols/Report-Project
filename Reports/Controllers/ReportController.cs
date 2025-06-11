using System.Linq;
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
                DeliveryStatus = u.DeliveryStatus,
                CarrierName = u.CarrierName,
                TrackingNumber = u.TrackingNumber,
                ShippingAdress = u.ShippingAdress,
                Price = u.Price,

            }).ToList();

            return View(userReportsViewModels);
        }


        public List<ReportListViewModel> GetReportsForUser(Guid userId)
        {
            ViewBag.ReportsList= _dbContext.TblReports
                .Where(r => r.User.UserId == userId)
                .Select(r => new ReportListViewModel { ReportId = r.ReportId, Name = r.Name })
                .Distinct()
                .ToList();
            return ViewBag.ReportsList;
        }

        public void SelectedReport(string selectedReport)
        {
            ViewBag.SelectedReport = selectedReport;
        }

        [HttpGet]
        public IActionResult SearchReport(Guid? selectedUser, Guid? selectedReport, int pageNumber, int pageSize, string searchKey)
        {
            if (pageNumber == 0) pageNumber = 1;
            if (pageSize == 0) pageSize = 10;

            ViewBag.Users = _dbContext.TblApplicationUsers
                    .Select(r => new { r.UserName, r.UserId })
                    .ToList();

            var report = _dbContext.TblReports;
            var details = _dbContext.TblReportDetails.AsQueryable();
            if (selectedUser != null && selectedReport != null)
            {
                var reportId = report.Where(r => r.User.UserId == selectedUser && r.ReportId == selectedReport).Select(a => a.ReportId).FirstOrDefault();
                var reportName = report.Where(r => r.User.UserId == selectedUser && r.ReportId == selectedReport).Select(a => a.Name).FirstOrDefault();
                ViewBag.SelectedReport = selectedReport.Value;
                details = details.Where(a => a.ReportId == reportId);
            }
            else if (selectedUser != null && selectedReport == null)
            {
                details = details.Where(a => a.Report.User.UserId == selectedUser);
            }
            else if (selectedUser == null && selectedReport == null)
            {
                details = details.Include(a => a.Report).Include(a => a.Report.User).OrderBy(r => r.ReportId).ThenBy(r => r.ReportDetailId);
            }
            if (searchKey != null)
            {
                searchKey = searchKey.ToLower();
                details = details.Where(a => a.Report.Name.ToLower().Contains(searchKey) || a.Report.User.UserName.ToLower().Contains(searchKey)|| a.DeliveryStatus.ToLower().Contains(searchKey) || a.ShippingAdress.ToLower().Contains(searchKey) || a.TrackingNumber.ToLower().Contains(searchKey)).OrderBy(r => r.ReportId).ThenBy(r => r.ReportDetailId).Include(a => a.Report).Include(a => a.Report.User);
            }
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
            }).ToList().Skip((pageNumber - 1) * pageSize).Take(pageSize);
            
            var totalRecords = details.ToList().Count();

            ViewBag.Users = _dbContext.TblApplicationUsers
                    .Select(r => new { r.UserName, r.UserId })
                    .ToList();
            ViewBag.SelectedUser = selectedUser;
            ViewBag.SelectedReport = selectedReport;
            if(totalRecords%pageSize == 0)
            {
                ViewBag.TotalPages = (totalRecords / pageSize);
            }
            else
            {
                ViewBag.TotalPages = (totalRecords / pageSize)+1;
            }
            ViewBag.CurrentPage = pageNumber;
            ViewBag.CurrentSelectedSize = pageSize;
            return View(userReportsViewModels);
        }
    }
}
