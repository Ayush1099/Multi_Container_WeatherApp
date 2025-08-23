using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace WeatherApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;

        private const string CacheKey_AllData = "weather_all";
        private const string CacheKey_UniqueSet = "weather_keys";

        public WeatherController(WeatherDbContext context, IDistributedCache cache, IConnectionMultiplexer redis)
        {
            _context = context;
            _cache = cache;
            _redis = redis;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Weather>>> GetWeather()
        {
            try
            {
                var cachedData = await _cache.GetAsync(CacheKey_AllData);
                if (cachedData != null)
                {
                    var json = Encoding.UTF8.GetString(cachedData);
                    return Ok(JsonSerializer.Deserialize<List<Weather>>(json));
                }
            }
            catch
            {
                Console.WriteLine("Redis unavailable, reading from DB.");
            }

            var result = await _context.WeatherEntries.ToListAsync();

            try
            {
                var serialized = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));
                await _cache.SetAsync(CacheKey_AllData, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });
            }
            catch
            {
                Console.WriteLine("Could not update Redis cache.");
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> CreateWeather(List<Weather> weatherList)
        {
            if (weatherList == null || weatherList.Count == 0)
                return BadRequest("No weather data provided.");

            var uniqueKeys = new HashSet<string>();
            bool redisAvailable = true;

            try
            {
                var db = _redis.GetDatabase();
                var redisKeys = await db.SetMembersAsync(CacheKey_UniqueSet);

                if (redisKeys.Length == 0)
                {
                    // Redis empty ? load from DB and populate set
                    var existing = await _context.WeatherEntries
                        .Select(w => $"{w.City}_{w.Date:yyyyMMdd}")
                        .ToListAsync();

                    foreach (var key in existing)
                        await db.SetAddAsync(CacheKey_UniqueSet, key);

                    uniqueKeys = existing.ToHashSet();
                }
                else
                {
                    uniqueKeys = redisKeys.Select(k => (string)k).ToHashSet();
                }
            }
            catch
            {
                Console.WriteLine("Redis not available, checking duplicates from DB.");
                redisAvailable = false;

                var existing = await _context.WeatherEntries
                    .Select(w => $"{w.City}_{w.Date:yyyyMMdd}")
                    .ToListAsync();

                uniqueKeys = existing.ToHashSet();
            }

            // Filter out duplicates
            var newRecords = weatherList
                .Where(w => !uniqueKeys.Contains($"{w.City}_{w.Date:yyyyMMdd}"))
                .ToList();

            if (newRecords.Count == 0)
                return Ok("No new records to insert.");

            _context.WeatherEntries.AddRange(newRecords);
            await _context.SaveChangesAsync();

            // Update Redis set
            if (redisAvailable)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    foreach (var w in newRecords)
                        await db.SetAddAsync(CacheKey_UniqueSet, $"{w.City}_{w.Date:yyyyMMdd}");
                }
                catch
                {
                    Console.WriteLine("Could not update Redis duplicate check set.");
                }
            }

            // Invalidate main cache (will rebuild on next GET)
            try
            {
                await _cache.RemoveAsync(CacheKey_AllData);
            }
            catch
            {
                Console.WriteLine("Could not invalidate Redis main cache.");
            }

            return Ok($"{newRecords.Count} new records inserted.");
        }

        [HttpGet("monitor-redis")]
        public IActionResult MonitorRedis()
        {
            try
            {
                var info = _redis.GetDatabase().Execute("INFO").ToString();
                return Ok(info);
            }
            catch
            {
                return StatusCode(500, "Redis is not available.");
            }
        }
    }
}
