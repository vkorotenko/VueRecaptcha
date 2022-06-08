using Microsoft.AspNetCore.Mvc;
using VueRecaptcha.Services;
using VueRecaptcha.ViewModels;

namespace VueRecaptcha.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
        

    private readonly ILogger<UploadController> _logger;
    private readonly ReCaptcha _captcha;

    public UploadController(ILogger<UploadController> logger, ReCaptcha captcha)
    {
        _logger = logger;
        _captcha = captcha;
    }

    [HttpGet]
    public ActionResult<string> Get()
    {
        return _captcha.GetClientMarkup();
    }
    [HttpPost]
    public async Task<ActionResult<UploadResult>> Post([FromForm] UploadModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (string.IsNullOrWhiteSpace(model.CaptchaResponse)) 
            return BadRequest(new UploadResult { Message = "reCaptcha не может быть пустой."});
        
        
        if (!await _captcha.IsValid(model.CaptchaResponse))
            return BadRequest(new UploadResult { Message = "reCaptcha не пройдена." });
        foreach (var formFile in model.Files)
        {
            var path = "c:\\temp";
            if (formFile.Length <= 0) continue;
            var fp = Path.Combine(path, formFile.FileName);
            await using var stream = System.IO.File.Create(fp);
            await formFile.CopyToAsync(stream);
        }
        var result = new UploadResult{Message = "Файлы успешно загружены"};
        return result;
    }
}