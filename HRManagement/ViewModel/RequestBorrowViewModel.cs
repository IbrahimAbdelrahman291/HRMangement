using DAL.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRManagement.ViewModel
{
    public class RequestBorrowViewModel
    {
        public int Id { get; set; }
        public int Amount { get; set; }
        public DateTime RequestDate { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? notes { get; set; }
        public string? ReasonOfRejection { get; set; }
        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
    }
}
