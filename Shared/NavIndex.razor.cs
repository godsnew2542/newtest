using LoanApp.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LoanApp.Shared
{
    public partial class NavIndex
    {
        [Inject] protected IPsuoAuth2Services OAuth2Services { get; set; } = null!;
        [Inject] IConfiguration Config { get; set; } = null!;
        [Inject] private IServices.IUtilityServer utilityServer { get; set; } = null!;


        private bool testNotice { get; set; } = false;
        private bool IsMobile { get; set; } = false;

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
            }
        }

        private void SignIn()
        {
            string url = OAuth2Services.CallAuthorize();
            navigationManager.NavigateTo(url);
        }

        private void SignInThaId()
        {
            string url = OAuth2Services.CallAuthorizeThaiId();
            navigationManager.NavigateTo(url);
        }
    }
}
