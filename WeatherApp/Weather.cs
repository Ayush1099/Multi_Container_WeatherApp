namespace WeatherApp
{
    public class Weather
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public int TemperatureC { get; set; }
        public DateTime Date { get; set; }
    }
}
