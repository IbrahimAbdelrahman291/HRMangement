using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class EvaluationResult : BaseEntity
    {
        public int QuarterlyEvaluationId { get; set; }
        public QuarterlyEvaluation QuarterlyEvaluation { get; set; }

        public int EvaluationCriteriaId { get; set; }
        public EvaluationCriteria EvaluationCriteria { get; set; }

        public string? Rating { get; set; }
    }
}
