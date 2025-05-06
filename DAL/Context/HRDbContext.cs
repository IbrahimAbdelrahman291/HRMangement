using DAL.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Context
{
    public class HRDbContext : IdentityDbContext<User>
    {
        public HRDbContext(DbContextOptions<HRDbContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<WorkLogs>()
            .HasOne(w => w.Employee)
            .WithMany(e => e.workLogs)
            .HasForeignKey(w => w.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
            base.OnModelCreating(builder);

            var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            v => v.ToDateTime(TimeOnly.MinValue),
            v => DateOnly.FromDateTime(v));

            builder.Entity<WorkLogs>()
            .Property(w => w.Day)
            .HasConversion(dateOnlyConverter);

            var timeOnlyConverter = new ValueConverter<TimeOnly, TimeSpan>(
            v => v.ToTimeSpan(),
            v => TimeOnly.FromTimeSpan(v));

            builder.Entity<WorkLogs>()
            .Property(w => w.End)
            .HasConversion(timeOnlyConverter);
            builder.Entity<WorkLogs>()
            .Property(w => w.Start)
            .HasConversion(timeOnlyConverter);

            builder.Entity<MonthlyEmployeeData>()
                .HasOne(m => m.Employee)
                .WithMany(e => e.MonthlyData)
                .HasForeignKey(m => m.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
        public DbSet<WorkLogs> WorkLogs { get; set; }
        public DbSet<Discounts> Discounts { get; set; }
        public DbSet<Borrow> Borrows { get; set; }
        public DbSet<Bouns> Bounss { get; set; }
        public DbSet<HolidayRequests> HolidayRequests { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<MonthlyEmployeeData> MonthlyEmployeeData { get; set; }
        public DbSet<ResignationRequests> ResignationRequests { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RequestForForgetCloseShift> requests { get; set; }
        public DbSet<RequestBorrow> requestBorrows { get; set; }
        public DbSet<Complaints> Complaints { get; set; }
    }
}
