# 🌦️ WeatherApp with Redis (Docker + GHCR)

This is a simple **ASP.NET Core Web API** application that uses **Redis** for caching.  
The app is containerized with Docker and the image is hosted on **GitHub Container Registry (GHCR)**.

---

## 🚀 Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop) installed and running  
- GitHub account with access to the private image *(if private)*  

---

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





