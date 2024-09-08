namespace Sample.Worker
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    //using Consumers;
    using Contracts;
    using MassTransit;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;


    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = CreateHostBuilder(args);

            var host = builder.Build();
            await host.RunAsync();
        }

        public static HostApplicationBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddMassTransit(x =>
                    {
                        x.AddServiceBusMessageScheduler();

                        x.SetKebabCaseEndpointNameFormatter();

                        x.UsingAzureServiceBus((context, cfg) =>
                        {
                            cfg.Host(builder.Configuration.GetConnectionString("AzureServiceBus"));

                            cfg.UseServiceBusMessageScheduler();

                            cfg.Send<OrderSubmitted>(s => s.UseSessionIdFormatter(c => c.Message.OrderId.ToString("D")));
                            cfg.Send<MonitorOrderShipmentTimeout>(s => s.UseSessionIdFormatter(c => c.Message.OrderId.ToString("D")));

                            cfg.ConfigureEndpoints(context);
                        });
                    });

            builder.Services.AddHostedService<Worker>();
            return builder;
        }
    }

    public class Worker : BackgroundService
    {
        readonly IServiceScopeFactory _scopeFactory;

        readonly Guid _defaultOrderId = Guid.Parse("4354ABD4-4DCD-447F-B2BF-64FA5F877023");
        readonly Guid _defaultOrderId2 = Guid.Parse("4354ABD4-4DCD-447F-B2BF-64FA5F877024");

        private static int _count = 1;

        public Worker(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var pubEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                var orderId = _count % 2 == 0 ? _defaultOrderId : _defaultOrderId2;
                orderId = _count % 3 == 0 ? Guid.NewGuid() : orderId;

                Console.WriteLine($"OrderSubmitted: {orderId}");
                await pubEndpoint.Publish(new OrderSubmitted
                {
                    OrderId = orderId,
                    OrderTimestamp = DateTimeOffset.Now,
                    OrderNumber = "test-order-1"
                }, stoppingToken);

                await Task.Delay(1000, stoppingToken);
                _count++;
            }
        }
    }
}