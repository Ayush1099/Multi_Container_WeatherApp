using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text;

namespace WeatherApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherDbContext _context;
        private readonly IDistributedCache _cache;

        public WeatherController(WeatherDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Weather>>> GetWeather()
        {
            var cacheKey = "weather_all";

            // Try Redis cache first
            var cachedData = await _cache.GetAsync(cacheKey);
            if (cachedData != null)
            {
                var json = Encoding.UTF8.GetString(cachedData);
                return Ok(JsonSerializer.Deserialize<List<Weather>>(json));
            }

            // Otherwise load from DB
            var result = await _context.WeatherEntries.ToListAsync();

            // Save to cache
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            };
            var serialized = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));
            await _cache.SetAsync(cacheKey, serialized, options);

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Weather>> CreateWeather(Weather weather)
        {
            _context.WeatherEntries.Add(weather);
            await _context.SaveChangesAsync();

            // Clear cache after insert
            await _cache.RemoveAsync("weather_all");

            return CreatedAtAction(nameof(GetWeather), new { id = weather.Id }, weather);
        }
    }
}
