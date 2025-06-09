using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;
using LoanApp.IServices;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace LoanApp.Shared
{
    public partial class MainLayout
    {
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }
        [CascadingParameter] private Task<AuthenticationState>? _authStateTask { get; set; }


        #region Inject
        [Inject] private IPsuoAuth2Services psuoAuth2Services { get; set; } = null!;
        [Inject] IWebHostEnvironment _env { get; set; } = null!;
        [Inject] IConfiguration Config { get; set; } = null!;
        [Inject] private IServices.IUtilityServer utilityServer { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;

        #endregion

        //public EventCallback<RoleModel> SetChildData { get; set; }

        private RoleModel Role { get; set; } = new();
        private Dictionary<string, CCampus> cCampusDict { get; set; } = new();

        private decimal[] Manager { get; } = new[] { 3m };
        private decimal[] AdminUni { get; } = new[] { 1m };
        private decimal[] AdminCampus { get; } = new[] { 2m };
        private decimal[] Treasury { get; } = new[] { 4m };
        private string? StaffID { get; set; } = string.Empty;
        private string GetImage { get; set; } = string.Empty;
        private bool IsMobile { get; set; } = false;

        private string UserImgBaseUrl = "https://dss.psu.ac.th/dss_person/images/staff/";
        private bool testNotice { get; set; } = false;

        public bool collapseNavMenu { get; set; } = false;
        //private string NavMenuCssClass => collapseNavMenu ? "sb-nav-fixed sb-sidenav-toggled" : "sb-nav-fixed";

        protected override void OnInitialized()
        {
            testNotice = utilityServer.CheckDBtest();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                IsMobile = await JS.InvokeAsync<bool>("isDevice");
                StateHasChanged();

                cCampusDict = await EntitiesCentralService.GetCampusDict();
                StaffID = StateProvider?.CurrentUser.StaffId;

                if (!string.IsNullOrEmpty(StaffID))
                {
                    GetImage = GetUserImgURL(StaffID);
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

                        if (StateProvider != null)
                        {
                            StateProvider.CurrentUser.CapmSelectNow = Role.CampId;
                        }
                    }
                }
                else
                {
                    navigationManager.NavigateTo("/");
                }
                StateHasChanged();
            }
        }

        public void ToggleNavMenu(bool? val = null)
        {
            if (val == null)
            {
                collapseNavMenu = !collapseNavMenu;
            }
            else
            {
                collapseNavMenu = val.Value;
                StateHasChanged();
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

        public async Task SetCurrentDataAsync(RoleModel value)
        {
            Role = new();
            Role = value;
            // navigationManager.NavigateTo(Role.Topage)
            await sessionStorage.ClearAsync();

            navigationManager.NavigateTo("/");
        }

        public string GetFullName()
        {
            var fullName = userService.GetFullName(StaffID);
            return fullName;
        }

        public string? GetCampusByStaffId()
        {
            var StaffDetail = userService.GetUserDetail(StaffID);
            var CampusName = "-";

            if (StaffDetail != null && !string.IsNullOrEmpty(StaffDetail.StaffId))
            {
                CampusName = StaffDetail.CampNameThai;
            }
            return CampusName;
        }

        public string? GetPositionByStaffId()
        {
            var StaffDetail = userService.GetUserDetail(StaffID);
            var PosName = "-";

            if (StaffDetail != null)
            {
                PosName = StaffDetail.PosNameThai;

            }
            return PosName;
        }

        public string GetSubName()
        {
            var StaffDetail = userService.GetUserDetail(StaffID);
            var Name = string.Empty;

            if (StaffDetail != null)
            {
                Name = $"{StaffDetail.StaffNameThai}";
            }
            return Name;
        }

        public string GetRole(RoleModel dataRole, bool IsShowCamp = false)
        {
            var roleName = "ผู้ใช้งาน";

            if (dataRole.Id != 0)
            {
                roleName = userService.GetGroupName(dataRole.Id);

                if (IsShowCamp && dataRole.Id != (int)RoleType.AdminUni && StateProvider?.CurrentUser.CapmSelectNow != null)
                {
                    roleName += $" {cCampusDict[StateProvider.CurrentUser.CapmSelectNow].CampNameThai}";
                }
            }

            return roleName;
        }

        private string GetUserImgURL(string? staffID)
        {
            string imgUrl = string.Empty;
            try
            {
                imgUrl = string.Format("{0}{1}.jpg", UserImgBaseUrl, staffID);

                if (!Utility.IsRemoteFileExist(imgUrl))
                {
                    var SeparatorChar = (Utility.CheckOSisWindows() ? Path.DirectorySeparatorChar : Path.AltDirectorySeparatorChar);

                    //imgUrl = "css//images//Avatar.jpeg"
                    //string path = Path.Combine(_env.WebRootPath, imgUrl).Replace("\\", "/")

                    imgUrl = $"css{SeparatorChar}images{SeparatorChar}Avatar.jpeg";
                    string path = Path.Combine(_env.WebRootPath, imgUrl);
                    imgUrl = Utility.ImageToBase64String(path);
                }
                else
                {
                    Byte[] _byte = Utility.GetImage(imgUrl);
                    imgUrl = Convert.ToBase64String(_byte);
                }
            }
            catch (Exception)
            {
                throw;
            }
            return imgUrl;
        }
    }
}
