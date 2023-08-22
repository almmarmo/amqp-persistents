// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Lib.Amqp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Starting application...");

ServiceCollection services = new();

var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

services.AddOptions();
//var options = new PersistentConsumerOptions();
//config.GetSection("PersistentConsumer").Bind(options);
services.AddSingleton<IConfiguration>(config);
services.AddLogging(s => s.AddConsole());
services.AddOptions<PersistentConsumerOptions>().BindConfiguration("PersistentConsumer");
services.AddSingleton<PersistentConsumerFactory>();
services.AddScoped<IPersistentConsumer, PersistentConsumer>(x =>
{
    var factory = x.GetService<PersistentConsumerFactory>()!;

    return factory.Create();
});

ServiceProvider provider = services.BuildServiceProvider();

IPersistentConsumer consumer = provider.GetService<IPersistentConsumer>()!;

await consumer.StartReceive(async w => {
    string messageBody = w.GetMessageBody().ToString()!;

    Console.WriteLine($"[{DateTime.Now}]-{messageBody}");

    w.AcceptMessage();
    await Task.CompletedTask;
});
