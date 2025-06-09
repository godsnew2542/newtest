using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Shared
{
    public partial class NavManager
    {
        [Parameter]
        public EventCallback<RoleModel> SetChildData { get; set; }

        public RoleModel Role { get; set; } = new();

        public void SetCurrentData(RoleModel value)
        {
            Role = value;
            SetChildData.InvokeAsync(Role);
        }
    }
}
