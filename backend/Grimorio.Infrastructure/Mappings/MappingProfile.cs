using AutoMapper;
using Grimorio.Application.DTOs;
using Grimorio.Domain.Entities.Organization;

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
            .ForMember(dest => dest.PositionName, opt => opt.Ignore()); // Se asigna manualmente en handlers

        // Position mappings
        CreateMap<Position, PositionDto>();

        // Branch mappings
        CreateMap<Branch, BranchDto>();
    }
}
