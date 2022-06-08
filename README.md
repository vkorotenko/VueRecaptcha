# �������� ���������� NET 6 + VUE � ������� reCaptcha
## ��������

������ ������ ������� ��� �������� ������ ����� � ������� 
Google reCaptcha � ���� SPA ����������. 
����� ����, ������������ ������ ����� ��� �������������� ������� SPA � .NET ����������.
������ ������ �������� �� ������ [Adding CAPTCHA on form posts with ASP.NET Core](https://blog.elmah.io/adding-captcha-on-form-posts-with-asp-net-core/)
���� ��� ����� ������ �� Razor pages �� ����������� ��.

## �������� .NET ����������
� ��������� **Microsoft Visual Studio 2022** � ������ ������������� ������ ������ ������ � ��� ����� ������� ���������.
�������� ������ **ASP .NET Core Web API**
![�������� ������ ASP .NET Core Web API](https://github.com/vkorotenko/VueRecaptcha/blob/master/images/createproject.PNG?raw=true)

�������� ���������� ���

![�������� ���������� ���](https://github.com/vkorotenko/VueRecaptcha/blob/master/images/configurename.PNG?raw=true)

� ��������� ����� ��� �� ���� ������, �������, ��� ��� ����� ��� ��������� ��������� �������.

![��������� �������](https://github.com/vkorotenko/VueRecaptcha/blob/master/images/projectoption.PNG?raw=true)


����� ������� ������ �����, ��������� ��������.
�������� ��������� ������:
* Microsoft.AspNetCore.SpaServices.Extensions
* Newtonsoft.Json

���� **Program.cs** ����� ������
```
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using VueRecaptcha.Services;

namespace VueRecaptcha
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddHttpClient<ReCaptcha>(x =>
            {
                x.BaseAddress = new Uri("https://www.google.com/recaptcha/api/siteverify");
            });
            builder.Services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";

            });
            builder.Services
                .AddMvc()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                });
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";
                spa.UseReactDevelopmentServer(npmScript: "serve");
            });
            app.Run();
        }
    }
}
```

���� **ReCaptcha.cs**
```
using Newtonsoft.Json.Linq;

namespace VueRecaptcha.Services;

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
```
 
 ���� **UploadModel.cs**
 ```
 namespace VueRecaptcha.ViewModels;

public class UploadModel
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string CaptchaResponse { get; set; } = "";
    public List<IFormFile> Files { get; set; } = new List<IFormFile>();
}
```
## �������� SPA ����������
� ����� .NET ���������� ��������� �������

```vue create clientapp```

������� ��� ����������� **vue 3**

��������� � ����� clientapp

�������� ��������� **typescript**

```vue add typescript```

���������� ������
```npm i -S axios```

��������� ������
**vue.config.js**
```
const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
  devServer: {    
    onAfterSetupMiddleware() { // Output the same message as the react dev server to get the Spa middleware working with vue.
      console.info("Starting the development server...");
    }
  },
  transpileDependencies: true
})
```

**HelloWorld.vue**
```
<template>
  <div class="hello">
    <div v-if="uploaded">����� ���������</div>
    <form class="uploadForm" v-else>
      <label for="email"
        >Email
        <input type="email" id="email" name="email" required v-model="email" />
      </label>
      <label for="name"
        >Name
        <input type="text" id="name" name="name" required v-model="name" />
      </label>
      <input type="file" multiple hidden id="upload_files" />
      <div class="g-recaptcha" :data-sitekey="siteKey"></div>
      <button @click="addFiles()">Add files</button>
      <button type="submit" @click.prevent="submit">Submit</button>
    </form>
  </div>
</template>
<script lang="ts">
import { Vue } from "vue-class-component";
import axios from "axios";

export default class HelloWorld extends Vue {  
  siteKey: string = "";
  email: string = "";
  name: string = "";
  uploaded: boolean = false;
  async submit() {    
    var formData = new FormData();
    // ��������� � ����� ����
    formData.append("email", this.email);
    formData.append("name", this.name);
    // �������� �������� �����
    const resp = (
      document.getElementsByName(
        "g-recaptcha-response"
      )[0] as HTMLTextAreaElement
    ).value;
    formData.append("captchaResponse", resp);
    // ���� �������� ������
    var imagefile = document.getElementById("upload_files") as HTMLInputElement;
    if (imagefile == null) return;
    if (imagefile.files == null) return;
    const files = imagefile.files;
    for (let i = 0; i < files.length; i++) {
      // ��������� � ����� ��������� �����
      formData.append("files", files[i]);
    }
    var result = await axios.post("/api/upload", formData);
    if (result) {
      console.log(result);
      this.uploaded = true;
    }
    return false;
  }
  addFiles() {
    const upload = document.getElementById("upload_files");
    upload?.click();    
  }
  async mounted() {
    // � ������ ��� ��� �� ��� ��������� ��������� �� �������, ���� �� �������� ������ ������ �� ������������ ��� ��������
    var captcha = await axios.get("/api/upload");
    this.siteKey = captcha.data.toString();
    this.createRecaptcha();
  }
  createRecaptcha() {
    var s = document.createElement("script");
    s.setAttribute("src", "https://www.google.com/recaptcha/api.js");
    s.setAttribute("async", "async");
    s.setAttribute("defer", "defer");
    document.body.appendChild(s);
  }
}
</script>

<style scoped>
a {
  color: #42b983;
}
.uploadForm {
  margin: 20px auto;
  width: 300px;
  display: block;
}
.uploadForm label {
  display: block;
  width: 100%;
  margin-bottom: 10px;
}
</style>

```

## ��������� ����� ������
������� � ���� ������� reCAPTCHA  
[https://www.google.com/recaptcha/about/](https://www.google.com/recaptcha/about/) 
�������� ����� ����, ��� ����� ���������� ������������ �� �������� ��������
localhost
�������� reCAPTCHA v2 
Site Key � Secret Key
���������� � ���� **appsettings.json**

� ����� ��������� ������� ���������� � Visual Studio.

## ����������
� ����� �� ��������� ��������� ��������� ������ �� SPA ���������� �� ������ � ��������� ������.
������� ��� � ������ �����! [�������� ���������](https://vkorotenko.ru)

## �������� ���
[https://github.com/vkorotenko/VueRecaptcha](https://github.com/vkorotenko/VueRecaptcha)






