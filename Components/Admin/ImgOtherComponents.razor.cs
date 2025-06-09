using LoanApp.Model.Models;
using LoanApp.Model.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace LoanApp.Components.Admin
{
    public partial class ImgOtherComponents
    {
        [Parameter] public ImgOtherModel? imgOther { get; set; } = null;

        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] IOptions<FileUploadSetting> fileUploadSetting { get; set; } = null!;
    }
}
