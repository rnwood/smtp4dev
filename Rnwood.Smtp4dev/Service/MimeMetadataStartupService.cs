using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rnwood.Smtp4dev.Server;

namespace Rnwood.Smtp4dev.Service
{
    public class MimeMetadataStartupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MimeMetadataStartupService> _logger;

        public MimeMetadataStartupService(IServiceProvider serviceProvider, ILogger<MimeMetadataStartupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting MIME metadata population for existing messages");
                
                using var scope = _serviceProvider.CreateScope();
                var populationService = scope.ServiceProvider.GetRequiredService<MimeMetadataPopulationService>();
                await populationService.PopulateExistingMessagesAsync();
                
                _logger.LogInformation("MIME metadata population completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate MIME metadata for existing messages");
                // Don't fail the application startup, just log the error
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}