using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace LoanApp.Components.AdminOption
{
    public partial class ReportLoanForMonth
    {
        [Parameter] public List<VReportTransaction> ReportTransactions { get; set; } = new();

        [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private List<VReportTransactionEditModel> DataSet { get; set; } = new();
        private int _pageIndex { get; set; } = 1;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    if (ReportTransactions.Any())
                    {
                        List<VReportTransactionEditModel> test = new();

                        #region Get เงินงวดล่าสุดมา
                        List<PaymentTransaction> transactions = ReportTransactions
                             .Where(c => !(new List<decimal?>() { 98, 99 }).Contains(c.CurrentStatusId))
                             .Select(x => new PaymentTransaction()
                             {
                                 InstallmentNo = x.InstallmentNo,
                                 ContractId = x.ContractId,
                                 ContractNo = x.ContractNo,
                             })
                             .ToList();

                        List<VReportTransaction> temp = await psuLoan.CheckDataFormImportPayment(transactions);

                        #endregion

                        #region Set new Data เงินงวดล่าสุดมา
                        if (temp.Any())
                        {
                            foreach (var item in temp)
                            {
                                VReportTransaction? vReport = ReportTransactions
                                    .FirstOrDefault(x => x.ContractId == item.ContractId);

                                if (vReport != null)
                                {
                                    VReportTransactionEditModel transactionEdit = new()
                                    {
                                        Transaction = vReport,
                                        IsEditBalanceAmount = true
                                    };

                                    transactionEdit.Transaction.BalanceAmount = item.BalanceAmount - vReport.PrincipleAmount;

                                    test.Add(transactionEdit);
                                }
                            }
                        }
                        #endregion

                        #region check ข้อมูลที่เหลือ และนำมาแสดง
                        var contractIds = test.Select(x => x.Transaction.ContractId).ToList();

                        if (contractIds.Any())
                        {
                            var pp = ReportTransactions
                                .Where(c => !contractIds.Contains(c.ContractId))
                                .ToList();

                            if (pp.Any())
                            {
                                foreach (var item in pp)
                                {
                                    VReportTransactionEditModel transactionEdit = new()
                                    {
                                        Transaction = item,
                                        IsEditBalanceAmount = false
                                    };
                                    test.Add(transactionEdit);
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in ReportTransactions)
                            {
                                VReportTransactionEditModel transactionEdit = new()
                                {
                                    Transaction = item,
                                    IsEditBalanceAmount = false
                                };
                                test.Add(transactionEdit);
                            }
                        }
                        #endregion

                        DataSet = test;
                    }

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await notificationService.Error(notificationService.ExceptionLog(ex));
                }
            }
        }

        public List<VReportTransactionEditModel> ResultDataUI()
        {
            return DataSet;
        }

    }

    public class VReportTransactionEditModel
    {
        public VReportTransaction Transaction { get; set; } = new();
        public bool IsEditBalanceAmount { get; set; } = false;
    }
}
