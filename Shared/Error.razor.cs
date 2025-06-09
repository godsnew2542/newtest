using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LoanApp.Shared
{
    public partial class Error
    {
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; } = null!;

        //public string SessionExpiredMsg { get; set; } = "Login session expired, Please login again.";

        //bool IsErrorActive = true;
        string Title = string.Empty;
        string Detail = string.Empty;

        public async Task ProcessError(Exception ex, string? mess = null)
        {
            LogError(ex);

            Title = "Something went wrong."; //ex.Message;
            Detail = $"{(ex.InnerException != null ? ex.InnerException.Message : ex.Message)} <br/> " +
                    $"{ex.StackTrace}" +
                    $"{(!string.IsNullOrEmpty(mess) ? "<br/> <div><b>Description:</b>" + mess + "</div>" : "")}";
            await JsRuntime.InvokeVoidAsync("toggleModal", "#ErrorMessageModal", "show");
            StateHasChanged();
        }

        public void LogError(Exception ex)
        {
            string Username = UserService.FindUserName(userStateProvider?.CurrentUser.UserName);
            if (string.IsNullOrEmpty(Username))
            {
                Username = "Annonymous";
            }

            Logger.LogError("Error:ProcessError - Type: {Type} Message: {Message} ,StackTrace: {StackTrace} ,User: {UserName}",
                ex.GetType(), ex.Message, ex.StackTrace, Username);
        }

        private async Task CloseModal()
        {
            await JsRuntime.InvokeVoidAsync("toggleModal", "#ErrorMessageModal", "hide");
        }
    }
}
