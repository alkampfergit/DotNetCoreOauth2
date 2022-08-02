namespace WebAppTest.Support
{
    public static class ConfigurationHelper
    {
        public static T ConfigureSetting<T>(
            IServiceCollection services,
            IConfiguration configuration,
            string section) where T : class, new()
        {
            services.Configure<T>(configuration.GetSection(section));
            var setting = new T();
            configuration.Bind(section, setting);
            return setting;
        }
    }
}
