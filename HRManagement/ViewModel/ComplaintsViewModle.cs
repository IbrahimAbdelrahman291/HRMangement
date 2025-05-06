using DAL.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRManagement.ViewModel
{
    public class ComplaintsViewModle
    {
        public int Id { get; set; }
        public string content { get; set; }
        public string status { get; set; }
        public string? response { get; set; }
        public DateTime Date { get; set; }
        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
    }
}
