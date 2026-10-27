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
using Microsoft.Extensions.Options;

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
        private readonly OrderQueueOptions _queueOptions;
        private readonly ResponseDto _response;

        public OrderAPIController(
            AppDbContext db,
            IMapper mapper,
            IProductService productService,
            IMessageProducer messageBus,
            IOptions<OrderQueueOptions> queueOptions,
            ILogger<OrderAPIController> logger)
        {
            _db = db;
            _mapper = mapper;
            _messageBus = messageBus;
            _productService = productService;
            _queueOptions = queueOptions.Value;
            _response = new ResponseDto();
            _logger = logger;
        }

        //TODO: Since it is with parameter, path in OrderService of Web, is different in Post of ProductService

        [HttpPost("CreateOrder")]
        [Authorize]
        public async Task<ResponseDto> CreateOrder([FromBody] CartDto cartDto)
        {
            try
            {
                // Read the queue names from configuration.
                // Read the validated queue names from options.
                var emailQueue = _queueOptions.OrderCreatedEmailQueue;
                var rewardQueue = _queueOptions.OrderCreatedRewardQueue;

                if (string.IsNullOrWhiteSpace(emailQueue) ||
                    string.IsNullOrWhiteSpace(rewardQueue))
                {
                    throw new InvalidOperationException(
                        "OrderCreated queue names are not configured.");
                }

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

                await _db.SaveChangesAsync();

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

                // Publish independently to each consumer queue.
                // The shared MessageBus library applies the supplied queue options.

                var emailQueueOptions = new RabbitMqQueueOptions
                {
                    Durable = true,
                    Arguments = new Dictionary<string, object?>
                    {
                        ["x-dead-letter-exchange"] = "email.dlx",
                        ["x-dead-letter-routing-key"] = emailQueue
                    }
                };

                try
                {
                    await _messageBus.SendMessage(
                        orderCreatedEvent,
                        emailQueue,
                        orderCreatedEvent.EventId.ToString(),
                        emailQueueOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Order {OrderId} was saved, but publishing the email event to queue {QueueName} failed.",
                        orderCreated.Id,
                        emailQueue);
                }

                var rewardQueueOptions = new RabbitMqQueueOptions
                {
                    Durable = true,
                    Arguments = new Dictionary<string, object?>
                    {
                        ["x-dead-letter-exchange"] = "reward.dlx",
                        ["x-dead-letter-routing-key"] = rewardQueue
                    }
                };

                try
                {
                    await _messageBus.SendMessage(
                        orderCreatedEvent,
                        rewardQueue,
                        orderCreatedEvent.EventId.ToString(),
                        rewardQueueOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Order {OrderId} was saved, but publishing the reward event to queue {QueueName} failed.",
                        orderCreated.Id,
                        rewardQueue);
                }


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
