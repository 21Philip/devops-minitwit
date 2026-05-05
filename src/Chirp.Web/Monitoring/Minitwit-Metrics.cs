// Copyright (c) devops-gruppe-connie. All rights reserved.

using Prometheus;

namespace Chirp.Web.Monitoring;

public static class Metrics
{
    public static readonly Gauge CpuGauge = MetricsFactory.CreateGauge("minitwit_cpu_load_percent", "Current load of the CPU in percent.");

    public static readonly Counter HttpResponses = MetricsFactory.CreateCounter("minitwit_http_responses_total", "The count of HTTP responses sent.");

    public static readonly Gauge CheepsPosted = Prometheus.Metrics.CreateGauge(
        "minitwit_cheeps_posted_total",
        "Total number of cheeps in the database.");

    public static readonly Gauge AuthorsFollowed = Prometheus.Metrics.CreateGauge(
        "minitwit_authors_followed_total",
        "Total sum of followers across all authors.");

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
