using System;
using System.Collections.Generic;

namespace Reports.Models;

public partial class TblReport
{
    public Guid ReportId { get; set; }

    public string? Name { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? UserId { get; set; }

    public virtual ICollection<TblReportDetail> TblReportDetails { get; set; } = new List<TblReportDetail>();

    public virtual TblApplicationUser? User { get; set; }
}
