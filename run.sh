#!/bin/bash

# Start the web server in the background
dotnet SimpleAPI.dll &

# Wait for the server to start
sleep 5

# Run Apache Benchmark
ab -n 1000 -c 10 http://localhost:8080/health

# Exit after benchmarking
