

using EventHub.Domain.Entities;

namespace EventHub.Application.Specifications.Payments
{
        public sealed class GetPaymentByPaymobOrderIdSpecification:Specification<Payment, Guid>
        {
            public GetPaymentByPaymobOrderIdSpecification(string orderId)
                : base(p => p.PaymobOrderId == orderId)
            {
            }
        }
    
}
