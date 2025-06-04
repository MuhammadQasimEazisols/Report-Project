using System;
using System.Collections.Generic;

namespace Reports.Models;

public partial class TblApplicationUser
{
    public Guid UserId { get; set; }

    public string? UserName { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public virtual ICollection<TblReport> TblReports { get; set; } = new List<TblReport>();
}
