namespace Reports.ViewModel
{
    public class ReportDetailsViewModel
    {
        public Guid ReportDetailId { get; set; }
        public Guid ReportId { get; set; }
        public string ReportName { get; set; }
        public string DeliveryStatus { get; set; }
        public string CarrierName { get; set; }
        public string TrackingNumber { get; set; }
        public string ShippingAdress { get; set; }
        public int Price { get; set; }
    }
}
