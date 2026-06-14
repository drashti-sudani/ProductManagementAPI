using AutoMapper;
using Domain;
using Application.DTOs;

namespace Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductResponseDto>().ReverseMap();
        CreateMap<ProductCreateDto, Product>();
        CreateMap<Item, ItemResponseDto>();
    }
}
