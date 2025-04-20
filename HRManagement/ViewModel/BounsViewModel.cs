namespace HRManagement.ViewModel
{
    public class BounsViewModel
    {
        public int Id { get; set; }
        public int MonthlyEmployeeDataId { get; set; }
        public double Amount { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public int month { get; set; }
        public int year { get; set; }
        public DateTime DateOfBouns { get; set; }
    }
}
