using DAL.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRManagement.ViewModel
{
    public class WorkLogsViewModel
    {
        public int Id { get; set; }
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
        public TimeSpan TotalTime { get; set; }
        public DateOnly Day { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
    }
}
