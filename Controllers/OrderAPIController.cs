using AutoMapper;

using Mango.MessageBus;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Service.IService;
using Mango.Services.OrderAPI.Utility;
using Mango.Services.OrderAPI.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.OrderAPI.Controllers
{
    [Route("api/order")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly IMessageProducer _messageBus;
        private readonly ILogger<OrderAPIController> _logger;

        /// <summary>
        /// Dont Make it private readonly, else it will be null.
        /// </summary>
        private IProductService _productService;
        private readonly ResponseDto _response; // response object to communicate success or failure to the client

        public OrderAPIController(
            AppDbContext db,
            IMapper mapper,
            IProductService productService,
            IMessageProducer messageBus,
            ILogger<OrderAPIController> logger)
        {
            _db = db;
            _mapper = mapper;
            _messageBus = messageBus;
            _productService = productService;
            _response = new ResponseDto();
            _logger = logger;
        }

        //TODO: Since it is with parameter, path in OrderService of Web, is different in Post of ProductService

        [HttpPost("CreateOrder")]
        [Authorize]
        public async Task<ResponseDto> CreateOrder(
            [FromBody] CartDto cartDto,
            CancellationToken cancellationToken)
        {
            try
            {
                OrderHeaderDto orderHeaderDto =
                    _mapper.Map<OrderHeaderDto>(cartDto.CartHeader);

                // Validate required event fields before saving the order.
                if (string.IsNullOrWhiteSpace(orderHeaderDto.UserId) ||
                    string.IsNullOrWhiteSpace(orderHeaderDto.Email))
                {
                    throw new InvalidOperationException(
                        "Order requires a user ID and customer email.");
                }

                orderHeaderDto.OrderTime = DateTime.Now;
                orderHeaderDto.Status = SD.Status_Pending;
                orderHeaderDto.OrderDetails =
                    _mapper.Map<IEnumerable<OrderDetailsDto>>(
                        cartDto.CartDetails);

                // Save the order first so it receives a database ID.
                OrderHeader orderCreated = _db.OrderHeaders
                    .Add(_mapper.Map<OrderHeader>(orderHeaderDto)).Entity;

                await _db.SaveChangesAsync(cancellationToken);

                orderHeaderDto.Id = orderCreated.Id;

                // Build the shared OrderCreated event.
                var orderCreatedEvent = new OrderCreatedEvent
                {
                    OrderId = orderCreated.Id,
                    UserId = orderHeaderDto.UserId,
                    CustomerEmail = orderHeaderDto.Email,
                    TotalAmount = orderCreated.OrderTotal,
                    Currency = "INR",
                    OccurredAtUtc = DateTime.UtcNow,
                    Items = orderHeaderDto.OrderDetails.Select(item =>
                        new OrderCreatedItem
                        {
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            Quantity = item.Count,
                            UnitPrice = item.Price
                        }).ToList()
                };



                _response.Result = orderHeaderDto;
                _response.IsSuccess = true;

                try
                {
                    await _messageBus.PublishAsync(
                        orderCreatedEvent,
                        MessageDestination.OrderCreated,
                        orderCreatedEvent.EventId.ToString(),
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Order {OrderId} was saved, but publishing OrderCreated event {EventId} failed.",
                        orderCreated.Id,
                        orderCreatedEvent.EventId);
                    throw;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }

            return _response;
        }


        //TODO: create GET endpoint for all orders
        //TODO: create GET endpoint for order by id

        //[HttpPost("CreateStripeSession")]
        //[Authorize]
        //public async Task<ResponseDto> CreateStripeSession([FromBody] PayementRequestDto paymentRequestDto)
        //{
        //    try
        //    {
        //        var options = new SessionCreateOptions
        //        {
        //            SuccessUrl = paymentRequestDto.ApprovedUrl,
        //            LineItems = new List<SessionLineItemOptions>()
        //            {
        //                new SessionLineItemOptions{
        //                    Price = "price_12345",
        //                    Quantity = 2,
        //                }
        //            },
        //            Mode = "payment",
        //            CancelUrl = paymentRequestDto.CancelUrl,
        //        };
        //        var service = new SessionService();
        //        service.Create(options);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.Message = ex.Message;
        //    }

        //    return _response;
        //}


    }
}
