using DevStore.Core.Messages;
using DevStore.Core.Messages.Integration;
using DevStore.MessageBus;
using DevStore.Orders.API.Application.DTO;
using DevStore.Orders.API.Application.Events;
using DevStore.Orders.Domain.Vouchers;
using DevStore.Orders.Domain.Vouchers.Specs;
using FluentValidation.Results;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevStore.Orders.Domain.Orders;
using DevStore.Core.Http;

namespace DevStore.Orders.API.Application.Commands
{
    public class OrderCommandHandler : CommandHandler,
        IRequestHandler<AddOrderCommand, ValidationResult>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IMessageBus _bus;
        private readonly IRestClient _restClient;

        public OrderCommandHandler(IVoucherRepository voucherRepository,
                                   IOrderRepository orderRepository,
                                   IMessageBus bus,
                                   IRestClient restClient)
        {
            _voucherRepository = voucherRepository;
            _orderRepository = orderRepository;
            _bus = bus;
            _restClient = restClient;
        }

        public async Task<ValidationResult> Handle(AddOrderCommand message, CancellationToken cancellationToken)
        {
            // command validation
            if (!message.IsValid()) return message.ValidationResult;

            // Map Order
            var order = MapOrder(message);

            // apply voucher, if exists
            if (!await ApplyVoucher(message, order)) return ValidationResult;

            // Validate order
            if (!IsOrderValid(order)) return ValidationResult;

            // pay the order
            if (!await DoPayment(order, message)) return ValidationResult;

            // If paid, authorize order!
            order.Authorize();

            // Adding event
            order.AddEvent(new OrderDoneEvent(order.Id, order.CustomerId));

            // Add Order Repositorio
            _orderRepository.Add(order);

            // Commiting order and voucher data
            return await PersistData(_orderRepository.UnitOfWork);
        }

        private Order MapOrder(AddOrderCommand message)
        {
            var address = new Address
            {
                StreetAddress = message.Address.StreetAddress,
                BuildingNumber = message.Address.BuildingNumber,
                SecondaryAddress = message.Address.SecondaryAddress,
                Neighborhood = message.Address.Neighborhood,
                ZipCode = message.Address.ZipCode,
                City = message.Address.City,
                State = message.Address.State
            };

            var order = new Order(message.CustomerId, message.Amount, message.OrderItems.Select(OrderItemDTO.ToOrderItem).ToList(),
                message.HasVoucher, message.Discount);

            order.SetAddress(address);
            return order;
        }

        private async Task<bool> ApplyVoucher(AddOrderCommand message, Order order)
        {
            if (!message.HasVoucher) return true;

            var voucher = await _voucherRepository.GetVoucherByCode(message.Voucher);
            if (voucher == null)
            {
                AddError("Voucher not found!");
                return false;
            }

            var voucherValidation = new VoucherValidation().Validate(voucher);
            if (!voucherValidation.IsValid)
            {
                voucherValidation.Errors.ToList().ForEach(m => AddError(m.ErrorMessage));
                return false;
            }

            order.AssociateVoucher(voucher);
            voucher.GetOne();

            _voucherRepository.Update(voucher);

            return true;
        }

        private bool IsOrderValid(Order order)
        {
            var orderAmount = order.Amount;
            var orderDiscount = order.Discount;

            order.CalculateOrderAmount();

            if (order.Amount != orderAmount)
            {
                AddError("The order total amount order is different from total amount of individual items");
                return false;
            }

            if (order.Discount != orderDiscount)
            {
                AddError("The amount sent is different from order amount");
                return false;
            }

            return true;
        }

        public async Task<bool> DoPayment(Order order, AddOrderCommand message)
        {
            var orderStarted = new OrderInitiatedIntegrationEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Amount = order.Amount,
                PaymentType = 1, // fixed - change if we have more types
                Holder = message.Holder,
                CardNumber = message.CardNumber,
                ExpirationDate = message.ExpirationDate,
                SecurityCode = message.SecurityCode
            };

            //var result = await _bus
            //    .RequestAsync<OrderInitiatedIntegrationEvent, ResponseMessage>(orderStarted);

            var result = await _restClient
                .PostAsync<OrderInitiatedIntegrationEvent, ResponseMessage>(orderStarted);

            if (result.ValidationResult.IsValid) return true;

            foreach (var erro in result.ValidationResult.Errors)
            {
                AddError(erro.ErrorMessage);
            }

            return false;
        }
    }
}