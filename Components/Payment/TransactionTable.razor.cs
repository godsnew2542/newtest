using Microsoft.AspNetCore.Components;
using LoanApp.Model.Models;
using LoanApp.Shared;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Components.Test;
using DocumentFormat.OpenXml.Spreadsheet;

namespace LoanApp.Components.Payment
{
    public partial class TransactionTable
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        #region Parameter
        [Parameter] public string[] DayInstallments { get; set; } = Array.Empty<string>();
        [Parameter] public decimal? ContractId { get; set; } = null;

        #endregion

        #region Inject
        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        #endregion

        private List<InstallmentDetail> InstallmentDetailList { get; set; } = new();
        private List<InstallmentDetail> LInstallmentDetail { get; set; } = new();
        private List<PaymentTransaction> PTransaction { get; set; } = new();

        private List<PaymentTransaction> TestResult { get; set; } = new();
        private bool LoadingTransaction { get; set; } = true;

        //protected override async Task OnInitializedAsync()
        //{
        //    try
        //    {
        //        LInstallmentDetail = new();
        //        PTransaction = new();

        //        if (DayInstallments.Length != 0)
        //        {
        //            ContractMain? main = await psuLoan.GeContractMainByContractIdAsync(ContractId);

        //            if (main != null && main.ContractStatusId == 99)
        //            {
        //                List<PaymentTransaction> tempResult = await psuLoan.GetAllPaymentTransactionByContractNo(main.ContractNo);
        //                PTransaction = tempResult.DistinctBy(x => x.InstallmentNo).ToList();
        //            }
        //            else
        //            {
        //                LInstallmentDetail = await GetDataAsync(main);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await notificationService.Error(notificationService.ExceptionLog(ex));
        //    }
        //}

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                TestResult = new();

                if (ContractId == null)
                {
                    StateHasChanged();
                    return;
                }

                try
                {
                    DateTime? paidDate = null;
                    List<PaymentTransaction> result = new();
                    List<InstallmentDetail> detailList = new();
                    List<PaymentTransaction> tempResult = new();

                    ContractMain? main = await psuLoan.GeContractMainByContractIdAsync(ContractId);

                    if (main != null && main.LoanNumInstallments != null)
                    {
                        tempResult = await psuLoan.GetAllPaymentTransactionByContractNo(main.ContractNo);
                        paidDate = main.PaidDate != null ? main.PaidDate : main.ContractDate;

                        detailList = await psuLoan.GetAllInstallmentDetailByContractId(ContractId);

                        if (!detailList.Any())
                        {
                            if (paidDate != null)
                            {
                                int? loanNumInstallments = (int?)main.LoanNumInstallments;

                                PaymentListComponent paymentListComponentPage = new();

                                detailList = paymentListComponentPage.SetInstallmentDetail(
                                    date: paidDate.Value,
                                    LoanNumInstallments: loanNumInstallments!.Value,
                                    LoanAmount: main.LoanAmount!.Value,
                                    LoanInterest: main.LoanInterest,
                                    _transactionService: TransactionService,
                                    contractId: main.ContractId);
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        return;
                    }


                    if (tempResult.Any())
                    {
                        decimal? maxInstallmentNo = tempResult.Max(x => x.InstallmentNo);

                        if (maxInstallmentNo != null)
                        {
                            for (int i = 0; i < maxInstallmentNo; i++)
                            {
                                decimal installmentNo = i + 1;
                                PaymentTransaction? pt = tempResult
                                .OrderByDescending(x => x.CreatedDate)
                                .FirstOrDefault(x => x.InstallmentNo == installmentNo);

                                PaymentTransaction temp = new()
                                {
                                    PaymentTransId = pt == null ? 0 : pt.PaymentTransId,
                                    InstallmentNo = installmentNo,
                                    ContractId = pt?.ContractId,
                                    PayDate = pt?.PayDate != null ? pt?.PayDate : detailList.Find(x => x.InstallmentNo == installmentNo)?.DueDate,
                                    PrincipleAmount = pt?.PrincipleAmount,
                                    InterestAmont = pt?.InterestAmont,
                                    TotalAmount = pt?.TotalAmount,
                                    BalanceAmount = pt?.BalanceAmount,
                                    ReferenceId1 = pt?.ReferenceId1,
                                    ReferenceId2 = pt?.ReferenceId2,
                                    ContractNo = pt?.ContractNo,
                                    CreatedBy = pt?.CreatedBy,
                                    CreatedDate = pt?.CreatedDate,
                                    Remark = pt?.Remark,
                                };

                                result.Add(temp);
                            }

                            decimal installmentNoInfo = main.LoanNumInstallments!.Value - maxInstallmentNo!.Value;

                            if (installmentNoInfo > 0)
                            {
                                for (int i = (int)maxInstallmentNo!.Value; i < main.LoanNumInstallments; i++)
                                {
                                    decimal installmentNo = i + 1;
                                    InstallmentDetail? detail = detailList.Find(x => x.InstallmentNo == installmentNo);

                                    result.Add(SetDataTransaction(detail, installmentNo));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (detailList.Any())
                        {
                            for (int i = 0; i < main!.LoanNumInstallments; i++)
                            {
                                decimal installmentNo = i + 1;
                                InstallmentDetail? detail = detailList.Find(x => x.InstallmentNo == installmentNo);

                                result.Add(SetDataTransaction(detail, installmentNo));
                            }
                        }
                    }

                    TestResult = result;

                    var tt = tempResult.Find(x => x.InstallmentNo == 0);

                    if (tt != null)
                    {
                        TestResult.Insert(0, tt);
                    }

                    LoadingTransaction = false;
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    LoadingTransaction = false;
                    _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
                    StateHasChanged();
                }
            }
        }

        private PaymentTransaction SetDataTransaction(InstallmentDetail? detail, decimal installmentNo)
        {
            PaymentTransaction temp = new()
            {
                InstallmentNo = installmentNo,
                ContractId = detail?.ContractId,
                PayDate = detail?.DueDate,
                PrincipleAmount = detail?.PrincipleAmount,
                InterestAmont = detail?.InterestAmont,
                TotalAmount = detail?.TotalAmount,
                BalanceAmount = null,
                ReferenceId1 = null,
                ReferenceId2 = null,
                ContractNo = null,
                CreatedBy = null,
                CreatedDate = null,
                Remark = null,
            };

            return temp;
        }

        private async Task<List<InstallmentDetail>> GetDataAsync(ContractMain? main)
        {
            InstallmentDetailList = new();

            List<InstallmentDetail> detailList = await psuLoan.GetAllInstallmentDetailByContractId(ContractId);

            if (main != null)
            {
                DateTime? paidDate = main.PaidDate != null ? main.PaidDate : main.ContractDate;

                if (detailList.Any())
                {
                    InstallmentDetailList = detailList;
                }
                else
                {
                    if (paidDate != null)
                    {
                        InstallmentDetailList = await SetInstallmentDetailAsync(main, paidDate.Value);
                    }
                }
            }
            return InstallmentDetailList;
        }

        private async Task<List<InstallmentDetail>> SetInstallmentDetailAsync(ContractMain contract, DateTime date)
        {
            List<InstallmentDetail> result = new();
            try
            {
                List<string> ListDate = new(DayInstallments);
                LoanType? loan = await psuLoan.GetLoanTypeAsync((byte?)contract.LoanTypeId);
                decimal BalanceAmount = contract.LoanAmount != null ? contract.LoanAmount.Value : 0;

                if (loan == null)
                {
                    return result;
                }

                for (int i = 0; i < contract.LoanNumInstallments; i++)
                {
                    MonthModel _month = new();
                    var index = i + 1;

                    var SplitDueDate = ListDate[i].Split(" ");
                    int IntMonth = Array.IndexOf(_month.Th, SplitDueDate[1]) + 1;
                    int IntYear = Convert.ToInt32(SplitDueDate[2]) - 543;
                    DateTime DueDate = new DateTime(IntYear, IntMonth, Convert.ToInt32(SplitDueDate[0]));

                    var PaidInstallment = SetLoanInstallment(contract.LoanAmount!.Value, (int)contract.LoanNumInstallments, contract.LoanTypeId);

                    var Interest = GetInterest(ListDate, index, BalanceAmount, loan.LoanInterest, contract, date);
                    var Balance = PaidInstallment - Interest;

                    // งวดสุดท้าย
                    if (index == contract.LoanNumInstallments)
                    {
                        Balance = BalanceAmount;
                        PaidInstallment = BalanceAmount + Interest;
                    }
                    BalanceAmount = BalanceAmount - Balance;

                    InstallmentDetail Installment = new()
                    {
                        ContractId = contract.ContractId,
                        InstallmentNo = index,
                        DueDate = DueDate,
                        PrincipleAmount = Balance,
                        InterestAmont = Interest,
                        TotalAmount = PaidInstallment
                    };

                    result.Add(Installment);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }

            return result;
        }

        private decimal GetInterest(List<string> ListPayDate, int NumInstallments, decimal BalanceAmount, decimal? LoanInterest, ContractMain Contract, DateTime PaidDate)
        {
            MonthModel _month = new MonthModel();

            var TotalInstallments = Convert.ToDecimal(Contract.LoanNumInstallments);
            var _BalanceAmount = BalanceAmount;
            var payDateString = ListPayDate[NumInstallments - 1];
            DateTime payDate = TransactionService.ChangeFormatPayDate(payDateString, _month.Number);

            decimal Interest = TransactionService.GetTransactionByInterest(TotalInstallments, NumInstallments, PaidDate, payDate, _BalanceAmount, LoanInterest!.Value);
            return Interest;
        }

        private decimal SetLoanInstallment(decimal LoanAmount, int LoanNumInstallments, decimal? loanTypeId)
        {
            var loan = userService.GetLoanType((byte?)loanTypeId);
            var Installment = 0m;
            if (LoanNumInstallments != 0 && loan != null)
            {
                Installment = TransactionService.GetTransactionByInstallment(LoanAmount, (decimal)LoanNumInstallments, loan.LoanInterest!.Value);
            }

            return Installment;
        }

        private PaymentTransaction? GetPaymentTransaction(int LoanNumInstallments, decimal? ContractId)
        {
            PaymentTransaction? PaymentTransaction = _context.PaymentTransactions
                    .Where(c => c.ContractId == ContractId &&
                    c.InstallmentNo == Convert.ToDecimal(LoanNumInstallments))
                    .FirstOrDefault();

            return PaymentTransaction;
        }
    }
}
