using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Components.Message
{
    public partial class MessagePage
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        private MessageModel MessageData { get; set; } = new();

        private string StorageName { get; set; } = "Message";
        public object HtmlUtilities { get; private set; } = new();

        protected async override Task OnInitializedAsync()
        {
            List<object> setMessage = new List<object>();
            MessageData.Message = setMessage;

            try
            {
                var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
                if (checkData != null)
                {
                    MessageData = await sessionStorage.GetItemAsync<MessageModel>(StorageName);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        public async Task BackHomeAsync(string urlPage)
        {
            var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
            if (checkData != null)
            {
                await sessionStorage.RemoveItemAsync(StorageName);
            }

            navigationManager.NavigateTo(urlPage);
        }
    }
}
