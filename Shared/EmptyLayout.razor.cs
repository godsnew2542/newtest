using LoanApp.IServices;
using LoanApp.Services;
using LoanApp.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace LoanApp.Shared
{
    public partial class EmptyLayout
    {
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }
        [CascadingParameter] private Task<AuthenticationState>? _authStateTask { get; set; }

        [Inject] private IPsuoAuth2Services psuoAuth2Services { get; set; } = null!;

        public string StaffID { get; set; } = string.Empty;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                StaffID = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
                StateHasChanged();
            }
        }

        public static async Task SignOut(IUserService _userService, Task<AuthenticationState>? _authStateTask, IPsuoAuth2Services psuoAuth)
        {
            if (_authStateTask != null)
            {
                var authState = await _authStateTask;
                var authUser = authState.User;

                if (authUser.Identity!.IsAuthenticated)
                {
                    string? token = authUser.FindFirst(c => c.Type == "Token")!.Value;

                    if (!string.IsNullOrEmpty(token))
                    {
                        await psuoAuth.SignOut(token);
                    }
                }
            }

            await _userService.RemoveUserRoleAsync();
        }
    }
}
