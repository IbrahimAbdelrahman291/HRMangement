using DAL.Models;

namespace HRManagement.ViewModel
{
    public class RequestForForgetCloseShiftViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime RequestDate { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public string? ReasonOfRejection { get; set; }
        public string EmployeeName { get; set; }
    }
}
