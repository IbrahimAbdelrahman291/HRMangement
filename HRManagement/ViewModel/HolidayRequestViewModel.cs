namespace HRManagement.ViewModel
{
    public class HolidayRequestViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }  
        public DateTime HolidayDate { get; set; }
        public string? Status { get; set; }
        public string ReasonOfHoliday { get; set; }
        public string? ReasonOfRejection { get; set; }
    }
}
