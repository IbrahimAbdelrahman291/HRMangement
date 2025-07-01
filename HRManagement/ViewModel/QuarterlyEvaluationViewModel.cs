namespace HRManagement.ViewModel
{
    public class QuarterlyEvaluationViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }

        public string Quarter { get; set; }           // مثال: Q2 2025
        public string EvaluatedBy { get; set; }

        public List<EvaluationResultViewModel> EvaluationResults { get; set; } = new List<EvaluationResultViewModel>();
    }
}
