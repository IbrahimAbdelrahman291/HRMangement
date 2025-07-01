using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class QuarterlyEvaluation : BaseEntity
    {
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public string Quarter { get; set; }  // Q1 2025 مثلاً
        public string EvaluatedBy { get; set; }

        public ICollection<EvaluationResult> EvaluationResults { get; set; }
    }
}
