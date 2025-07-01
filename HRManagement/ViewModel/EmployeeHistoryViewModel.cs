namespace HRManagement.ViewModel
{
    public class EmployeeHistoryViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; }

        public DateTime HiringDate { get; set; }

        public string Qualification { get; set; }

        public int GraduationYear { get; set; }

        public DateTime? EndOfServiceDate { get; set; }

        public string? EndOfServiceReason { get; set; }
    }
}
