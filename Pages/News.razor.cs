using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using NPOI.SS.Formula.Functions;

namespace LoanApp.Pages;

public partial class News
{
    [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

    [Inject] private INotificationService notificationService { get; set; } = null!;

    public RoleModel Role { get; set; } = new();
    public decimal[] Admin { get; } = new[] { 1m, 2m };
    public decimal[] Manager { get; } = new[] { 3m };
    public decimal[] AdminUni { get; } = new[] { 1m };
    public decimal[] AdminCampus { get; } = new[] { 2m };
    public decimal[] Treasury { get; } = new[] { 4m };
    public string StaffID { get; set; } = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                //var builder1 = new ConfigurationBuilder().AddUserSecrets<Program>()
                //var Configuration = builder1.Build()
                //test = Configuration["password"]

                StaffID = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);
                if (!string.IsNullOrEmpty(StaffID))
                {
                    var RoleData = userService.GetRole(StaffID);
                    // เช็คการเข้าครั้งแรกว่า ยังค้างอยู่ role ??
                    var roleIsNull = await CheckloaclRoleAsync(RoleData);

                    // ต้องยังไม่มีถึงจะเข้า
                    if (string.IsNullOrEmpty(roleIsNull))
                    {
                        if (RoleData.Count != 0)
                        {
                            var Index = 0;
                            for (int i = 0; i < RoleData.Count; i++)
                            {
                                var item = RoleData[i];
                                if (Index == i)
                                {
                                    Role = await userService.SetSelectedUserRoleAsync(StateProvider?.CurrentUser.StaffId, item.GroupId!.Value);
                                }
                            }
                        }
                        else
                        {
                            Role = await userService.SetSelectedUserRoleAsync(StateProvider?.CurrentUser.StaffId);

                        }
                    }

                    if (StateProvider != null)
                    {
                        StateProvider.CurrentUser.CapmSelectNow = Role.CampId;
                    }

                    navigationManager.NavigateTo(Role.Topage!);
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }
        }
    }

    private async Task<string> CheckloaclRoleAsync(List<LoanStaffPrivilege> listPrivilege)
    {
        var role = await localStorage.GetItemAsync<string>("role");
        string roleIsNull = string.Empty;
        LoanStaffPrivilege? staffPrivilege = null;

        if (!string.IsNullOrEmpty(role))
        {
            roleIsNull = role;
            if (role == "adminUni")
            {
                staffPrivilege = listPrivilege.Find(x => AdminUni.Contains(x.GroupId!.Value));
                if (staffPrivilege != null)
                {
                    await SetGroupIdAsync(staffPrivilege.GroupId!.Value);
                }
                else
                {
                    await SetGroupIdAsync();
                }
            }
            else if (role == "adminCampus")
            {
                staffPrivilege = listPrivilege.Find(x => AdminCampus.Contains(x.GroupId!.Value));
                if (staffPrivilege != null)
                {
                    await SetGroupIdAsync(staffPrivilege.GroupId!.Value);
                }
                else
                {
                    await SetGroupIdAsync();
                }
            }
            else if (role == "manager")
            {
                staffPrivilege = listPrivilege.Find(x => Manager.Contains(x.GroupId!.Value));
                if (staffPrivilege != null)
                {
                    await SetGroupIdAsync(staffPrivilege.GroupId!.Value);
                }
                else
                {
                    await SetGroupIdAsync();
                }
            }
            else if (role == "treasury")
            {
                staffPrivilege = listPrivilege.Find(x => Treasury.Contains(x.GroupId!.Value));
                if (staffPrivilege != null)
                {
                    await SetGroupIdAsync(staffPrivilege.GroupId!.Value);
                }
                else
                {
                    await SetGroupIdAsync();
                }
            }
            else
            {
                await SetGroupIdAsync();
            }
        }

        return roleIsNull;
    }

    private async Task SetGroupIdAsync(decimal Id = 0)
    {
        Role = await userService.SetSelectedUserRoleAsync(StateProvider?.CurrentUser.StaffId, Id);
        if (StateProvider != null)
        {
            StateProvider.CurrentUser.CapmSelectNow = Role.CampId;
        }
    }

    public void SetCurrentData(RoleModel value)
    {
        Role = new RoleModel();
        Role = value;
        navigationManager.NavigateTo(Role.Topage!);
    }

    private bool getNewsData(DateTime endDate)
    {
        DateTime dateTime = DateTime.Now;
        if (dateTime <= endDate)
        {
            return true;
        }
        return false;
    }

    private string? getFileUrl(string? fileName)
    {
        string rootUrl = SaveFileAndImgService.GetFullPhysicalFilePathDir();
        string fileTemplate = $"{rootUrl}\\{Utility.TEMPLATE_DIR}\\{fileName}";

        bool isnoFile = File.Exists(fileTemplate);

        if (isnoFile)
        {
            var rootFile = $"{navigationManager.BaseUri}{Utility.Files_DIR}\\{Utility.TEMPLATE_DIR}\\{fileName}";

            return rootFile;
        }
        else
        {
            return null;
        }
    }
}
