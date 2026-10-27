using AutoMapper;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;

namespace Mango.Services.OrderAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var loggerFactory = LoggerFactory.Create(builder => {
                builder.AddConsole();
            });

            var mappingConfig = new MapperConfiguration(config =>
            {
                //TODO: Inspect this why mapping is in this way
                config.CreateMap<OrderHeaderDto, CartHeaderDto>()
                  .ForMember(dest => dest.CartTotal, u => u.MapFrom(src => src.OrderTotal)).ReverseMap();

                config.CreateMap<CartDetailsDto, OrderDetailsDto>()
                  .ForMember(dest => dest.ProductName, u => u.MapFrom(src => src.Product.Name))
                  .ForMember(dest => dest.Price, u => u.MapFrom(src => src.Product.Price));

                config.CreateMap<OrderDetailsDto, CartDetailsDto>().ReverseMap();

                config.CreateMap<OrderHeader, OrderHeaderDto>().ReverseMap();
                config.CreateMap<OrderDetailsDto, OrderDetails>().ReverseMap();

            }, loggerFactory);
            return mappingConfig;
        }
    }
}