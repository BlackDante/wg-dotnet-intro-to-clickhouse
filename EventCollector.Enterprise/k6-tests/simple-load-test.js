import http from 'k6/http';
import { check, sleep } from 'k6';

// Simple load test for quick testing
export const options = {
  stages: [
    { duration: '30s', target: 1000 },  // Ramp up to 20 users
    { duration: '1m', target: 1500 },   // Stay at 50 users
    { duration: '30s', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'],
    http_req_failed: ['rate<0.05'],
  },
};

const BASE_URL = 'http://localhost:5189';

// Simple taxi trip generator
function generateSimpleTaxiTrip() {
  const now = new Date();
  const pickupTime = new Date(now.getTime() - Math.random() * 3600000);
  const dropoffTime = new Date(pickupTime.getTime() + (Math.random() * 30 + 10) * 60000);
  
  return {
    eventType: "trip.completed",
    source: "k6-load-test",
    tripData: {
      VendorID: Math.random() > 0.5 ? 1 : 2,
      tpep_pickup_datetime: pickupTime.toISOString(),
      tpep_dropoff_datetime: dropoffTime.toISOString(),
      passenger_count: 1,
      trip_distance: Math.round((Math.random() * 10 + 1) * 100) / 100,
      RatecodeID: 1,
      store_and_fwd_flag: "N",
      PULocationID: Math.floor(Math.random() * 200) + 1,
      DOLocationID: Math.floor(Math.random() * 200) + 1,
      payment_type: Math.random() > 0.7 ? 1 : 2,
      fare_amount: Math.round((Math.random() * 20 + 5) * 100) / 100,
      extra: 0,
      mta_tax: 0.50,
      tip_amount: Math.round((Math.random() * 5) * 100) / 100,
      tolls_amount: 0,
      improvement_surcharge: 0.30,
      total_amount: 0,
      congestion_surcharge: Math.random() > 0.5 ? 2.50 : 0
    },
    correlationId: `k6-test-${__VU}-${__ITER}`
  };
}

export default function () {
  const trip = generateSimpleTaxiTrip();
  
  // Calculate total amount
  trip.tripData.total_amount = Math.round((
    trip.tripData.fare_amount +
    trip.tripData.extra +
    trip.tripData.mta_tax +
    trip.tripData.tip_amount +
    trip.tripData.tolls_amount +
    trip.tripData.improvement_surcharge +
    (trip.tripData.congestion_surcharge || 0)
  ) * 100) / 100;
  
  const payload = JSON.stringify(trip);
  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };
  
  const response = http.post(`${BASE_URL}/api/taxi/trips`, payload, params);
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
  
  sleep(1); // 1 second between requests
}