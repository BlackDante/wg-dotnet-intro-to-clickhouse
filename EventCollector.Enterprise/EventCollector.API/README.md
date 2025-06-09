# EventCollector API

A minimal ASP.NET Core API for collecting events and taxi trip data, publishing them to Kafka using MassTransit.

## Features

### General Events
- **Single Event Collection**: POST `/api/events` - Accept single events
- **Batch Event Collection**: POST `/api/events/batch` - Accept multiple events in one request
- **Event Type Information**: GET `/api/events/types` - Get supported event types and schema

### Taxi-Specific Events (ClickHouse Integration)
- **Single Taxi Trip**: POST `/api/taxi/trips` - Accept taxi trip events aligned with ClickHouse taxi_db.trips schema
- **Batch Taxi Trips**: POST `/api/taxi/trips/batch` - Accept multiple taxi trips in one request
- **Taxi Schema Information**: GET `/api/taxi/types` - Get taxi-specific event types and schema

### System
- **Health Check**: GET `/health` - Service health endpoint
- **Swagger UI**: Available at `/swagger` in development mode

## Architecture

- **.NET 9.0** Minimal API
- **MassTransit** for message bus abstraction
- **Kafka** for event streaming
- **Swagger/OpenAPI** for documentation

## Schemas

### General Event Schema
```json
{
  "eventType": "string (required)",
  "source": "string (required)", 
  "data": "object (required)",
  "timestamp": "datetime (optional, defaults to UTC now)",
  "correlationId": "string (optional)",
  "userId": "string (optional)",
  "metadata": "dictionary<string, string> (optional)"
}
```

### Taxi Trip Event Schema (Aligned with ClickHouse taxi_db.trips)
```json
{
  "eventType": "trip.completed",
  "source": "taxi-mobile-app",
  "tripData": {
    "VendorID": 2,
    "tpep_pickup_datetime": "2024-01-15T09:30:00Z",
    "tpep_dropoff_datetime": "2024-01-15T09:45:00Z",
    "passenger_count": 1,
    "trip_distance": 2.5,
    "RatecodeID": 1,
    "store_and_fwd_flag": "N",
    "PULocationID": 142,
    "DOLocationID": 236,
    "payment_type": 1,
    "fare_amount": 12.50,
    "extra": 0.50,
    "mta_tax": 0.50,
    "tip_amount": 2.50,
    "tolls_amount": 0.00,
    "improvement_surcharge": 0.30,
    "total_amount": 16.30,
    "congestion_surcharge": 2.50
  },
  "driverId": "D12345",
  "vehicleId": "V67890",
  "correlationId": "trip-abc-123"
}
```

## Dependencies

- **MassTransit 8.4.1** - Message bus abstraction
- **MassTransit.Kafka 8.4.1** - Kafka integration
- **Swashbuckle.AspNetCore 8.1.4** - Swagger UI

## Configuration

Configure Kafka connection in `appsettings.json`:

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topics": {
      "Events": "events",
      "TaxiTrips": "taxi-trips"
    },
    "ConsumerGroups": {
      "EventCollector": "event-collector-consumer",
      "TaxiTripProcessor": "taxi-trip-consumer"
    }
  }
}
```

## Kafka Topics

- **events**: General application events (3 partitions)
- **taxi-trips**: Taxi trip events aligned with ClickHouse schema (6 partitions)

## Running

```bash
dotnet run
```

API will be available at `https://localhost:5001` and `http://localhost:5000`