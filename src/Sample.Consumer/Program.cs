namespace Sample.Worker
{
    using System.Threading.Tasks;
    using Consumers;
    using Contracts;
    using MassTransit;
    using Microsoft.Extensions.Configuration;
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

                x.AddConsumer<OrderSubmittedConsumer>();

                x.UsingAzureServiceBus((context, cfg) =>
                {
                    cfg.Host(builder.Configuration.GetConnectionString("AzureServiceBus"));

                    cfg.UseServiceBusMessageScheduler();

                    // Subscribe to OrderSubmitted directly on the topic, instead of configuring a queue
                    cfg.SubscriptionEndpoint<OrderSubmitted>("order-submitted-consumer", e =>
                    {
                        e.RequiresSession = true;
                        e.ConfigureConsumer<OrderSubmittedConsumer>(context);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            return builder;
        }
    }
}