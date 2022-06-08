#region License

// // Разработано: Коротенко Владимиром Николаевичем (Vladimir N. Korotenko)
// // email: koroten@ya.ru
// // skype:vladimir-korotenko
// // https://vkorotenko.ru
// // Создано:   08:06:2022 6:59 AM

#endregion

namespace VueRecaptcha.ViewModels;

public class UploadModel
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string CaptchaResponse { get; set; } = "";
    public List<IFormFile> Files { get; set; } = new List<IFormFile>();
}