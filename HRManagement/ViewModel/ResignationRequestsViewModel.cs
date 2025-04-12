namespace HRManagement.ViewModel
{
    public class ResignationRequestsViewModel
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime ResignationDate { get; set; }
        public string? Status { get; set; }
        public string ReasonOfResignation { get; set; }
        public string? ReasonOfRejection { get; set; }
    }
}
