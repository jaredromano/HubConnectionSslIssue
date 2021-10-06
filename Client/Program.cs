using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Client
{
    class Program
    {
        private static string ServerAddress = "https://localhost:5001";
        private static readonly ServiceCollection ServiceCollection = new();
        private static ILogger _logger;
        private static HubConnection _connection;

        static async Task Main(string[] args)
        {

            ServiceCollection
                .AddLogging(
                    builder =>
                    {
                        builder.AddConsole();
                    });
            var serviceProvider = ServiceCollection.BuildServiceProvider();
            _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(Client));

            try
            {
                _logger.LogInformation("Application Started");
                _connection = new HubConnectionBuilder()
                    .WithUrl(
                        $"{ServerAddress}/TimerTest",
                        httpConnectionOptions =>
                        {
                            httpConnectionOptions.AccessTokenProvider = () =>
                            {
                                return Task.Run(() =>
                                {
                                    _logger.LogWarning("Token request");
                                    return string.Empty;
                                });
                            };
                            httpConnectionOptions.HttpMessageHandlerFactory = _ => new HttpClientHandler()
                            {
                                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                            };
                        })
                    .ConfigureLogging(
                        loggingBuilder =>
                        {
                            loggingBuilder.AddConsole();
                        })
                    .Build();

                _connection.On<string, string>("ReceiveMessage", Handler);

                await _connection.StartAsync();

                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

                await ExecutePeriodically(cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception!");
            }
            finally
            {
                // Stop the application once the work is done
                //_appLifetime.StopApplication();
            }
        }

        private static void Handler(string user, string message)
        {
            _logger.LogInformation($"{user}: {message}");
        }

        private static async Task ExecutePeriodically(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await _connection.InvokeAsync("SendMessage", "user", "test", cancellationToken: ct);
                await Task.Delay(5000, ct);   
            }

        }
    }
}
