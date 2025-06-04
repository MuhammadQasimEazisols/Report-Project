using System;
using System.Collections.Generic;

namespace Reports.Models;

public partial class TblReportDetail
{
    public Guid ReportDetailId { get; set; }
    public Guid ReportId { get; set; }
    public string DeliveryStatus { get; set; }
    public string CarrierName { get; set; }
    public string TrackingNumber { get; set; }
    public string ShippingAdress { get; set; }
    public int Price { get; set; }
    public virtual TblReport Report { get; set; } = null!;
}
