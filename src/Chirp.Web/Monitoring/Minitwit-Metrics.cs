// Copyright (c) devops-gruppe-connie. All rights reserved.

using Prometheus;

namespace Chirp.Web.Monitoring;

public static class Metrics
{
    // Gauge for CPU load percent
    public static readonly Gauge CpuGauge = MetricsFactory.CreateGauge("minitwit_cpu_load_percent", "Current load of the CPU in percent.");

    public static readonly Counter HttpResponses = Prometheus.Metrics.CreateCounter(
      "minitwit_http_responses_total",
      "HTTP responses sent.",
      new CounterConfiguration { LabelNames = new[] { "method", "route", "status" } });

    public static readonly Histogram HttpRequestDuration = Prometheus.Metrics.CreateHistogram(
      "minitwit_http_request_duration_seconds",
      "HTTP request duration in seconds.",
      new HistogramConfiguration
      {
        LabelNames = new[] { "method", "route", "status" },
        Buckets = Histogram.ExponentialBuckets(0.005, 2, 12), // 5ms .. ~10s
      });

    public static readonly Counter CheepsPosted = Prometheus.Metrics.CreateCounter(
      "minitwit_cheeps_posted_total",
      "Number of cheeps successfully posted.");

    public static readonly Counter AuthorsFollowed = Prometheus.Metrics.CreateCounter(
        "minitwit_authors_followed_total",
        "Number of follow actions performed by users.");

    public static readonly Gauge UserFollowers = Prometheus.Metrics.CreateGauge(
      "minitwit_user_followers",
      "Number of followers per user.",
      new GaugeConfiguration { LabelNames = new[] { "user" } });
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
