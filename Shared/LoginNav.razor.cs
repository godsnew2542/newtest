using LoanApp.IServices;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Shared
{
    public partial class LoginNav
    {
        [Parameter] public bool TestNotice { get; set; } = true;

        [Inject] protected IPsuoAuth2Services OAuth2Services { get; set; } = null!;

        private string imgThaID = "css/images/ThaID.png";

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
