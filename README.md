# Temperature Prediction Service (Dockerized)

**.NET 7** REST API that predicts temperature by city and date.  
If no model exists on first run the service **trains a new model automatically** and stores it in an `experiments/` folder so that subsequent requests are fast.

---

## Quick Start – Run from Docker Hub

**Image:** `quantdevxx/temp-prediction-service:1.0`

```bash
# 1. Pull the image
docker pull quantdevxx/temp-prediction-service:1.0

# 2. Run (replace sk-… with your real OpenAI key)
docker run -d -p 8080:80   -e "ASPNETCORE_ENVIRONMENT=Development"   -e "OpenAI__ApiKey=sk-..."   -v "$(pwd)/TemperaturePredictionService.Api/experiments:/app/experiments"   --name temp-pred-svc   quantdevxx/temp-prediction-service:1.0
```

*On Windows PowerShell use `${PWD}` for the host path.*

Browse to **<http://localhost:8080/swagger>** to try the API.

---

## Clone & Run from Source

```bash
# Clone the repo
git clone https://github.com/iatrochemistx/temp-prediction-service.git
cd temp-prediction-service

# Restore & run locally
dotnet run --project TemperaturePredictionService.Api   --urls "http://localhost:8080"   --environment Development   -- OpenAI__ApiKey=sk-...
```

First run trains a model and saves it under `TemperaturePredictionService.Api/experiments/<timestamp>/model.zip`.

---

## API Test (cURL)

```bash
curl -X POST http://localhost:8080/predict_temperature   -H "Content-Type: application/json"   -d '{"city":"Dubai","date":"2025-06-08T00:00:00Z"}'
```

---

## Features

* **Auto‑train** on first run – no pre‑built model required.  
* **Persistent experiments** directory (mount with `-v`).  
* **Swagger/OpenAPI** docs at `/swagger`.  
* Clean separation of Core / Application / Infrastructure projects.

---

## Environment Variables

| Variable                 | Purpose                                   |
|--------------------------|-------------------------------------------|
| `ASPNETCORE_ENVIRONMENT` | Use `Development` to enable Swagger UI    |
| `OpenAI__ApiKey`         | **Required.** Your OpenAI API key         |

---

## Example docker‑compose.yml

```yaml
version: "3.8"
services:
  temp-prediction:
    image: quantdevxx/temp-prediction-service:1.0
    ports:
      - "8080:80"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      OpenAI__ApiKey: sk-...  # replace
    volumes:
      - ./TemperaturePredictionService.Api/experiments:/app/experiments
```

---

## Troubleshooting

* **`FileNotFoundException` for model** – ensure the `experiments/` folder is volume‑mounted.  
* **Swagger UI not showing** – run with `ASPNETCORE_ENVIRONMENT=Development`.  
* **OpenAI errors** – check the `OpenAI__ApiKey` value and quota.

---
