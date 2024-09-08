namespace Sample.Worker.Consumers
{
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using Microsoft.Extensions.Logging;


    public class OrderSubmittedConsumer :
        IConsumer<OrderSubmitted>
    {
        private static int _count = 0;
        readonly ILogger _logger;

        public OrderSubmittedConsumer(ILogger<OrderSubmittedConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderSubmitted> context)
        {
            _logger.LogInformation("Order Submitted: {OrderId}-{Count}", context.Message.OrderId, _count);
            _count++;

            if (_count > 5 && _count < 100)
            {
                return;
            }

            if (_count == 100)
            {
                _count = 0;
            }
            await Task.Delay(1100);
        }
    }
}