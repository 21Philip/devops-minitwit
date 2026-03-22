// Copyright (c) devops-gruppe-connie. All rights reserved.

using Prometheus;

namespace Chirp.Web.Monitoring;

public static class Metrics
{
    // Gauge for CPU load percent
    public static readonly Gauge CpuGauge = MetricsFactory.CreateGauge("minitwit_cpu_load_percent", "Current load of the CPU in percent.");

    // Counter for HTTP responses
    public static readonly Counter ResponseCounter = MetricsFactory.CreateCounter("minitwit_http_responses_total", "The count of HTTP responses sent.");

    // Histogram for request durations (milliseconds)
    public static readonly Histogram RequestDuration = MetricsFactory.CreateHistogram("minitwit_request_duration_milliseconds", "Request duration distribution.");
}

internal static class MetricsFactory
{
    public static Gauge CreateGauge(string name, string help)
    {
        return Prometheus.Metrics.CreateGauge(name, help);
    }

    public static Counter CreateCounter(string name, string help)
    {
        return Prometheus.Metrics.CreateCounter(name, help);
    }

    public static Histogram CreateHistogram(string name, string help)
    {
        return Prometheus.Metrics.CreateHistogram(name, help);
    }
}
