using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Bouns : BaseEntity
    {
        [ForeignKey(nameof(MonthlyEmployeeData))]
        public int MonthlyEmployeeDataId { get; set; }
        public MonthlyEmployeeData MonthlyEmployeeData { get; set; }
        public double Amount { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public int year { get; set; }
        public int month { get; set; }
        public DateTime DateOfBouns { get; set; }
    }
}
