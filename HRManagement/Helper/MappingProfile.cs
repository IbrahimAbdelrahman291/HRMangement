using AutoMapper;
using DAL.Models;
using HRManagement.ViewModel;

namespace HRManagement.Helper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Employee, EmployeeViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.BranchName))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => src.BankName))
                .ForMember(dest => dest.BankAccount, opt => opt.MapFrom(src => src.BankAccount))
                .ForMember(dest => dest.Target, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().Target))
                .ForMember(dest => dest.TotalBonuss, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().TotalBouns))
                .ForMember(dest => dest.TotalBorrows, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().TotalBorrows))
                .ForMember(dest => dest.TotalDiscounts, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().TotalDiscounts))
                .ForMember(dest => dest.MonthlyEmployeeDataId, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().Id))
                .ForMember(dest => dest.Month, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().Month))
                .ForMember(dest => dest.Year, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().Year))
                .ForMember(dest => dest.Hours, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().Hours))
                .ForMember(dest => dest.HoursOverTime, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().HoursOverTime))
                .ForMember(dest => dest.ForgetedHours, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().ForgetedHours))
                .ForMember(dest => dest.SalaryPerHour, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().SalaryPerHour))
                .ForMember(dest => dest.TotalSalary, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().TotalSalary))
                .ForMember(dest => dest.Holidaies, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().Holidaies))
                .ForMember(dest => dest.NetSalary, opt => opt.MapFrom(src => src.MonthlyData.FirstOrDefault().NetSalary));

            // Reverse Mapping
            CreateMap<EmployeeViewModel, Employee>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.BranchName))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => src.BankName))
                .ForMember(dest => dest.BankAccount, opt => opt.MapFrom(src => src.BankAccount))
                .ForMember(dest => dest.MonthlyData, opt => opt.Ignore())
                .ForMember(dest => dest.workLogs, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore());


            // Discounts Mapping
            CreateMap<Discounts, DiscountViewModel>().ReverseMap();

            // Bouns Mapping
            CreateMap<Bouns, BounsViewModel>().ReverseMap();

            // Borrows Mapping
            CreateMap<Borrow, BorrowViewModel>().ReverseMap();
            // Holiday Requests Mapping
            CreateMap<HolidayRequests, HolidayRequestViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.EmployeeId))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.Name))
                .ForMember(dest => dest.HolidayDate, opt => opt.MapFrom(src => src.HolidayDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.status))
                .ForMember(dest => dest.ReasonOfHoliday, opt => opt.MapFrom(src => src.ReasonOfHoliday))
                .ForMember(dest => dest.ReasonOfRejection, opt => opt.MapFrom(src => src.ReasonOfRejection));

            CreateMap<HolidayRequestViewModel, HolidayRequests>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.EmployeeId))
                .ForMember(dest => dest.HolidayDate, opt => opt.MapFrom(src => src.HolidayDate))
                .ForMember(dest => dest.status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.ReasonOfHoliday, opt => opt.MapFrom(src => src.ReasonOfHoliday))
                .ForMember(dest => dest.ReasonOfRejection, opt => opt.MapFrom(src => src.ReasonOfRejection));
            // Resignation Requests Mapping
            CreateMap<ResignationRequests, ResignationRequestsViewModel>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.Name));

            CreateMap<ResignationRequestsViewModel, ResignationRequests>()
                .ForMember(dest => dest.Employee, opt => opt.Ignore());
            // WorkLogs Mapping
            CreateMap<WorkLogs, WorkLogsViewModel>()
            .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.Name));

            CreateMap<WorkLogsViewModel, WorkLogs>();

            CreateMap<RequestForForgetCloseShift, RequestForForgetCloseShiftViewModel>()
                .ForMember(dest => dest.EmployeeName , opt => opt.MapFrom(src => src.employee.Name));
            CreateMap<RequestForForgetCloseShiftViewModel, RequestForForgetCloseShift>().ForMember(dest => dest.employee, opt => opt.Ignore());

            CreateMap<RequestBorrow, RequestBorrowViewModel>().ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.Name));
            CreateMap<RequestBorrowViewModel, RequestBorrow>().ForMember(dest => dest.Employee , opt => opt.Ignore());

            CreateMap<Complaints, ComplaintsViewModle>().ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.Name));
            CreateMap<ComplaintsViewModle, Complaints>().ForMember(dest => dest.Employee , opt => opt.Ignore());
        }
    }
}
