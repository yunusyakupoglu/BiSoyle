# 🎯 BiSoyle - Microservice Architecture

Microservice tabanlı modern pastane yönetim sistemi.

## 🚀 Hızlı Başlatma

### Backend Servisleri
```powershell
# Gateway
cd gateway && dotnet run

# Receipt Service (Port 5001)
cd services/receipt-service && dotnet run

# Product Service (Port 5002)
cd services/product-service && dotnet run

# Transaction Service (Port 5003)
cd services/transaction-service && dotnet run
```

### Frontend
```powershell
cd frontend/Admin && npm start
```

## 📊 Servis Portları

| Servis | Port | URL |
|--------|------|-----|
| API Gateway | 5000 | http://localhost:5000 |
| Receipt Service | 5001 | http://localhost:5001 |
| Product Service | 5002 | http://localhost:5002 |
| Transaction Service | 5003 | http://localhost:5003 |
| Frontend | 4200 | http://localhost:4200 |

## 🧪 API Endpoints

### API Gateway (Port 5000)
```
GET  http://localhost:5000/api/v1/products
GET  http://localhost:5000/api/v1/transactions
POST http://localhost:5000/api/v1/receipt/print
```

### Microservices
```
# Receipt Service (Port 5001)
POST /api/receipt/print
WS   /hub/receipt (SignalR)

# Product Service (Port 5002)
GET  /api/products

# Transaction Service (Port 5003)
GET  /api/transactions
```

## 📝 Swagger Documentation
- http://localhost:5000/swagger
- http://localhost:5001/swagger
- http://localhost:5002/swagger
- http://localhost:5003/swagger

## 🏗️ Mimari

```
┌─────────────┐
│   Frontend  │  (Angular 17, Port 4200)
│   :4200     │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Gateway   │  (API Gateway, Port 5000)
│   :5000     │
└──────┬──────┘
       │
    ┌──┴──┬─────────┬────────┐
    ▼     ▼         ▼        ▼
┌────┐  ┌────┐   ┌────┐   ┌────────┐
│RCPT│  │PRDT│   │TRAN│   │ Voice  │
│:5001│  │:5002│   │:5003│  │:8765 │
└────┘  └────┘   └────┘   └────────┘
```

## 📦 Teknolojiler

### Backend
- .NET 8.0 (C#)
- ASP.NET Core Web API
- SignalR (Real-time)
- Entity Framework Core
- PostgreSQL/SQLite

### Frontend
- Angular 17
- Bootstrap 5
- NgRx (State Management)
- SignalR Client

### Infrastructure
- RabbitMQ (Message Queue)
- Docker Compose
- Swagger UI

## 📂 Proje Yapısı

```
BiSoyle/
├── gateway/              # API Gateway (Port 5000)
├── services/
│   ├── receipt-service/   # Receipt Service (Port 5001)
│   ├── product-service/    # Product Service (Port 5002)
│   ├── transaction-service/ # Transaction Service (Port 5003)
│   └── voice-service/     # Voice Service (Python, Port 8765)
├── frontend/
│   └── Admin/            # Angular Frontend (Port 4200)
├── shared/               # Shared libraries
└── docker-compose.yml     # Docker configuration
```

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.
