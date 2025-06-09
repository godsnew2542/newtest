using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Shared
{
    public partial class NavAdminCampus
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
