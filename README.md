# 🌦️ Docker + Redis Multi-Container .NET App

Built a multi-container .NET application using Docker to validate a production-ready setup, with Docker images published to GitHub Container Registry (GHCR).
---

## 🚀 Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running  
- GitHub account with access to the private image *(if private)*  

---

## 📌 GET /api/weather (GetWeather) Flow
```
1️⃣ Client sends a GET request → /api/weather
2️⃣ API tries to read Redis key: "weather_all"
    - If found:
        ✔ Deserialize JSON → Return data to client (FAST)
    - If not found OR Redis unavailable:
        ❌ Cache miss → Fetch from database

3️⃣ Store DB result in Redis:
    - Key: "weather_all"
    - Value: JSON string of all weather data
    - Expiry: 1 minute

4️⃣ Return data to client
```
## 📌 POST /api/weather (CreateWeather) Flow
```
1️⃣ Client sends POST request with multiple weather records.

2️⃣ Duplicate check:
    a) Try to read Redis Set: "weather_keys" (contains only City_Date strings)
        - If found (not empty):
            ✔ Use it to filter out duplicates quickly.
        - If empty:
            🔄 Load all City_Date from DB → Add them to Redis Set.
        - If Redis is down:
            📥 Load all City_Date from DB into memory.

3️⃣ Filter input list:
    - Remove any records whose City_Date already exists.
    - If no new records remain → Return "No new records to insert."

4️⃣ Insert new records into DB.

5️⃣ Update Redis Set ("weather_keys") with new City_Date entries (if Redis available).

6️⃣ Invalidate Redis key "weather_all":
    - Remove it so next GET request will fetch fresh data from DB.

7️⃣ Return "{X} new records inserted."
```
## Diagram
```
[Client POST] → Check Redis("weather_keys")
      | Found & has data? → Filter duplicates
      | Empty? → Load City_Date from DB → Save to Redis Set
      | Redis down? → Load City_Date from DB into memory

Filter input → Keep only new records
      | None left? → Return "No new records"
      v
Insert into DB
      |
Update Redis("weather_keys") with new City_Date
      |
Remove Redis("weather_all") → Forces refresh on next GET
      v
Return "{X} new records inserted"

```

## 🐳 Option 1: Run with Docker Commands

### 1. Create a Docker network
```
docker network create weather-net
```

### 2. Login to GitHub Container Registry
```
echo YOUR_PAT | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```
-Replace YOUR_PAT → your GitHub PAT (must include read:packages scope)
-Replace YOUR_GITHUB_USERNAME → your GitHub username

### 3. Run Redis container
```
docker run -d --name redisCache --network weather-net -p 6379:6379 redis
```
-This runs Redis inside the weather-net network.
### 4. Pull WeatherApp image from GHCR
```
docker pull ghcr.io/ayush1099/weatherapp:latest
```
### 5. Run WeatherApp container
```
docker run -d --name weatherapp --network weather-net -p 5000:8080 -e ASPNETCORE_ENVIRONMENT=Development ghcr.io/ayush1099/weatherapp:latest
```
-Runs the app on http://localhost:5000
-Exposes Swagger in Development mode

## 🐳 Option 2: Run with Docker Compose
-Create a file named docker-compose.yml in your project folder with this content:
```
version: '3.9'
services:
  redisCache:
    image: redis:latest
    container_name: redisCache
    ports:
      - "6379:6379"
    networks:
      - weather-net

  weatherapp:
    image: ghcr.io/ayush1099/weatherapp:latest
    container_name: weatherapp
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - redisCache
    networks:
      - weather-net

networks:
  weather-net:
    driver: bridge
```
### Then run:
```
docker-compose up -d
```
To stop everything:
```
docker-compose down
```

## 🌍 Access the Application
Swagger UI → http://localhost:5000/swagger

## 🛑 Stopping Everything (manual mode)
```
docker rm -f weatherapp redisCache
docker network rm weather-net
```

## 🔧 Notes
If the image is public → no login needed.
If the image is private → testers need read access to the GHCR package.
Container names (redisCache, weatherapp) must match the connection string used in the app.
Docker Compose is recommended since it simplifies setup to a single command.





