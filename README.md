# Seasonal Decomposition of Time Series by Loess
![STL.NET Version](https://buildstats.info/nuget/stlnet)

This .NET Core implementation of the Seasonal-Trend-Loess (STL) algorithm is a port from the [stl-decomp-4j Java implementation](https://github.com/ServiceNow/stl-decomp-4j), which in turn is port from and the [R](https://stat.ethz.ch/R-manual/R-devel/library/stats/html/stl.html) and [Python](https://github.com/jcrotinger/pyloess) versions, which both use the original Fortran version under the hood. It also uses parts of an internal .NET implementation by Dominik Herr of the VIS institute.
Therefore, this implementation also expects equally spaced input data without any missing data.


## Usage
The STL algorithm requires you to specifying the periodicity of the data and the width of the Loess smoother used to smooth seasonal data. Although an instance of `SeasonalTrendLoess` can be created directly, it is recommended to use the builder, which provides a fluent API as it is common in ASP.NET Core.

```c#
IList<double> values = ... // Some per-month data.

// Create a factory that allows creating the STL smoother.
var builder = new SeasonalTrendLoessBuilder()
    .SetPeriodLength(12)   // Data has period of 12 month.
    .SetSeasonalWidth(35)  // Monthly data are smoothed over 35 years.
    .SetNonRobust();       // Do not perform robustness iterations as we do not expect outliers.

// Instantiate the smoother for the data.
var smoother = builder.Build(values);

// Create the decomposition.
var stl = smoother.Decompose();

// Season, trend and remainder can be accessed as property of the decomposition.
var seasonal = stl.Seasonal;
var trend = stl.Trend;
var residuals = stl.Residuals;
```

In order to use data that are not equally spaced, we provide an extension method `SpaceEvenly` for pairs of `DateTime` and a value. In order to use this extension method, you need to specify the temporal length of a bin, an aggregator function that can sum data points falling into the same bin and a neutral element (the zero value).

```C#
var input = new[] {
    new DateTimePoint<int>(new DateTime(2020, 1, 1, 0, 0, 0), 0),
    // Other data points
};

// Create an evenly spaced time series with one-second bins.
var output = input.SpaceEvenly(TimeSpan.FromSeconds(1), (l, r) => l + r, 0).ToArray();
```

There is a convenience method of `SpaceEvenly` for `DateTimePoint<double>` that can be directly used without specifying an aggregator and the neutral element. There is also an even more generic version, which allows for mapping basically any type of structure by retrieving the time and the value via callbacks:

```C#
var input = new[] {
    new DateTimePoint<int>(new DateTime(2020, 1, 1, 0, 0, 0), 0),
    // Other data points
};

// Create an evenly spaced time series with one-second bins.
var output = input.SpaceEvenly(TimeSpan.FromSeconds(1),
    (e) => e.Time,
    (e) => e.Value,
    (l, r) => l + r,
    0).ToArray();
```

## Acknowledgements
This project has received funding from the European Union’s Horizon 2020 research and innovation programme under grant agreement No. 833418.
