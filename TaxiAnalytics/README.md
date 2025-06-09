# NYC Taxi Analytics Dashboard

A simple ASP.NET Core MVC web application that visualizes NYC taxi trip data from either ClickHouse or PostgreSQL databases.

## Features

- Real-time database switching between ClickHouse and PostgreSQL using query string parameter
- 6 different analytical visualizations:
  - Top 10 Busiest Days
  - Payment Type Distribution
  - Hourly Trip Patterns
  - Trip Distance Distribution
  - Revenue by Vendor
  - Cumulative Stats (First 30 Days)
- Interactive charts using Chart.js
- Responsive Bootstrap layout

## Prerequisites

- .NET 9.0 SDK
- Docker (for running ClickHouse and PostgreSQL)
- The databases should be running with taxi data loaded

## Configuration

The application expects the databases to be available at:
- ClickHouse: `localhost:9000` (database: `taxi_db`)
- PostgreSQL: `localhost:5432` (database: `demo_db`)

Connection strings are configured in `appsettings.json`.

## Running the Application

1. Navigate to the project directory:
   ```bash
   cd TaxiAnalytics/TaxiAnalytics.Web
   ```

2. Build the application:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Open your browser and navigate to:
   - ClickHouse: `https://localhost:5001/Dashboard?db=clickhouse`
   - PostgreSQL: `https://localhost:5001/Dashboard?db=postgresql`

## Database Switching

You can switch between databases by using the `db` query parameter:
- `?db=clickhouse` - Uses ClickHouse database
- `?db=postgresql` - Uses PostgreSQL database

The dashboard includes buttons to easily switch between databases.

## Project Structure

- `Models/` - Data models for taxi statistics
- `Services/` - Database service interfaces and implementations
- `Controllers/` - MVC controllers (DashboardController)
- `Views/Dashboard/` - Dashboard views with Chart.js visualizations
- `wwwroot/` - Static assets

## Technologies Used

- ASP.NET Core 9.0 MVC
- ClickHouse.Client
- Npgsql (PostgreSQL)
- Dapper (micro-ORM)
- Chart.js (data visualization)
- Bootstrap 5 (UI framework)