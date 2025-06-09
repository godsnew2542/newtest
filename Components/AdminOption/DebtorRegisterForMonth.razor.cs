using AntDesign;
using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Components.AdminOption
{
    public partial class DebtorRegisterForMonth
    {
        [Parameter] public List<VReportTransaction> ReportTransactions { get; set; } = new();

        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private List<DebtorRegisterModel> TempDebtorRegisters { get; set; } = new();
        private List<DebtorRegisterModel> DebtorRegisters { get; set; } = new();

        private int _pageIndex { get; set; } = 1;
        private bool LoadingDataTable { get; set; } = true;
        private bool switchValue { get; set; } = true;

        private bool DefaultExpand { get; set; } = false;

        private Table<DebtorRegisterModel> TableRef { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                TempDebtorRegisters = new();
                DebtorRegisters = new();
                LoadingDataTable = true;

                try
                {
                    if (ReportTransactions.Any())
                    {
                        List<decimal> contractIdList = ReportTransactions
                            .Where(c => c.ContractId != null)
                            .Select(x => x.ContractId!.Value)
                            .ToList();

                        var result = await psuLoan.GetListVReportTransactionByContractIdList(contractIdList, "Y");

                        foreach (var item in ReportTransactions)
                        {
                            DebtorRegisterModel registerModel = new()
                            {
                                ReportTransaction = item,
                                TransactionList = result.Where(x => x.ContractId == item.ContractId).ToList(),
                            };

                            TempDebtorRegisters.Add(registerModel);
                        }
                    }
                    DebtorRegisters = TempDebtorRegisters;

                    LoadingDataTable = false;
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    LoadingDataTable = false;
                    StateHasChanged();
                    await notificationService.Error(notificationService.ExceptionLog(ex));
                }
            }
        }

        private async Task CheckboxSwitch()
        {
            LoadingDataTable = true;
            _pageIndex = 1;

            await Task.Delay(1);
            StateHasChanged();

            var interopResult = !switchValue;
            if (interopResult)
            {
                DebtorRegisters = TempDebtorRegisters;

            }
            else
            {
                DebtorRegisters = TempDebtorRegisters
                    .Where(x => !(new List<decimal>() { 99, 98 }).Contains(x.ReportTransaction.CurrentStatusId!.Value))
                    .ToList();
            }

            switchValue = interopResult;

            LoadingDataTable = false;
            StateHasChanged();
        }

        private async Task OnExpandTable()
        {
            LoadingDataTable = true;

            await Task.Delay(1);
            StateHasChanged();

            DefaultExpand = !DefaultExpand;

            LoadingDataTable = false;
            StateHasChanged();
        }

        public List<DebtorRegisterModel> ResultDataUI()
        {
            if (!LoadingDataTable)
            {
                return DebtorRegisters;
            }

            return new List<DebtorRegisterModel>();
        }

    }

    public class DebtorRegisterModel
    {
        public VReportTransaction ReportTransaction { get; set; } = new();
        public List<VReportTransaction> TransactionList { get; set; } = new();
    }
}
