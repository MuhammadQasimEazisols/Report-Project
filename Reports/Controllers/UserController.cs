using Microsoft.AspNetCore.Mvc;
using Reports.Models;
using Reports.ViewModel;

namespace Reports.Controllers
{
    public class UserController : Controller
    {
        private readonly ReportsManagementDbContext _dbContext;

        public UserController(ReportsManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult UserList()
        {
            var users = _dbContext.TblApplicationUsers.ToList();
            var userViewModels = users.Select(u => new UserViewModel
            {
                UserId = u.UserId,
                UserName = u.UserName,
                Email = u.Email
            }).ToList();

            return View(userViewModels);
        }
        
        public IActionResult UserDetail(Guid id)
        {
            var reports = _dbContext.TblReports.Where(a=>a.UserId == id);
            var userReportsViewModels = reports.Select(u => new UserReportsViewModel
            {
                 ReportId= u.ReportId,
                 Name = u.Name,
                 CreatedDate = u.CreatedDate,
                 UserName = u.User.UserName,

            }).OrderByDescending(a=>a.CreatedDate).ToList();

            return View(userReportsViewModels);
        }
    }
}