using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Components.Upload
{
    public partial class ListUpload
    {
        [Parameter] public EventCallback<int> ListId { get; set; }
        [Parameter] public EventCallback<UploadModel> SelectFileCallback { get; set; }
        [Parameter] public UploadModel listModel { get; set; } = new();

        public UploadModel SelectListUpload { get; set; } = new();
        public string FileName { get; set; } = string.Empty;
        public string StorageName { get; set; } = "SelectListUpload";

        protected async override Task OnInitializedAsync()
        {
            SelectListUpload = new UploadModel();
            var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
            if (checkData != null) 
            {
                SelectListUpload = await sessionStorage.GetItemAsync<UploadModel>(StorageName);
            }
        }

        private async Task ConfirmFromAsync(UploadModel listUpload)
        {
            FileName = string.Empty;
            await sessionStorage.SetItemAsync(StorageName, listUpload);
            DeleteFile();
            await SelectFileCallback.InvokeAsync(listUpload);
        }

        //public async Task GetFileNameDataAsync()
        //{
        //    var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
        //    if (checkData != null)
        //    {
        //        UploadModel obj = await sessionStorage.GetItemAsync<UploadModel>(StorageName);
        //        FileName = obj.Name!;
        //    }

        //    StateHasChanged();
        //}


        private void DeleteFile()
        {
            ListId.InvokeAsync();
        }
    }
}
