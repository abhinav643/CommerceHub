# Commerce Hub Microservice

This project implements a focused Commerce Hub backend microservice using .NET 8, MongoDB, and RabbitMQ. The service manages orders and inventory with concurrency-safe atomic updates and publishes events for order creation.

---

## Features Implemented

### Core API Endpoints

POST /api/orders/checkout  
- Validates order input
- Checks product stock
- Atomically decrements inventory
- Creates order record
- Publishes OrderCreated event to RabbitMQ

GET /api/orders/{id}  
- Retrieves order by ID
- Returns 404 if not found

PUT /api/orders/{id}  
- Idempotent full order replacement
- Updates blocked if order is already shipped

PATCH /api/products/{id}/stock  
- Direct inventory adjustment
- Uses atomic operations to prevent negative stock

---

## Concurrency Safety

Stock updates use MongoDB guarded atomic operations:

- Single update command with stock availability check
- Prevents race conditions under concurrent requests
- Ensures inventory never becomes negative

---

## Messaging

RabbitMQ is used for event publishing:

Exchange: commercehub.events  
Routing Key: orders.created  

OrderCreated events are published only after successful order creation.

---
## Unit Testing

nUnit tests cover:

- Validation logic (negative quantities rejected)
- Correct stock decrement behavior
- Event emission verification

Run tests:

dotnet test

---

## Running the Project

Start all services with one command:

docker compose up --build

Services will be available at:

Swagger UI: http://localhost:8080/swagger  
RabbitMQ UI: http://localhost:15672  

Login: guest / guest

---

## Seeding Sample Data

Seed products using Mongo shell:

docker exec -it commercehub-mongo mongosh

use commercehub

db.Products.insertMany([
  { sku: "P1", name: "Phone", stock: 10, price: NumberDecimal("500.00"), updatedAtUtc: new Date() },
  { sku: "P2", name: "Laptop", stock: 5, price: NumberDecimal("1200.00"), updatedAtUtc: new Date() }
])

---

## Technologies Used

.NET 8 Web API  
MongoDB  
RabbitMQ  
Docker Compose  
nUnit  
