using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

namespace NamecheapDynamicDnsPinger;

public static class Program
{
    private static string ServiceName = ".NET Namecheap Dynamic DNS Pinger";
    public static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args).UseWindowsService(options =>
        {
            options.ServiceName = ServiceName;
        })
        .ConfigureLogging((context, logging) =>
        {
            // See: https://github.com/dotnet/runtime/issues/47303
            logging.AddConfiguration(
                context.Configuration.GetSection("Logging"));
        })
        .ConfigureServices(services =>
        {
            LoggerProviderOptions.RegisterProviderOptions<
                EventLogSettings, EventLogLoggerProvider>(services);
            var password = Environment.GetEnvironmentVariable("NamecheapDynamicDNSPassword");
            if (password is null)
            {
                throw new Exception("Unable to find password for namecheap");
            }

            services.AddSingleton(new DomainInfo("nathanieljwright.com", "minecraft", password));
            services.AddHostedService<PingerService>();
        })
        .Build();

        await host.RunAsync();
    }
}
