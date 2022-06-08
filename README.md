# Создание приложения NET 6 + VUE с защитой reCaptcha
## Введение

Данный шаблон поможет вам добавить защиту формы с помощью 
Google reCaptcha в ваше SPA приложение. 
Кроме того, используется прокси пакет для одновременного запуска SPA и .NET приложений.
Данная статья основана на статье [Adding CAPTCHA on form posts with ASP.NET Core](https://blog.elmah.io/adding-captcha-on-form-posts-with-asp-net-core/)
Если вам нужна защита на Razor pages то используйте ее.

## Создание .NET приложения
Я использую **Microsoft Visual Studio 2022** в случае использования других версий экраны и код будут немного отличатся.
Создайте проект **ASP .NET Core Web API**
![Создайте проект ASP .NET Core Web API](https://github.com/vkorotenko/VueRecaptcha/blob/master/images/createproject.PNG?raw=true)

Выберите подходящее имя

![Выберите подходящее имя](https://github.com/vkorotenko/VueRecaptcha/blob/master/images/configurename.PNG?raw=true)

И настройте опции как на этом экране, впрочем, это мой выбор для упрощения тестового проекта.

![Настройки проекта](https://github.com/vkorotenko/VueRecaptcha/blob/master/images/projectoption.PNG?raw=true)


Далее удалите лишние файлы, созданные мастером.
Добавьте следующие пакеты:
* Microsoft.AspNetCore.SpaServices.Extensions
* Newtonsoft.Json

Файл **Program.cs** после правок
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

Файл **ReCaptcha.cs**
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
 
 Файл **UploadModel.cs**
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
## Создание SPA приложения
В папке .NET приложения выполните команду

```vue create clientapp```

укажите что используете **vue 3**

перейдите в папку clientapp

добавьте поддержку **typescript**

```vue add typescript```

Установите пакеты
```npm i -S axios```

настройте проект
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
    <div v-if="uploaded">Файлы загружены</div>
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
    // добавляем в форму поля
    formData.append("email", this.email);
    formData.append("name", this.name);
    // получаем значение капчи
    const resp = (
      document.getElementsByName(
        "g-recaptcha-response"
      )[0] as HTMLTextAreaElement
    ).value;
    formData.append("captchaResponse", resp);
    // поле загрузки файлов
    var imagefile = document.getElementById("upload_files") as HTMLInputElement;
    if (imagefile == null) return;
    if (imagefile.files == null) return;
    const files = imagefile.files;
    for (let i = 0; i < files.length; i++) {
      // добавляем в форму выбранные файлы
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
    // я сделал так что бы все настройки хранились на сервере, если не нравится лишний запрос то захардкодьте это значение
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

## Получение своих ключей
Войдите в свой аккаунт reCAPTCHA  
[https://www.google.com/recaptcha/about/](https://www.google.com/recaptcha/about/) 
Добавьте новый сайт, для целей локального тестирования не забудьте добавить
localhost
Выберите reCAPTCHA v2 
Site Key и Secret Key
скопируйте в файл **appsettings.json**

И можно запускать отладку приложения в Visual Studio.

## Заключение
В итоге мы научились загружать несколько файлов из SPA приложения на бэкенд с проверкой капчей.
Успехов вам и меньше спэма! [Владимир Коротенко](https://vkorotenko.ru)

## Исходный код
[https://github.com/vkorotenko/VueRecaptcha](https://github.com/vkorotenko/VueRecaptcha)






