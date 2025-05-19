// Create a new class (e.g., MappingProfile.cs)
using AutoMapper;
using CampusBites.Application.DTOs;
using CampusBites.Domain.Entities;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<MenuItem, MenuItemDto>(); // Add other mappings as needed
    }
}