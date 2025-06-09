using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Pages.User
{
    public partial class UploadDoc
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        #region Parameter
        [Parameter] public decimal LoadID { get; set; } = 0;
        [Parameter] public decimal StapID { get; set; } = 0;

        #endregion

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;

        #endregion

        List<UploadModel> resultInfoList { get; set; } = new();
        List<VAttachmentRequired> ItemUploadImg { get; set; } = new();
        private StepsUserApplyLoanModel StepsUser { get; set; } = new() { Current = 1 };
        private int LoanRequestId { get; set; } = 0;
        private decimal LoanRequestAmount { get; set; } = 0;
        private int LoanRequestNumInstall { get; set; } = 0;
        private byte LoanTypeID { get; set; } = 0;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    if (LoadID != 0)
                    {
                        ItemUploadImg = await psuLoan.GetListVAttachmentRequired((byte)LoadID, 1);

                        #region กรณีไม่มีไฟล์ให้อัปโหลด
                        if (!ItemUploadImg.Any())
                        {
                            await NextPageAsync();
                        }
                        #endregion
                    }

                    var FromLoan1 = await sessionStorage.GetItemAsStringAsync("FromLoan_1");

                    if (!string.IsNullOrEmpty(FromLoan1))
                    {
                        ApplyLoanModel FromLoan = await sessionStorage.GetItemAsync<ApplyLoanModel>("FromLoan_1");
                        LoanRequestId = FromLoan.LoanRequestId;
                        LoanTypeID = (byte)FromLoan.LoanTypeID!;
                        LoanRequestAmount = FromLoan.LoanAmount;
                        LoanRequestNumInstall = FromLoan.LoanNumInstallments;
                    }

                    if (StapID != 0)
                    {
                        ItemUploadImg = await _context.VAttachmentRequireds
                            .Where(c => c.LoanTypeId == LoanTypeID)
                            .Where(c => c.ContractStepId == StapID)
                            .ToListAsync();

                        var FromLoan2 = await sessionStorage.GetItemAsStringAsync("FromLoan_2");

                        if (!string.IsNullOrEmpty(FromLoan2))
                        {
                            resultInfoList = await sessionStorage.GetItemAsync<List<UploadModel>>("FromLoan_2");
                        }
                    }

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }
            }
        }

        private async Task<string> GetLoanType(byte TypeID)
        {
            string TypeName = string.Empty;
            if (TypeID != 0)
            {
                LoanType? Loan = await psuLoan.GetLoanTypeAsync(TypeID);
                TypeName = userService.GetLoanSubName(Loan);
            }
            return TypeName;
        }

        //private void SetCurrentData(DTEventArgs value)
        //{
        //    UploadModel ModelUpload = new();
        //    ModelUpload.Id = resultInfoList.Count() + 1;
        //    ModelUpload.Name = value.Params[0].ToString();
        //    ModelUpload.Url = value.Params[1].ToString();
        //    ModelUpload.TempImgName = value.Params[2].ToString();
        //    ModelUpload.AttachmentTypeId = (decimal)value.Params[3];
        //    resultInfoList.Add(ModelUpload);
        //}

        private void SetCurrentData(UploadModel value)
        {
            UploadModel ModelUpload = new()
            {
                Id = resultInfoList.Count + 1,
                Name = value.Name,
                Url = value.Url,
                TempImgName = value.TempImgName,
                AttachmentTypeId = value.AttachmentTypeId
            };

            //ModelUpload.Id = resultInfoList.Count() + 1
            //ModelUpload.Name = value.Params[0].ToString()
            //ModelUpload.Url = value.Params[1].ToString()
            //ModelUpload.TempImgName = value.Params[2].ToString()
            //ModelUpload.AttachmentTypeId = (decimal)value.Params[3]
            resultInfoList.Add(ModelUpload);
        }

        public async Task CurrentRemoveListAsync(int value)
        {
            var SelectListUpload = await SaveFileAndImgService.ReadStorageSelectUploadAsync();
            if (SelectListUpload != null)
            {
                UploadModel? myTodo = resultInfoList.Find(x => x.Id == SelectListUpload.Id);
                if (myTodo != null)
                {
                    resultInfoList.Remove(myTodo);
                    File.Delete(SelectListUpload.Url!);
                    await SaveFileAndImgService.RemoveStorageAsync();
                }
            }
        }

        protected bool CheckFileExist(string URL)
        {
            bool RetFlag = false;
            RetFlag = File.Exists(URL);
            return RetFlag;
        }

        private async Task NextPageAsync()
        {
            //var CheckStepData = await userService.CheckStepRequireAsync(resultInfoList, LoadID, 1)

            if (!CheckStepRequireFile(resultInfoList, ItemUploadImg))
            {
                var alert = $"กรุณาอัปโหลดเอกสารประกอบการกู้";
                await notificationService.WarningDefult(alert);
                return;
            }

            await SaveDataToStorageAsync("FromLoan_2", resultInfoList);
            navigationManager.NavigateTo("/CheckDataByApplyLoan");
        }

        private static bool CheckStepRequireFile(List<UploadModel> resultFile, List<VAttachmentRequired> rootFile)
        {
            bool result = true;

            if (!resultFile.Any())
            {
                return false;
            }

            foreach (var item in rootFile)
            {
                var t = resultFile.Find(x => x.AttachmentTypeId == item.AttachmentTypeId);

                if (result && t == null)
                {
                    result = false;
                }
            }

            return result;
        }

        private void BackPage()
        {
            if (resultInfoList.Count != 0)
            {
                for (var i = 0; i < resultInfoList.Count; i++)
                {
                    var element = resultInfoList[i];
                    if (CheckFileExist(element.Url!))
                    {
                        File.Delete(element.Url!);
                    }
                }
            }
            navigationManager.NavigateTo($"/Applyloan/Edit/{LoanRequestId}");
        }

        private async Task SaveDataToStorageAsync(string key, List<UploadModel> val)
        {
            await sessionStorage.SetItemAsync(key, val);
        }
    }
}
