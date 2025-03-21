using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Discounts : BaseEntity
    {
        [ForeignKey(nameof(MonthlyEmployeeData))]
        public int MonthlyEmployeeDataId { get; set; }
        public MonthlyEmployeeData MonthlyEmployeeData { get; set; }
        public decimal Amount { get; set; }
        public string ReasonOfDiscount { get; set; }
        public string? Notes { get; set; }
        public DateTime Date { get; set; }
    }
}
