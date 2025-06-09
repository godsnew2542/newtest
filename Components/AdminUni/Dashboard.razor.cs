using LoanApp.Model.Models;
using LoanApp.Pages.Admin;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Components.AdminUni
{
    public partial class Dashboard
    {
        [Parameter] public List<ReportAdminModel> ReportData { get; set; } = new();
        [Parameter] public string? Title { get; set; } = null;

        private AdminHome adminHome { get; set; } = new();
        private StaffTypeModel StaffType { get; set; } = new();
    }
}
