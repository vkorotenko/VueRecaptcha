using Newtonsoft.Json.Linq;

namespace VueRecaptcha.Services
{
    public class ReCaptcha
    {
        private readonly HttpClient _captchaClient;
        private readonly ILogger<ReCaptcha> _logger;
        private readonly IConfiguration _configuration;
        public ReCaptcha(HttpClient captchaClient, ILogger<ReCaptcha> logger, IConfiguration config)
        {
            _captchaClient = captchaClient;
            _logger = logger;
            _configuration = config;
        }

        public string GetClientMarkup()
        {
            var siteKey = _configuration.GetValue<string>("SiteKey");
            return siteKey;
        }
        public async Task<bool> IsValid(string captcha)
        {
            try
            {
                var secretKey = _configuration.GetValue<string>("SecretKey");
                var postTask = await _captchaClient
                    .PostAsync($"?secret={secretKey}&response={captcha}", new StringContent(""));
                var result = await postTask.Content.ReadAsStringAsync();
                var resultObject = JObject.Parse(result);
                dynamic success = resultObject["success"];
                return (bool)success;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to validate",e);
                return false;
            }
        }
    }
}
