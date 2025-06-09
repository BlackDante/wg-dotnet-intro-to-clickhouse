# K6 Performance Tests for EventCollector

This directory contains k6 performance tests to validate the EventCollector API under various load conditions and generate high-volume taxi trip data.

## Test Scripts

### 1. `taxi-load-test.js` - Comprehensive Load Testing
**Purpose**: Full-scale performance testing with realistic taxi trip data and multiple traffic patterns.

**Features**:
- **Realistic Data Generation**: Uses actual NYC taxi LocationIDs and realistic trip patterns
- **Mixed Request Types**: 70% single trips, 30% batch requests (5-50 trips per batch)
- **Multiple Scenarios**:
  - Warm-up: Gradual ramp from 1 to 50 users
  - High Load: Sustained 100 concurrent users for 5 minutes
  - Burst Load: Peak at 1000 requests/second
- **Comprehensive Metrics**: Response times, error rates, throughput analysis

**Run Command**:
```bash
k6 run taxi-load-test.js
```

### 2. `simple-load-test.js` - Quick Validation
**Purpose**: Fast, simple load test for basic API validation.

**Features**:
- Simple taxi trip generation
- 20-50 concurrent users
- 2-minute test duration
- Basic performance thresholds

**Run Command**:
```bash
k6 run simple-load-test.js
```

### 3. `batch-performance-test.js` - Batch Processing Analysis
**Purpose**: Specifically test batch processing performance with different batch sizes.

**Features**:
- **Multiple Batch Sizes**: 10, 100, 1000, and 8192 (ETL optimal)
- **Performance Comparison**: Analyze throughput vs latency for different batch sizes
- **ETL Optimization**: Test 8192 batch size (matches ClickHouse index_granularity)
- **Custom Metrics**: Messages per second, batch size trends

**Run Command**:
```bash
k6 run batch-performance-test.js
```

## Prerequisites

1. **Install k6**:
   ```bash
   # macOS
   brew install k6
   
   # Linux
   sudo gpg -k
   sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
   echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
   sudo apt-get update
   sudo apt-get install k6
   
   # Windows
   winget install k6
   ```

2. **Start EventCollector API**:
   ```bash
   cd EventCollector.API
   dotnet run
   ```

3. **Start EventCollector ETL** (optional, for end-to-end testing):
   ```bash
   cd EventCollector.ETL
   dotnet run
   ```

4. **Ensure Kafka is running** (if testing with ETL):
   ```bash
   # Start from project root
   docker-compose up -d
   ```

## Test Data

The tests generate realistic NYC taxi trip data including:

- **Vendor IDs**: 1 (Creative Mobile Technologies) and 2 (VeriFone Inc.)
- **Real LocationIDs**: Actual pickup/dropoff locations from NYC TLC data
- **Realistic Patterns**:
  - 80% single passenger trips
  - 70% credit card payments
  - 30% tips (10-35% of fare)
  - 5% store-and-forward trips
  - 40% congestion surcharge (Manhattan business district)
  - 10% toll trips
  - Trip durations: 5-65 minutes
  - Trip distances: 0.5-20.5 miles

## Performance Thresholds

- **Response Time**: 95th percentile < 500ms (1000ms for large batches)
- **Error Rate**: < 1%
- **Availability**: > 99%

## Expected Results

### Single Trip Requests
- **Throughput**: 500-1000 requests/second
- **Latency**: p95 < 100ms

### Batch Requests
- **Small Batches (10-50)**: p95 < 500ms
- **Medium Batches (100-500)**: p95 < 1000ms  
- **Large Batches (1000+)**: p95 < 2000ms
- **ETL Batches (8192)**: p95 < 5000ms

### Message Throughput
- **Target**: 10,000+ messages/second during peak load
- **Batch Processing**: 50,000+ messages/second with optimal batch sizes

## Monitoring During Tests

Monitor these components during load testing:

1. **EventCollector API**:
   - Response times and error rates
   - Memory and CPU usage
   - Request queue lengths

2. **Kafka**:
   - Topic lag and throughput
   - Partition utilization
   - Producer/consumer rates

3. **EventCollector ETL**:
   - Batch processing rates
   - ClickHouse insertion performance
   - Memory usage during batching

4. **ClickHouse**:
   - Insert rates and query performance
   - Memory and disk usage
   - Table compression ratios

## Customization

### Adjust Load Patterns
Modify the `options.scenarios` in each script to change:
- Virtual user counts
- Test duration
- Ramp-up/ramp-down patterns
- Request rates

### Change API Endpoint
Update `BASE_URL` variable to test different environments:
```javascript
const BASE_URL = 'https://your-api-endpoint.com';
```

### Custom Data Patterns
Modify the `generateTaxiTrip()` functions to:
- Change trip patterns and distributions
- Add specific business logic testing
- Include edge cases and error scenarios

## Example Output

```
========== Performance Test Summary ==========
Total Requests: 45,231
Failed Requests: 12 (0.03%)
Average Response Time: 156.23ms
95th Percentile: 342.15ms
Max Response Time: 1,204.33ms
Requests/sec: 376.92

Thresholds:
- 95th percentile < 500ms: ✅ PASS
- Error rate < 1%: ✅ PASS
============================================
```

## Troubleshooting

### High Error Rates
- Check API logs for specific error messages
- Verify Kafka connectivity
- Ensure database connections are stable
- Monitor resource utilization

### Poor Performance
- Increase API instance resources
- Optimize batch sizes
- Check network latency
- Review ClickHouse performance settings

### Connection Issues
- Verify all services are running
- Check firewall settings
- Confirm endpoint URLs and ports
- Test connectivity with simple HTTP requests