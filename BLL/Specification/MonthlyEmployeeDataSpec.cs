using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Specification
{
    public class MonthlyEmployeeDataSpec : BaseSpecification<MonthlyEmployeeData>
    {
        public MonthlyEmployeeDataSpec(): base()
        {
            
        }
        public MonthlyEmployeeDataSpec(int id) : base(M => M.Id == id)
        {
            
        }
    }
}
