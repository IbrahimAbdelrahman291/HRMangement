using BLL.Interfaces;
using BLL.Specification;
using DAL.Context;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly HRDbContext _dbContext;

        public GenericRepository(HRDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task AddAsync(T Item)
        {
            await _dbContext.AddAsync(Item);
        }

        public async Task<int> CompleteAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public void Delete(T Item)
        {
            _dbContext.Remove(Item);
        }

        public void Update(T Item)
        {
            _dbContext.Update(Item);
        }
        public async Task<IEnumerable<T>> GetAllWithSpecAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).ToListAsync();
        }

        public async Task<T> GetByIdWithSpecAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }


        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            return SpecificationElvator<T>.GetQuery(_dbContext.Set<T>(), spec);
        }
    }
}
