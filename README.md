# Seasonal Decomposition of Time Series by Loess
This .NET Core implementation of the Seasonal-Trend-Loess (STL) algorithm is a port from the [stl-decomp-4j Java implementation](https://github.com/ServiceNow/stl-decomp-4j), which in turn is port from and the [R](https://stat.ethz.ch/R-manual/R-devel/library/stats/html/stl.html) and [Python](https://github.com/jcrotinger/pyloess) versions, which both use the original Fortran version under the hood.
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
