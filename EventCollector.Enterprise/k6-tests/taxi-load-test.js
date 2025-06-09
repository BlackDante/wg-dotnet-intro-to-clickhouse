import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
  scenarios: {
    // Warm-up phase: gradually increase load
    warmup: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '30s', target: 10 },
        { duration: '1m', target: 50 },
      ],
      gracefulRampDown: '30s',
      tags: { test_type: 'warmup' },
    },
    
    // High load: sustained high throughput to test batch processing
    high_load: {
      executor: 'constant-vus',
      vus: 100,
      duration: '5m',
      startTime: '2m',
      tags: { test_type: 'high_load' },
    },
    
    // Burst load: simulate traffic spikes
    burst_load: {
      executor: 'ramping-arrival-rate',
      startRate: 100,
      timeUnit: '1s',
      preAllocatedVUs: 50,
      maxVUs: 200,
      stages: [
        { duration: '30s', target: 500 }, // Ramp up to 500 req/s
        { duration: '1m', target: 1000 },  // Peak at 1000 req/s
        { duration: '30s', target: 200 },  // Ramp down
      ],
      startTime: '8m',
      tags: { test_type: 'burst' },
    },
  },
  
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests under 500ms
    http_req_failed: ['rate<0.01'],   // Error rate under 1%
    errors: ['rate<0.05'],            // Custom error rate under 5%
  },
};

const BASE_URL = 'http://localhost:5000';

// NYC taxi location IDs (real LocationIDs from NYC TLC data)
const PICKUP_LOCATIONS = [142, 236, 161, 186, 239, 48, 79, 87, 261, 114, 68, 90, 13, 4, 162, 170, 137, 166, 230, 107];
const DROPOFF_LOCATIONS = [236, 142, 161, 79, 48, 186, 87, 114, 261, 68, 137, 90, 170, 239, 107, 230, 162, 4, 166, 13];

// Generate realistic taxi trip data
function generateTaxiTrip() {
  const now = new Date();
  const pickupTime = new Date(now.getTime() - Math.random() * 3600000); // Up to 1 hour ago
  const tripDurationMinutes = 5 + Math.random() * 60; // 5-65 minutes
  const dropoffTime = new Date(pickupTime.getTime() + tripDurationMinutes * 60000);
  
  const distance = 0.5 + Math.random() * 20; // 0.5-20.5 miles
  const baseRate = 2.50;
  const perMileRate = 2.50;
  const fareAmount = baseRate + (distance * perMileRate);
  const tip = Math.random() > 0.3 ? fareAmount * (0.1 + Math.random() * 0.25) : 0; // 30% no tip, others 10-35%
  
  return {
    eventType: "trip.completed",
    source: `taxi-mobile-app-${Math.floor(Math.random() * 100)}`,
    tripData: {
      VendorID: Math.random() > 0.6 ? 1 : 2, // 60% vendor 2, 40% vendor 1
      tpep_pickup_datetime: pickupTime.toISOString(),
      tpep_dropoff_datetime: dropoffTime.toISOString(),
      passenger_count: Math.random() > 0.8 ? Math.floor(Math.random() * 4) + 2 : 1, // 80% single passenger
      trip_distance: Math.round(distance * 100) / 100,
      RatecodeID: Math.random() > 0.95 ? Math.floor(Math.random() * 5) + 2 : 1, // 95% standard rate
      store_and_fwd_flag: Math.random() > 0.95 ? "Y" : "N", // 5% store and forward
      PULocationID: PICKUP_LOCATIONS[Math.floor(Math.random() * PICKUP_LOCATIONS.length)],
      DOLocationID: DROPOFF_LOCATIONS[Math.floor(Math.random() * DROPOFF_LOCATIONS.length)],
      payment_type: Math.random() > 0.7 ? 1 : 2, // 70% credit card, 30% cash
      fare_amount: Math.round(fareAmount * 100) / 100,
      extra: Math.random() > 0.7 ? 0.50 : 0, // 30% extra charge
      mta_tax: 0.50,
      tip_amount: Math.round(tip * 100) / 100,
      tolls_amount: Math.random() > 0.9 ? Math.round((Math.random() * 10 + 2) * 100) / 100 : 0, // 10% have tolls
      improvement_surcharge: 0.30,
      total_amount: 0,
      congestion_surcharge: Math.random() > 0.6 ? 2.50 : 0, // 40% congestion surcharge
      trip_type: Math.random() > 0.9 ? 2 : 1, // 90% street-hail, 10% dispatch
      ehail_fee: null
    },
    driverId: `D${Math.floor(Math.random() * 10000).toString().padStart(5, '0')}`,
    vehicleId: `V${Math.floor(Math.random() * 10000).toString().padStart(5, '0')}`,
    correlationId: `trip-${Math.random().toString(36).substr(2, 9)}`
  };
}

// Calculate total amount
function calculateTotalAmount(trip) {
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
  // Randomly choose between single trip and batch requests
  const useBatch = Math.random() > 0.7; // 30% batch requests
  
  if (useBatch) {
    // Send batch of 5-50 trips
    const batchSize = Math.floor(Math.random() * 46) + 5;
    const trips = [];
    
    for (let i = 0; i < batchSize; i++) {
      trips.push(calculateTotalAmount(generateTaxiTrip()));
    }
    
    const payload = JSON.stringify(trips);
    const params = {
      headers: {
        'Content-Type': 'application/json',
      },
      tags: { request_type: 'batch', batch_size: batchSize.toString() },
    };
    
    const response = http.post(`${BASE_URL}/api/taxi/trips/batch`, payload, params);
    
    const isSuccess = check(response, {
      'batch request status is 200': (r) => r.status === 200,
      'batch request duration < 2000ms': (r) => r.timings.duration < 2000,
    });
    
    if (!isSuccess) {
      errorRate.add(1);
      console.log(`Batch request failed: ${response.status} - ${response.body}`);
    }
    
  } else {
    // Send single trip
    const trip = calculateTotalAmount(generateTaxiTrip());
    const payload = JSON.stringify(trip);
    const params = {
      headers: {
        'Content-Type': 'application/json',
      },
      tags: { request_type: 'single' },
    };
    
    const response = http.post(`${BASE_URL}/api/taxi/trips`, payload, params);
    
    const isSuccess = check(response, {
      'single request status is 200': (r) => r.status === 200,
      'single request duration < 1000ms': (r) => r.timings.duration < 1000,
    });
    
    if (!isSuccess) {
      errorRate.add(1);
      console.log(`Single request failed: ${response.status} - ${response.body}`);
    }
  }
  
  // Small random delay to simulate realistic user behavior
  sleep(Math.random() * 0.5);
}

export function handleSummary(data) {
  return {
    'summary.json': JSON.stringify(data, null, 2),
    stdout: `
========== Performance Test Summary ==========
Total Requests: ${data.metrics.http_reqs.count}
Failed Requests: ${data.metrics.http_req_failed.count} (${(data.metrics.http_req_failed.rate * 100).toFixed(2)}%)
Average Response Time: ${data.metrics.http_req_duration.avg.toFixed(2)}ms
95th Percentile: ${data.metrics.http_req_duration['p(95)'].toFixed(2)}ms
Max Response Time: ${data.metrics.http_req_duration.max.toFixed(2)}ms
Requests/sec: ${data.metrics.http_req_rate ? data.metrics.http_req_rate.rate.toFixed(2) : 'N/A'}

Thresholds:
- 95th percentile < 500ms: ${data.metrics.http_req_duration['p(95)'] < 500 ? '✅ PASS' : '❌ FAIL'}
- Error rate < 1%: ${data.metrics.http_req_failed.rate < 0.01 ? '✅ PASS' : '❌ FAIL'}
============================================
    `,
  };
}