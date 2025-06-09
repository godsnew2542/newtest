using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Components.Admin
{
    public partial class ImgStapComponents
    {
        [Parameter] public List<ListDocModel> ListDocModels { get; set; } = new();
    }
}