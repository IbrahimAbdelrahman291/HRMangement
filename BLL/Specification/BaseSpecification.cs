using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Specification
{
    public class BaseSpecification<T> : ISpecification<T> where T : BaseEntity
    {
        public Expression<Func<T, bool>> Criteria { get; set; }
        public List<Expression<Func<T, object>>> Includes { get; set; } = new List<Expression<Func<T, object>>>();
        public List<Func<IQueryable<T>, IIncludableQueryable<T, object>>> ThenIncludes { get; set; } = new List<Func<IQueryable<T>, IIncludableQueryable<T, object>>>();
        public List<Expression<Func<T, bool>>> FilterExpressions { get; set; } = new List<Expression<Func<T, bool>>>();

        public BaseSpecification() { }

        public BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        public BaseSpecification(Expression<Func<T, bool>> criteria, List<Expression<Func<T, bool>>> filters)
        {
            Criteria = criteria;
            FilterExpressions = filters ?? new List<Expression<Func<T, bool>>>();
        }

        public void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        public void AddThenInclude<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> thenIncludeExpression)
        {
            ThenIncludes.Add(source => source.Include(thenIncludeExpression));
        }

        public void AddFilter(Expression<Func<T, bool>> filterExpression)
        {
            FilterExpressions.Add(filterExpression);
        }
    }
}
