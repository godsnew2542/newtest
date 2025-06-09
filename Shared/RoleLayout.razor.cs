using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;

using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Shared
{
    public partial class RoleLayout
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        [Parameter] public EventCallback<RoleModel> SetChildData { get; set; }

        private RoleModel Role { get; set; } = new();
        private List<LoanStaffPrivilege> RoleList { get; set; } = new();
        private Dictionary<string, CCampus> CampusDict { get; set; } = new();

        public string? StaffID { get; set; } = string.Empty;
        public string? UserName { get; set; } = string.Empty;

        protected async override Task OnInitializedAsync()
        {
            StaffID = StateProvider?.CurrentUser.StaffId;
            UserName = StateProvider?.CurrentUser.UserName;

            if (!string.IsNullOrEmpty(StaffID))
            {
                RoleList = userService.GetRole(StaffID);
                CampusDict = await EntitiesCentralService.GetCampusDict();
            }
        }

        private List<LoanGroup> GetLoanGroups()
        {
            List<LoanGroup> Lgroups = new();

            if (!string.IsNullOrEmpty(UserName))
            {
                //string[] ListUserDev = Utility.UserDev;
                //if (ListUserDev.Contains(UserName))
                //{
                //    Lgroups = _context.LoanGroups.ToList();
                //}

                LoanGroup group = new()
                {
                    GroupId = 0,
                    GroupName = "ผู้ใช้งาน",
                };

                Lgroups.Add(group);
            }
            return Lgroups;
        }

        private async Task ChangeRoleAsync(decimal? groupId = 0, string? camp = null)
        {
            Role = new RoleModel();
            try
            {
                if (groupId == 0)
                {
                    Role = await userService.SetSelectedUserRoleAsync(StateProvider?.CurrentUser.StaffId);
                    if (StateProvider != null)
                    {
                        StateProvider.CurrentUser.CapmSelectNow = null;
                    }
                }
                else
                {
                    LoanGroup? LoanGroups = _context.LoanGroups
                        .Where(c => c.GroupId == groupId)
                        .FirstOrDefault();
                    if (LoanGroups != null)
                    {
                        Role = await userService.SetSelectedUserRoleAsync(StateProvider?.CurrentUser.StaffId, LoanGroups.GroupId, camp);
                        if (StateProvider != null)
                        {
                            if (groupId == 1)
                            {
                                StateProvider.CurrentUser.CapmSelectNow = "00";
                            }
                            else
                            {
                                StateProvider.CurrentUser.CapmSelectNow = camp;
                            }
                        }
                    }
                }
                await SetChildData.InvokeAsync(Role);
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
                throw;
            }
        }
    }
}
