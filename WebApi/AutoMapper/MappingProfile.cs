using AutoMapper;
using Core.Entities.Items;
using Core.Entities.Ventas;
using WebApi.DTOs;

namespace WebApi.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.DocumentLines, opt => opt.MapFrom(src => src.DocumentLines));
            CreateMap<DocumentLineOrder, DocumentLineOrderDto>()
                .ForMember(dest => dest.LineTaxJurisdictions, opt => opt.MapFrom(src => src.LineTaxJurisdictions));
            CreateMap<LineTaxJurisdiction, LineTaxJurisdictionDto>();

            CreateMap<Item, ItemDto>();
            CreateMap<ItemPrice, ItemPriceDto>();
            CreateMap<ItemWarehouseInfoCollection, ItemWarehouseInfoCollectionDto>();

            // Configuración adicional si es necesario
            // Ejemplo: Mapear propiedades con nombres diferentes
            // CreateMap<Order, OrderDto>().ForMember(dest => dest.PropiedadDestino, opt => opt.MapFrom(src => src.PropiedadOrigen));
        }
    }
}
