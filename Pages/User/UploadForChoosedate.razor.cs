
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LoanApp.Shared;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models.Data;
using LoanApp.Model.Settings;

namespace LoanApp.Pages.User
{
    public partial class UploadForChoosedate
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        #region Parameter
        [Parameter] public decimal RequestID { get; set; } = 0;
        [Parameter] public bool Edit { get; set; } = false;
        [Parameter] public string Role { get; set; } = string.Empty;
        [Parameter] public string StaffID { get; set; } = string.Empty;

        #endregion

        [Inject] IOptions<AppSettings> AppSettings { get; set; } = null!;
        [Inject] Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private UploadModel ModelUpload { get; set; } = new();
        private List<VAttachmentRequired> ItemUploadImg { get; set; } = new();
        private List<UploadModel> ResultInfoList { get; set; } = new();
        private List<UploadModel> ShowListResultImg { get; set; } = new();
        private StepUserChooseDateModel StepsChooseDate { get; set; } = new();

        private int ContractStepId { get; set; } = 2;
        private string ChooseDate { get; set; } = string.Empty;
        private List<string> ChangeDateValue { get; set; } = new();
        private string DataApply { get; set; } = string.Empty;
        private decimal ShowResultImgTitle { get; set; } = 0;
        private decimal? typeID { get; set; } = 0;

        protected override async Task OnInitializedAsync()
        {
            StepsChooseDate.Current = 1;
            try
            {
                if (RequestID != 0)
                {
                    LoanRequest? Req = await psuLoan.GetLoanRequestByLoanRequestId(RequestID);

                    typeID = (decimal?)Req?.LoanTypeId;

                    if (Req != null)
                    {

                        ItemUploadImg = await psuLoan.GetListVAttachmentRequired(Req.LoanTypeId, ContractStepId);
                        DataApply = await GetData(Req);
                    }
                }

                if (Edit)
                {
                    var Storage_2 = "FileChoosedate_2";
                    var checkFile = await sessionStorage.GetItemAsStringAsync(Storage_2);

                    if (checkFile != null)
                    {
                        ResultInfoList = await sessionStorage.GetItemAsync<List<UploadModel>>(Storage_2);
                    }
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private string GetTypeName(decimal AttTypeId)
        {
            var AttachmentNameThai = string.Empty;

            AttachmentType? AttType = _context.AttachmentTypes
            .Where(c => c.AttachmentTypeId == AttTypeId)
            .FirstOrDefault();

            if (AttType != null)
            {
                AttachmentNameThai = (!string.IsNullOrEmpty(AttType.AttachmentNameThai) ?
                    AttType.AttachmentNameThai : string.Empty);
            }

            return AttachmentNameThai;
        }

        private string GetUrl(UploadModel upload)
        {
            var path = string.Empty;
            if (upload.CollectByDB)
            {
                path = $"{AppSettings.Value.RequestFilePath}\\{upload.Url}";
            }
            else
            {
                path = $"{AppSettings.Value.RequestFilePath}\\Temp\\{upload.TempImgName}";
            }
            return path;
        }

        private async Task<string> GetData(LoanRequest? data)
        {
            LoanType? loanType = await psuLoan.GetLoanTypeAsync(data?.LoanTypeId);
            var LoanName = userService.GetLoanSubName(loanType);
            var Amount = (data?.LoanAmount != null ? data?.LoanAmount : 0);
            var Installment = (data?.LoanNumInstallments != null ? data?.LoanNumInstallments : 0);
            var message = $"{LoanName} ยอดเงิน {String.Format("{0:n2}", Amount)} บาท จำนวน {Installment} งวด";
            return message;
        }

        private async Task NextPageAsync()
        {
            await SaveDataToStorageAsync("FileChoosedate_2", ResultInfoList);
            if (Role == "Admin" || Role == "Manager")
            {
                navigationManager.NavigateTo($"/{Role}/AttachmentForChoosedate/{StaffID}/{RequestID}");
            }
            else
            {
                navigationManager.NavigateTo($"/User/AttachmentForChoosedate/{RequestID}");
            }
        }

        async Task SaveDataToStorageAsync(string key, List<UploadModel>? val = null)
        {
            if (val != null)
            {
                await sessionStorage.SetItemAsync(key, val);
            }
        }

        private void ShowImg(List<UploadModel> LResultImg, decimal AttTypeId)
        {
            ShowListResultImg = new();
            ShowResultImgTitle = AttTypeId;

            if (LResultImg.Count != 0)
            {
                for (int i = 0; i < LResultImg.Count; i++)
                {
                    var resultImg = LResultImg[i];

                    if (resultImg.AttachmentTypeId == AttTypeId)
                    {
                        ShowListResultImg.Add(resultImg);
                    }
                }
            }
        }

        public async Task CurrentRemoveListAsync(int value)
        {
            var StorageName = "SelectListUpload";
            var checkData = await sessionStorage.GetItemAsStringAsync(StorageName);
            if (checkData != null)
            {
                var SelectListUpload = await sessionStorage.GetItemAsync<UploadModel>(StorageName);
                UploadModel? myTodo = ResultInfoList.Find(x => x.Id == SelectListUpload.Id);
                if (myTodo != null)
                {
                    ResultInfoList.Remove(myTodo);
                    File.Delete(SelectListUpload.Url!);
                    await sessionStorage.RemoveItemAsync(StorageName);
                }
            }
        }

        public void SetCurrentData(DTEventArgs value)
        {
            ModelUpload = new UploadModel();
            ModelUpload.Id = ResultInfoList.Count() + 1;
            ModelUpload.Name = value.Params[0].ToString();
            ModelUpload.Url = value.Params[1].ToString();
            ModelUpload.TempImgName = value.Params[2].ToString();
            ModelUpload.AttachmentTypeId = (decimal)value.Params[3];
            ResultInfoList.Add(ModelUpload);
        }

        private async Task<string> GetChooseDateAsync()
        {
            var NameStorage = "ChooseDateTime_1";
            var mes = string.Empty;
            var checkData = await sessionStorage.GetItemAsStringAsync(NameStorage);
            if (checkData != null)
            {
                ChangeDateValue = await sessionStorage.GetItemAsync<List<string>>(NameStorage);
                mes = $"วันที่ {ChangeDateValue[0]} เวลา {ChangeDateValue[1]} น.";
            }
            return mes;
        }

        private void Back()
        {
            if (Role == "Admin" || Role == "Manager")
            {
                navigationManager.NavigateTo($"/{Role}/ChooseDate/{true}/{StaffID}/{RequestID}");
            }
            else
            {
                navigationManager.NavigateTo($"User/ChooseDate/{true}/{RequestID}");
            }
        }
    }
}
