import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// Custom metrics
const batchSizeMetric = new Trend('batch_size');
const messagesPerSecond = new Rate('messages_per_second');
const totalMessages = new Counter('total_messages');

// Test specifically for batch processing performance
export const options = {
  scenarios: {
    // Test different batch sizes
    small_batches: {
      executor: 'constant-vus',
      vus: 10,
      duration: '2m',
      env: { BATCH_SIZE: '10' },
      tags: { batch_type: 'small' },
    },
    medium_batches: {
      executor: 'constant-vus',
      vus: 10,
      duration: '2m',
      startTime: '2m30s',
      env: { BATCH_SIZE: '100' },
      tags: { batch_type: 'medium' },
    },
    large_batches: {
      executor: 'constant-vus',
      vus: 5,
      duration: '2m',
      startTime: '5m',
      env: { BATCH_SIZE: '1000' },
      tags: { batch_type: 'large' },
    },
    // Test ETL batch size (8192)
    etl_batches: {
      executor: 'constant-vus',
      vus: 2,
      duration: '2m',
      startTime: '7m30s',
      env: { BATCH_SIZE: '8192' },
      tags: { batch_type: 'etl_optimal' },
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<5000'], // Allow longer for large batches
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = 'http://localhost:5000';

function generateTaxiTrip() {
  const now = new Date();
  const pickupTime = new Date(now.getTime() - Math.random() * 3600000);
  const dropoffTime = new Date(pickupTime.getTime() + (Math.random() * 60 + 5) * 60000);
  
  const distance = Math.round((Math.random() * 15 + 0.5) * 100) / 100;
  const fareAmount = Math.round((2.50 + distance * 2.50) * 100) / 100;
  const tipAmount = Math.round((fareAmount * Math.random() * 0.3) * 100) / 100;
  
  const trip = {
    eventType: "trip.completed",
    source: "k6-batch-test",
    tripData: {
      VendorID: Math.random() > 0.6 ? 1 : 2,
      tpep_pickup_datetime: pickupTime.toISOString(),
      tpep_dropoff_datetime: dropoffTime.toISOString(),
      passenger_count: Math.floor(Math.random() * 4) + 1,
      trip_distance: distance,
      RatecodeID: 1,
      store_and_fwd_flag: "N",
      PULocationID: Math.floor(Math.random() * 265) + 1,
      DOLocationID: Math.floor(Math.random() * 265) + 1,
      payment_type: Math.random() > 0.7 ? 1 : 2,
      fare_amount: fareAmount,
      extra: Math.random() > 0.7 ? 0.50 : 0,
      mta_tax: 0.50,
      tip_amount: tipAmount,
      tolls_amount: Math.random() > 0.9 ? Math.round((Math.random() * 8 + 2) * 100) / 100 : 0,
      improvement_surcharge: 0.30,
      total_amount: 0,
      congestion_surcharge: Math.random() > 0.6 ? 2.50 : 0,
    },
    correlationId: `batch-test-${__VU}-${__ITER}-${Date.now()}`
  };
  
  // Calculate total
  trip.tripData.total_amount = Math.round((
    trip.tripData.fare_amount +
    trip.tripData.extra +
    trip.tripData.mta_tax +
    trip.tripData.tip_amount +
    trip.tripData.tolls_amount +
    trip.tripData.improvement_surcharge +
    (trip.tripData.congestion_surcharge || 0)
  ) * 100) / 100;
  
  return trip;
}

export default function () {
  const batchSize = parseInt(__ENV.BATCH_SIZE) || 100;
  const trips = [];
  
  // Generate batch of trips
  for (let i = 0; i < batchSize; i++) {
    trips.push(generateTaxiTrip());
  }
  
  const payload = JSON.stringify(trips);
  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    tags: { 
      batch_size: batchSize.toString(),
      scenario: __ENV.K6_SCENARIO || 'unknown'
    },
  };
  
  const startTime = Date.now();
  const response = http.post(`${BASE_URL}/api/taxi/trips/batch`, payload, params);
  const endTime = Date.now();
  
  const isSuccess = check(response, {
    [`batch ${batchSize} status is 200`]: (r) => r.status === 200,
    [`batch ${batchSize} response time acceptable`]: (r) => {
      // Scale expected time with batch size
      const maxTime = Math.max(1000, batchSize * 0.5);
      return r.timings.duration < maxTime;
    },
  });
  
  if (isSuccess) {
    batchSizeMetric.add(batchSize);
    totalMessages.add(batchSize);
    
    const duration = endTime - startTime;
    const messagesPerSec = (batchSize / duration) * 1000;
    messagesPerSecond.add(messagesPerSec);
    
    console.log(`✅ Batch ${batchSize}: ${duration}ms (${messagesPerSec.toFixed(2)} msg/s)`);
  } else {
    console.log(`❌ Batch ${batchSize} failed: ${response.status} - ${response.body.substring(0, 200)}`);
  }
  
  // Longer sleep for larger batches to allow processing
  const sleepTime = Math.max(1, Math.min(10, batchSize / 1000));
  sleep(sleepTime);
}

export function handleSummary(data) {
  const scenarios = Object.keys(data.metrics).filter(key => key.includes('batch_type'));
  
  let summary = `
========== Batch Performance Test Summary ==========
Total HTTP Requests: ${data.metrics.http_reqs.count}
Total Messages Sent: ${data.metrics.total_messages ? data.metrics.total_messages.count : 'N/A'}
Failed Requests: ${data.metrics.http_req_failed.count} (${(data.metrics.http_req_failed.rate * 100).toFixed(2)}%)

Average Response Time: ${data.metrics.http_req_duration.avg.toFixed(2)}ms
95th Percentile: ${data.metrics.http_req_duration['p(95)'].toFixed(2)}ms
Max Response Time: ${data.metrics.http_req_duration.max.toFixed(2)}ms

`;

  if (data.metrics.batch_size) {
    summary += `Average Batch Size: ${data.metrics.batch_size.avg.toFixed(0)}\n`;
  }

  if (data.metrics.messages_per_second) {
    summary += `Peak Messages/Second: ${data.metrics.messages_per_second.max ? data.metrics.messages_per_second.max.toFixed(2) : 'N/A'}\n`;
  }

  summary += `
Recommendations:
- Batch size 100-1000: Good balance of throughput and latency
- Batch size 8192: Optimal for ETL processing (matches ClickHouse index_granularity)
- Monitor API response times and adjust batch sizes accordingly
================================================
`;

  return {
    'batch-summary.json': JSON.stringify(data, null, 2),
    stdout: summary,
  };
}