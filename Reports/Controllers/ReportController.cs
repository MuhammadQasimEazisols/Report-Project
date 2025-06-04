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
    }
}
