using DocumentFormat.OpenXml.InkML;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace LoanApp.Components.Admin
{
    public partial class FirstPaymentTable
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #region Parameter
        [Parameter] public EventCallback<List<SetNewRecordFirstPaymentModel>> SetChildData { get; set; }

        [Parameter] public decimal[] StatusId { get; set; } = null!;

        [Parameter] public decimal LoanTypeID { get; set; } = 0m;

        [Parameter] public SearchModel Search { get; set; } = new();

        /// <summary>
        /// LoanApp.Models.User.CapmSelectNow
        /// </summary>
        [Parameter] public string? AdminCampId { get; set; } = null;

        #endregion

        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;

        private List<VLoanRequestContract> LoanRequestContracts { get; set; } = new();
        private IOrderedQueryable<VLoanRequestContract> Query { get; set; } = null!;
        public List<SetNewRecordFirstPaymentModel> ListRecordFirstPayment { get; set; } = new();

        private int _pageIndex { get; set; } = 1;
        private bool loading { get; set; } = false;

        public async Task DataTableV2(string? searchName, decimal typeId, int? pageIndex = null)
        {
            LoanRequestContracts = new();

            await Task.Delay(1);
            StateHasChanged();
            try
            {
                if (pageIndex != null)
                {
                    _pageIndex = pageIndex.Value;
                }

                LoanRequestContracts = await Query
                     .Where(c => string.IsNullOrEmpty(searchName) ||
                     (c.DebtorNameTh!.Contains(searchName) ||
                     c.DebtorSnameTh!.Contains(searchName) ||
                     (c.DebtorNameEng!).ToLower().Contains(searchName.ToLower()) ||
                     (c.DebtorSnameEng!).ToLower().Contains(searchName.ToLower()) ||
                     (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                     (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower())))
                     .Where(c => typeId == 0 || c.LoanTypeId == typeId)
                     .ToListAsync();
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        public void RefreshContractNo(decimal? contractId, string? contractNo)
        {
            var data = LoanRequestContracts.Find(c => c.ContractId == contractId);

            if (data != null)
            {
                data.ContractNo = !string.IsNullOrEmpty(contractNo) ? contractNo : null;
            }
        }

        public async Task RefreshAllContractNo(List<SetNewRecordFirstPaymentModel> result)
        {
            loading = true;
            await Task.Delay(1);
            StateHasChanged();

            var pvp = result.Select(c => c.ContractId).ToList();
            var opp = LoanRequestContracts.Where(c => !pvp.Contains(c.ContractId)).ToList();

            foreach (var model in result)
            {
                RefreshContractNo(model.ContractId, model.ContractNo);
            }

            if (opp.Any())
            {
                opp.ForEach(x => x.ContractNo = null);
            }

            loading = false;
            StateHasChanged();
        }

        protected override void OnInitialized()
        {
            ListRecordFirstPayment = new();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    Query = await psuLoan.GetListIQueryableVLoanRequestContractFormPageFirstPaymentTable(AdminCampId, StatusId.ToList());

                    await DataTableV2(Search.Title, LoanTypeID);
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }

                StateHasChanged();
            }
        }

        private async Task AddContractNoAsync(string? contractNo, decimal? contractId)
        {
            SetNewRecordFirstPaymentModel newRecordFirstPayment = new()
            {
                ContractId = contractId,
                ContractNo = contractNo
            };

            if (ListRecordFirstPayment.Any())
            {
                SetNewRecordFirstPaymentModel? myTodo = ListRecordFirstPayment.Find(x => x.ContractId == newRecordFirstPayment.ContractId);

                if (myTodo != null)
                {
                    RemoveListRecordFirstPayment(myTodo);
                }
            }

            if (!string.IsNullOrEmpty(newRecordFirstPayment.ContractNo))
            {
                ListRecordFirstPayment.Add(newRecordFirstPayment);
            }

            await SetChildData.InvokeAsync(ListRecordFirstPayment);
        }

        private void RemoveListRecordFirstPayment(SetNewRecordFirstPaymentModel myTodo)
        {
            ListRecordFirstPayment.Remove(myTodo);
        }

        private async Task AddContractNoAuto(VLoanRequestContract context)
        {
            try
            {
                decimal? fiscalYear = userService.GetFiscalYear(DateTime.Now);

                var key = "auto";
                if (fiscalYear != null)
                {
                    var y= fiscalYear.ToString();
                    key = y.Substring(y.Length - 2);
                }

                if (!string.IsNullOrEmpty(StateProvider?.CurrentUser.CapmSelectNow))
                {
                    loading = true;
                    StateHasChanged();
                    await Task.Delay(1);

                    string? contractNo = null;

                    switch (StateProvider?.CurrentUser.CapmSelectNow)
                    {
                        case "01": // วิทยาเขตหาดใหญ่
                            //contractNo = $"{key}-{context.ContractId}";
                            break;

                        //case "02": // วิทยาเขตปัตตานี
                        //    break;

                        case "03": // วิทยาเขตภูเก็ต
                            contractNo = $"{key}-{context.ContractId}";
                            break;

                            //case "04": // วิทยาเขตสุราษฎร์ธานี
                            //    break;

                            //case "05": // วิทยาเขตตรัง
                            //    break;
                    }

                    if (contractNo == null)
                    {
                        _ = Task.Run(() => { notificationService.WarningDefult("ยังไม่เปิดใชช้งาน"); });
                        loading = false;
                        StateHasChanged();
                        return;
                    }

                    await AddContractNoAsync(contractNo, context.ContractId);
                    context.ContractNo = contractNo;
                }
            }
            catch (Exception ex)
            {
                _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
            }

            loading = false;
            StateHasChanged();
        }
    }
}
