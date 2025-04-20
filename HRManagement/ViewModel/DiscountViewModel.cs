namespace HRManagement.ViewModel
{
    public class DiscountViewModel
    {
        public int Id { get; set; }
        public int MonthlyEmployeeDataId { get; set; }
        public double Amount { get; set; }
        public string ReasonOfDiscount { get; set; }
        public string? Notes { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime Date { get; set; }
    }
}

