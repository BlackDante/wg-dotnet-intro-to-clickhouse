using EventCollector.API.DTOs;
using EventCollector.API.Messages;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

// Configure MassTransit with Kafka
builder.Services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });

    x.AddRider(rider =>
    {
        rider.AddProducer<EventMessage>("events");
        rider.AddProducer<TaxiTripMessage>("taxi-trips");

        rider.UsingKafka((context, k) =>
        {
            k.Host("localhost:9092");

            k.TopicEndpoint<EventMessage>("events", "event-collector-consumer", e =>
            {
                e.CreateIfMissing(t =>
                {
                    t.NumPartitions = 3;
                    t.ReplicationFactor = 1;
                });
            });

            k.TopicEndpoint<TaxiTripMessage>("taxi-trips", "taxi-trip-consumer", e =>
            {
                e.CreateIfMissing(t =>
                {
                    t.NumPartitions = 6;
                    t.ReplicationFactor = 1;
                });
            });
        });
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "EventCollector API V1");
    });
}

app.UseHttpsRedirection();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithSummary("Health check endpoint");

// Single event endpoint
app.MapPost("/api/events", async (EventDto eventDto, ITopicProducer<EventMessage> producer) =>
{
    try
    {
        var eventId = Guid.NewGuid().ToString();
        
        var eventMessage = new EventMessage
        {
            EventId = eventId,
            EventType = eventDto.EventType,
            Source = eventDto.Source,
            Data = eventDto.Data,
            Timestamp = eventDto.Timestamp,
            CorrelationId = eventDto.CorrelationId,
            UserId = eventDto.UserId,
            Metadata = eventDto.Metadata
        };

        await producer.Produce(eventMessage);

        return Results.Ok(new EventResponse
        {
            EventId = eventId,
            Status = "Accepted",
            Message = "Event successfully queued for processing"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Event Processing Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("CreateEvent")
.WithSummary("Create a single event")
.WithDescription("Accepts a single event and publishes it to the Kafka stream");

// Batch events endpoint
app.MapPost("/api/events/batch", async (BatchEventDto batchDto, ITopicProducer<EventMessage> producer) =>
{
    try
    {
        var responses = new List<EventResponse>();
        var batchId = batchDto.BatchId ?? Guid.NewGuid().ToString();

        foreach (var eventDto in batchDto.Events)
        {
            var eventId = Guid.NewGuid().ToString();
            
            var eventMessage = new EventMessage
            {
                EventId = eventId,
                EventType = eventDto.EventType,
                Source = eventDto.Source,
                Data = eventDto.Data,
                Timestamp = eventDto.Timestamp,
                CorrelationId = eventDto.CorrelationId,
                UserId = eventDto.UserId,
                Metadata = eventDto.Metadata ?? new Dictionary<string, string>()
            };

            // Add batch ID to metadata
            eventMessage.Metadata["BatchId"] = batchId;

            await producer.Produce(eventMessage);

            responses.Add(new EventResponse
            {
                EventId = eventId,
                Status = "Accepted",
                Message = "Event successfully queued for processing"
            });
        }

        return Results.Ok(new
        {
            BatchId = batchId,
            TotalEvents = responses.Count,
            Events = responses
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Batch Event Processing Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("CreateBatchEvents")
.WithSummary("Create multiple events in batch")
.WithDescription("Accepts multiple events and publishes them to the Kafka stream");

// Event types endpoint for documentation
app.MapGet("/api/events/types", () =>
{
    return Results.Ok(new
    {
        SupportedEventTypes = new[]
        {
            "user.created",
            "user.updated",
            "user.deleted",
            "order.placed",
            "order.fulfilled",
            "order.cancelled",
            "payment.processed",
            "payment.failed",
            "system.error",
            "custom.event"
        },
        EventSchema = new
        {
            EventType = "string (required)",
            Source = "string (required)",
            Data = "object (required)",
            Timestamp = "datetime (optional, defaults to UTC now)",
            CorrelationId = "string (optional)",
            UserId = "string (optional)",
            Metadata = "dictionary<string, string> (optional)"
        }
    });
})
.WithName("GetEventTypes")
.WithSummary("Get supported event types and schema")
.WithDescription("Returns information about supported event types and the event schema");

// =============== TAXI-SPECIFIC ENDPOINTS ===============

// Single taxi trip endpoint
app.MapPost("/api/taxi/trips", async (TaxiEventDto tripEvent, ITopicProducer<TaxiTripMessage> producer) =>
{
    try
    {
        var eventId = Guid.NewGuid().ToString();
        
        var tripMessage = new TaxiTripMessage
        {
            eventId = eventId,
            eventType = tripEvent.eventType,
            source = tripEvent.source,
            timestamp = tripEvent.timestamp,
            
            // Map trip data to ClickHouse schema
            VendorID = tripEvent.tripData.VendorID,
            tpep_pickup_datetime = tripEvent.tripData.tpep_pickup_datetime,
            tpep_dropoff_datetime = tripEvent.tripData.tpep_dropoff_datetime,
            passenger_count = tripEvent.tripData.passenger_count,
            trip_distance = tripEvent.tripData.trip_distance,
            RatecodeID = tripEvent.tripData.RatecodeID,
            store_and_fwd_flag = tripEvent.tripData.store_and_fwd_flag,
            PULocationID = tripEvent.tripData.PULocationID,
            DOLocationID = tripEvent.tripData.DOLocationID,
            payment_type = tripEvent.tripData.payment_type,
            fare_amount = tripEvent.tripData.fare_amount,
            extra = tripEvent.tripData.extra,
            mta_tax = tripEvent.tripData.mta_tax,
            tip_amount = tripEvent.tripData.tip_amount,
            tolls_amount = tripEvent.tripData.tolls_amount,
            improvement_surcharge = tripEvent.tripData.improvement_surcharge,
            total_amount = tripEvent.tripData.total_amount,
            congestion_surcharge = tripEvent.tripData.congestion_surcharge,
            trip_type = tripEvent.tripData.trip_type,
            ehail_fee = tripEvent.tripData.ehail_fee,
            
            // Event metadata
            correlationId = tripEvent.correlationId,
            driverId = tripEvent.driverId,
            vehicleId = tripEvent.vehicleId,
            metadata = tripEvent.metadata
        };

        await producer.Produce(tripMessage);

        return Results.Ok(new EventResponse
        {
            EventId = eventId,
            Status = "Accepted",
            Message = "Taxi trip event successfully queued for processing"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Taxi Trip Event Processing Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("CreateTaxiTrip")
.WithSummary("Create a taxi trip event")
.WithDescription("Accepts a taxi trip event and publishes it to the Kafka taxi-trips stream");

// Batch taxi trips endpoint
app.MapPost("/api/taxi/trips/batch", async (BatchTaxiEventDto batchDto, ITopicProducer<TaxiTripMessage> producer) =>
{
    try
    {
        var responses = new List<EventResponse>();
        var batchId = batchDto.batchId ?? Guid.NewGuid().ToString();

        foreach (var tripEvent in batchDto.trips)
        {
            var eventId = Guid.NewGuid().ToString();
            
            var tripMessage = new TaxiTripMessage
            {
                eventId = eventId,
                eventType = tripEvent.eventType,
                source = tripEvent.source,
                timestamp = tripEvent.timestamp,
                
                // Map trip data to ClickHouse schema
                VendorID = tripEvent.tripData.VendorID,
                tpep_pickup_datetime = tripEvent.tripData.tpep_pickup_datetime,
                tpep_dropoff_datetime = tripEvent.tripData.tpep_dropoff_datetime,
                passenger_count = tripEvent.tripData.passenger_count,
                trip_distance = tripEvent.tripData.trip_distance,
                RatecodeID = tripEvent.tripData.RatecodeID,
                store_and_fwd_flag = tripEvent.tripData.store_and_fwd_flag,
                PULocationID = tripEvent.tripData.PULocationID,
                DOLocationID = tripEvent.tripData.DOLocationID,
                payment_type = tripEvent.tripData.payment_type,
                fare_amount = tripEvent.tripData.fare_amount,
                extra = tripEvent.tripData.extra,
                mta_tax = tripEvent.tripData.mta_tax,
                tip_amount = tripEvent.tripData.tip_amount,
                tolls_amount = tripEvent.tripData.tolls_amount,
                improvement_surcharge = tripEvent.tripData.improvement_surcharge,
                total_amount = tripEvent.tripData.total_amount,
                congestion_surcharge = tripEvent.tripData.congestion_surcharge,
                trip_type = tripEvent.tripData.trip_type,
                ehail_fee = tripEvent.tripData.ehail_fee,
                
                // Event metadata
                correlationId = tripEvent.correlationId,
                driverId = tripEvent.driverId,
                vehicleId = tripEvent.vehicleId,
                metadata = tripEvent.metadata ?? new Dictionary<string, string>()
            };

            // Add batch ID to metadata
            tripMessage.metadata["BatchId"] = batchId;

            await producer.Produce(tripMessage);

            responses.Add(new EventResponse
            {
                EventId = eventId,
                Status = "Accepted",
                Message = "Taxi trip event successfully queued for processing"
            });
        }

        return Results.Ok(new
        {
            BatchId = batchId,
            TotalTrips = responses.Count,
            ProcessingTime = batchDto.processingTime,
            Events = responses
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Batch Taxi Trip Processing Failed",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
.WithName("CreateBatchTaxiTrips")
.WithSummary("Create multiple taxi trip events in batch")
.WithDescription("Accepts multiple taxi trip events and publishes them to the Kafka taxi-trips stream");

// Taxi event types endpoint
app.MapGet("/api/taxi/types", () =>
{
    return Results.Ok(new
    {
        SupportedTaxiEventTypes = new[]
        {
            "trip.started",
            "trip.completed", 
            "trip.cancelled",
            "trip.payment_processed",
            "trip.payment_failed",
            "driver.signed_in",
            "driver.signed_out",
            "vehicle.maintenance_required"
        },
        TaxiTripSchema = new
        {
            VendorID = "int (required) - 1=Creative Mobile, 2=VeriFone",
            tpep_pickup_datetime = "datetime (required)",
            tpep_dropoff_datetime = "datetime (required)",
            passenger_count = "int (required)",
            trip_distance = "decimal (required)",
            RatecodeID = "int (required) - 1=Standard, 2=JFK, 3=Newark, 4=Nassau/Westchester, 5=Negotiated, 6=Group ride",
            store_and_fwd_flag = "char (required) - Y/N",
            PULocationID = "int (required) - Pickup location ID",
            DOLocationID = "int (required) - Dropoff location ID", 
            payment_type = "int (required) - 1=Credit card, 2=Cash, 3=No charge, 4=Dispute, 5=Unknown, 6=Voided trip",
            fare_amount = "decimal (required)",
            extra = "decimal (required) - $0.50 and $1 rush hour and overnight charges",
            mta_tax = "decimal (required) - $0.50 MTA tax",
            tip_amount = "decimal (required)",
            tolls_amount = "decimal (required)",
            improvement_surcharge = "decimal (required) - $0.30",
            total_amount = "decimal (required)",
            congestion_surcharge = "decimal (optional) - $2.50 for trips in Manhattan",
            trip_type = "int (optional) - 1=Street-hail, 2=Dispatch",
            ehail_fee = "decimal (optional)"
        },
        ExamplePayload = new
        {
            eventType = "trip.completed",
            source = "taxi-mobile-app",
            tripData = new
            {
                VendorID = 2,
                tpep_pickup_datetime = "2024-01-15T09:30:00Z",
                tpep_dropoff_datetime = "2024-01-15T09:45:00Z",
                passenger_count = 1,
                trip_distance = 2.5m,
                RatecodeID = 1,
                store_and_fwd_flag = 'N',
                PULocationID = 142,
                DOLocationID = 236,
                payment_type = 1,
                fare_amount = 12.50m,
                extra = 0.50m,
                mta_tax = 0.50m,
                tip_amount = 2.50m,
                tolls_amount = 0.00m,
                improvement_surcharge = 0.30m,
                total_amount = 16.30m,
                congestion_surcharge = 2.50m
            },
            driverId = "D12345",
            vehicleId = "V67890",
            correlationId = "trip-abc-123"
        }
    });
})
.WithName("GetTaxiEventTypes")
.WithSummary("Get supported taxi event types and schema")
.WithDescription("Returns information about taxi-specific event types and the taxi trip schema aligned with ClickHouse taxi_db.trips table");

app.Run();