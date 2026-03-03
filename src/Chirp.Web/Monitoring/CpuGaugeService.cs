using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chirp.Web.Monitoring
{
    public class CpuGaugeService : BackgroundService
    {
        private readonly ILogger<CpuGaugeService> _logger;

        public CpuGaugeService(ILogger<CpuGaugeService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CpuGaugeService starting.");

            // Poll CPU usage at interval
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Simple CPU usage estimate using Process.TotalProcessorTime over 1s
                    var cpuPercent = await GetCpuUsageForProcessAsync(1000, stoppingToken).ConfigureAwait(false);
                    Metrics.CpuGauge.Set(cpuPercent);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // shutting down
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update CPU gauge");
                }

                await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
            }

            _logger.LogInformation("CpuGaugeService stopping.");
        }

        private static async Task<double> GetCpuUsageForProcessAsync(int sampleMilliseconds, CancellationToken ct)
        {
            var process = Process.GetCurrentProcess();
            var startTime = process.TotalProcessorTime;
            var start = DateTime.UtcNow;
            await Task.Delay(sampleMilliseconds, ct).ConfigureAwait(false);
            process.Refresh();
            var endTime = process.TotalProcessorTime;
            var end = DateTime.UtcNow;
            var cpuUsedMs = (endTime - startTime).TotalMilliseconds;
            var totalMsPassed = (end - start).TotalMilliseconds;
            var cpuCores = Environment.ProcessorCount;
            if (totalMsPassed <= 0) return 0.0;
            var cpuUsageTotal = (cpuUsedMs / (totalMsPassed * cpuCores)) * 100.0;
            return cpuUsageTotal;
        }
    }
}

