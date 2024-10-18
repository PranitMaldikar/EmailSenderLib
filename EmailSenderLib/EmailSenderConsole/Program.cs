using EmailSenderLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

class Program
{
    static async Task Main(string[] args)
    {
        // Load appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Initialize Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("console_email_logs.txt")
            .CreateLogger();

        // Create service collection
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Add logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddSerilog(dispose: true);
        });

        // Add EmailSender
        services.AddTransient<EmailSender>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get EmailSender instance
        var emailSender = serviceProvider.GetRequiredService<EmailSender>();

        // Input email details
        Console.WriteLine("Enter recipient email:");
        string recipientEmail = Console.ReadLine();

        await emailSender.SendEmailAsync(recipientEmail, "Test Subject", "This is a test email body.");

        // Ensure all logs are flushed
        Log.CloseAndFlush();
    }
}