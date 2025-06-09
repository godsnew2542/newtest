using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Shared
{
    public partial class NavAdminUni
    {
        [Parameter]
        public EventCallback<RoleModel> SetChildData { get; set; }

        private RoleModel Role { get; set; } = new();

        public void SetCurrentData(RoleModel value)
        {
            Role = value;
            SetChildData.InvokeAsync(Role);
        }
    }
}
