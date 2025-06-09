using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Shared
{
    public partial class NavUser
    {
        [CascadingParameter]
        private UserStateProvider? userStateProvider { get; set; }

        [Parameter]
        public EventCallback<RoleModel> SetChildData { get; set; }

        private RoleModel Role { get; set; } = new();
        private List<string?> Dev { get; } = new() { "watcharapon.n", "naparat.h" };

        public void SetCurrentData(RoleModel value)
        {
            Role = value;
            SetChildData.InvokeAsync(Role);
        }
    }
}
