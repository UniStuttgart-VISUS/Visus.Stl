# Seasonal Decomposition of Time Series by Loess
This .NET Core implementation of the Seasonal-Trend-Loess (STL) algorithm is a port from the [stl-decomp-4j Java implementation](https://github.com/ServiceNow/stl-decomp-4j), which in turn is port from and the [R](https://stat.ethz.ch/R-manual/R-devel/library/stats/html/stl.html) and [Python](https://github.com/jcrotinger/pyloess) versions, which both use the original Fortran version under the hood.
Therefore, this implementation also expects equally spaced input data without any missing data.
