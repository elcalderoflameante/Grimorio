using AutoMapper;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Domain.Entities.Payroll;

namespace Grimorio.Infrastructure.Mappings;

/// <summary>
/// Configuración de AutoMapper para mapeos de DTOs y Entities.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Employee mappings
        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(e => e.UserAccount == null ? (Guid?)null : e.UserAccount.Id))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(e => e.UserAccount == null ? null : e.UserAccount.Email))
            .ForMember(dest => dest.UserIsActive, opt => opt.MapFrom(e => e.UserAccount == null ? (bool?)null : e.UserAccount.IsActive))
              // Removed explicit mapping for BranchId since property names now match
            .ForMember(dest => dest.PositionName, opt => opt.Ignore()); // Se asigna manualmente en handlers

        // Position mappings
        CreateMap<Position, PositionDto>();

        // Branch mappings
        CreateMap<Branch, BranchDto>();

        // Payroll mappings
        CreateMap<PayrollConfiguration, PayrollConfigurationDto>();
        CreateMap<PayrollAdvance, PayrollAdvanceDto>();
        CreateMap<EmployeeConsumption, EmployeeConsumptionDto>();
        CreateMap<PayrollAdjustment, PayrollAdjustmentDto>();
        CreateMap<PayrollRoleHeader, PayrollRoleDto>();
        CreateMap<PayrollRoleDetail, PayrollRoleDetailDto>();
    }
}
