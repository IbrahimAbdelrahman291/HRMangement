using BLL.Specification;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Repositories
{
    public static class SpecificationElvator<T> where T : BaseEntity
    {
        public static IQueryable<T> GetQuery(IQueryable<T> InputQuery, ISpecification<T> Spec)
        {
            var Query = InputQuery;

            if (Spec.Criteria is not null)
            {
                Query = Query.Where(Spec.Criteria);
            }

            if (Spec.FilterExpressions is not null && Spec.FilterExpressions.Any())
            {
                foreach (var filter in Spec.FilterExpressions)
                {
                    Query = Query.Where(filter);
                }
            }

            Query = Spec.Includes.Aggregate(Query, (CurrQuery, InculdeExpression) => CurrQuery.Include(InculdeExpression));

            if (Spec.ThenIncludes is not null)
            {
                Query = Spec.ThenIncludes.Aggregate(Query, (current, include) => include(current));
            }

            return Query;
        }
    }
}
