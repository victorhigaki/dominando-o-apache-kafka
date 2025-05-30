using DevStore.Billing.API.Models;
using DevStore.Billing.API.Services;
using DevStore.Core.Messages.Integration;
using DevStore.WebAPI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DevStore.Billing.API.Controllers
{
    public class PaymentController : MainController
    {
        [HttpPost("/payment/order-initiated")]
        public async Task<IActionResult> PostAsync(
            OrderInitiatedIntegrationEvent orderInitiatedIntegrationEvent, 
            [FromServices] IBillingService billingService)
        {
            var payment = GetPaymentFrom(orderInitiatedIntegrationEvent);
            var response = await billingService.AuthorizeTransaction(payment);
            return Ok(payment);
        }

        private static Payment GetPaymentFrom(OrderInitiatedIntegrationEvent orderInitiatedIntegrationEvent)
        {
            return new Payment
            {
                OrderId = orderInitiatedIntegrationEvent.OrderId,
                PaymentType = (PaymentType)orderInitiatedIntegrationEvent.PaymentType,
                Amount = orderInitiatedIntegrationEvent.Amount,
                CreditCard = new CreditCard(
                    orderInitiatedIntegrationEvent.Holder, 
                    orderInitiatedIntegrationEvent.CardNumber, 
                    orderInitiatedIntegrationEvent.ExpirationDate, 
                    orderInitiatedIntegrationEvent.SecurityCode)
            };
        }
    }
}