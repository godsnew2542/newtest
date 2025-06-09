using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace LoanApp.Components.Admin
{
    public partial class PaymentTable
    {
        #region CascadingParameter
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #endregion

        #region Parameter
        [Parameter] public decimal[] StatusId { get; set; } = Array.Empty<decimal>();
        [Parameter] public bool OptionSearchName { get; set; } = true;
        [Parameter] public SearchModel Search { get; set; } = new();
        [Parameter] public decimal LoanTypeID { get; set; } = 0m;

        #endregion

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        #endregion

        private List<VLoanRequestContract> ListContract { get; set; } = new();
        private PanelFooterModel Footer { get; set; } = new();
        private List<PaymentTransaction> SelectRequest { get; set; } = new();

        private List<decimal> SelectRequestId { get; set; } = new();
        private bool IsAgreementSuccess { get; set; } = false;
        private DateTime DateValueCheck { get; set; } = DateTime.Now;
        public object AutoAlert { get; set; } = null!;

        protected override void OnInitialized()
        {
            SelectRequest = new();
            Footer = new();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await StartTableAsync();
                StateHasChanged();
            }
        }

        private async Task StartTableAsync()
        {
            var total = await CountVLoanRequestContractsAsync();
            SetUserView(total);
            await DataTableAsync(0, Footer.Limit, Search.Title, LoanTypeID);
        }

        protected void SetUserView(int count)
        {
            if (count > 0)
            {
                Footer.Count = count;
                Footer.TotalPages = (int)Math.Ceiling(count / (double)Footer.Limit);
            }
        }

        private void SelectData(VLoanRequestContract data)
        {
            SelectRequestId = new();
            SelectRequest = new();

            PaymentTransaction Payment = new();
            int PaymentNumInstallments = _context.PaymentTransactions
               .Where(c => c.ContractId == data.ContractId)
               .Select(c => new PaymentTransaction
               {
                   ContractId = c.ContractId,
                   InstallmentNo = c.InstallmentNo
               })
               .Distinct()
               .Count();
            IsAgreementSuccess = false;

            if (PaymentNumInstallments != data.ContractLoanNumInstallments)
            {
                Payment = SetTransaction(data, PaymentNumInstallments, 1);
                InstallmentDetail? IDetail = _context.InstallmentDetails
                    .Where(c => c.ContractId == data.ContractId)
                     .Where(c => c.InstallmentNo == Payment.InstallmentNo)
                    .FirstOrDefault();

                if (IDetail != null)
                {
                    Payment.PrincipleAmount = IDetail.PrincipleAmount;
                    Payment.InterestAmont = IDetail.InterestAmont;
                    Payment.TotalAmount = IDetail.PrincipleAmount + IDetail.InterestAmont;
                }

                if (PaymentNumInstallments != 0)
                {
                    PaymentTransaction? Transaction = _context.PaymentTransactions
                        .Where(c => c.ContractId == data.ContractId &&
                        c.InstallmentNo == PaymentNumInstallments)
                        .FirstOrDefault();
                    Payment.BalanceAmount = Transaction?.BalanceAmount;
                }
                SelectRequest.Add(Payment);
            }
            else
            {
                IsAgreementSuccess = true;
            }

            DateValueCheck = DateTime.Now;
            SelectRequestId.Add(data.LoanRequestId);
        }

        protected async Task SelectPageSizeAsync(ChangeEventArgs e)
        {
            Footer.Limit = Convert.ToInt32(e.Value!.ToString());
            Footer.TotalPages = (int)Math.Ceiling(Footer.Count / (double)Footer.Limit);
            Footer.CurrentPage = 1;
            await UpdateListAsync(Footer.CurrentPage);
        }

        protected async Task NavigateToAsync(string Direction)
        {
            if (Direction == "Prev" && Footer.CurrentPage != 1)
            {
                Footer.CurrentPage -= 1;
            }
            if (Direction == "Next" && Footer.CurrentPage != Footer.TotalPages)
            {
                Footer.CurrentPage += 1;
            }
            if (Direction == "First")
            {
                Footer.CurrentPage = 1;
            }
            if (Direction == "Last")
            {
                Footer.CurrentPage = Footer.TotalPages;
            }

            await UpdateListAsync(Footer.CurrentPage);
        }

        protected async Task SelectCurrentPageAsync(ChangeEventArgs e)
        {
            Footer.CurrentPage = Convert.ToInt32(e.Value!.ToString());
            await UpdateListAsync(Footer.CurrentPage);
        }

        protected async Task UpdateListAsync(int CurPage)
        {
            var statr = (Footer.Limit * CurPage) - Footer.Limit;
            Footer.CurrentPage = CurPage;
            await DataTableAsync(statr, Footer.Limit, Search.Title, LoanTypeID);
        }

        private async Task DataTableAsync(int start, int end, string? searchName, decimal typeId)
        {
            ListContract = new();

            if (!string.IsNullOrEmpty(searchName) && typeId != 0)
            {
                if (OptionSearchName)
                {
                    ListContract = await _context.VLoanRequestContracts
                        .Where(c => c.DebtorNameTh!.Contains(searchName) ||
                        c.DebtorSnameTh!.Contains(searchName) ||
                        (c.DebtorNameEng!).ToLower().Contains(searchName.ToLower()) ||
                        (c.DebtorSnameEng!).ToLower().Contains(searchName.ToLower()) ||
                        (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                        (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower()))
                        .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                        .Where(c => c.LoanTypeId == typeId)
                        .Skip(start)
                        .Take(end)
                        .ToListAsync();
                }
                else
                {
                    ListContract = await _context.VLoanRequestContracts
                        .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                        .Where(c => c.LoanTypeId == typeId)
                        .Where(c => c.ContractNo!.Contains(searchName))
                        .Skip(start)
                        .Take(end)
                        .ToListAsync();
                }
            }
            else if (!string.IsNullOrEmpty(searchName))
            {
                if (OptionSearchName)
                {
                    ListContract = await _context.VLoanRequestContracts
                        .Where(c => c.DebtorNameTh!.Contains(searchName) ||
                        c.DebtorSnameTh!.Contains(searchName) ||
                        (c.DebtorNameEng!).ToLower().Contains(searchName.ToLower()) ||
                        (c.DebtorSnameEng!).ToLower().Contains(searchName.ToLower()) ||
                        (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(searchName) ||
                        (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(searchName.ToLower()))
                        .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                        .Skip(start)
                        .Take(end)
                        .ToListAsync();
                }
                else
                {
                    ListContract = await _context.VLoanRequestContracts
                    .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                    .Where(c => c.ContractNo!.Contains(searchName))
                    .Skip(start)
                    .Take(end)
                    .ToListAsync();
                }
            }
            else if (typeId != 0)
            {
                ListContract = await _context.VLoanRequestContracts
                    .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                    .Where(c => c.LoanTypeId == typeId)
                    .Skip(start)
                    .Take(end)
                    .ToListAsync();
            }
            else
            {
                //var total = CountVLoanRequestContractsAsync()

                ListContract = await _context.VLoanRequestContracts
                    .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                    .Skip(start)
                    .Take(end)
                    .ToListAsync();
            }
        }

        private async Task<int> CountVLoanRequestContractsAsync()
        {
            var total = await _context.VLoanRequestContracts
                .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                .CountAsync();
            return total;
        }

        private static PaymentTransaction SetTransaction(VLoanRequestContract data, int PaymentNumInstallments, int Number)
        {
            PaymentTransaction Payment = new()
            {
                ContractId = data.ContractId,
                InstallmentNo = PaymentNumInstallments + Number,
                PrincipleAmount = 0,
                InterestAmont = 0,
                TotalAmount = 0,
                ContractNo = data.ContractNo,
                BalanceAmount = data.ContractLoanTotalAmount
            };
            return Payment;
        }

        private void GetDate(DateTime? Date)
        {
            DateValueCheck = dateService.ConvertToDateTime(Date);
        }

        private static decimal? ResultTotalAmount(decimal? PrincipleAmount, decimal? InterestAmont)
        {
            decimal? result = PrincipleAmount + InterestAmont;
            return result == null ? 0 : result;
        }

        private async Task ConfirmPageAsync(List<PaymentTransaction> listTransaction, DateTime PaidDate)
        {
            try
            {
                for (int i = 0; i < listTransaction.Count; i++)
                {
                    var transaction = listTransaction[i];
                    var Contract = _context.ContractMains
                        .Where(c => c.ContractId == transaction.ContractId)
                        .FirstOrDefault();

                    var getReq = userService.GetVLoanRequestContract(Contract?.LoanRequestId);
                    if (getReq != null)
                    {
                        await SaveToDBAsync(PaidDate, transaction, getReq);
                    }
                }

                await JS.InvokeVoidAsync("alertAuto", AutoAlert, 1500);
                ListContract = new();
                Search = new();
                Footer = new();
                SelectRequestId = new();
                await SearchDataAsync(Search.Title, OptionSearchName, LoanTypeID);
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task SaveToDBAsync(DateTime PaidDate, PaymentTransaction transaction, VLoanRequestContract reqCon)
        {
            decimal? ContractStatusId = 0;
            try
            {
                if (reqCon.ContractId != null)
                {
                    ContractMain? main = _context.ContractMains
                        .Where(c => c.ContractId == reqCon.ContractId)
                        .FirstOrDefault();

                    /// bugData (ImportPayment) การเช็คการ สิ้นสุดสัญญา เข็คจาก BalanceAmount == 0
                    /// bugData (PaymentTable) รอสอบถาม การเช็คการ สิ้นสุดสัญญา โดยจะเช็คจาก ??? แต่ตแนนี้ใส่ เข็คจาก งวด ไปก่อน
                    decimal? BalanceAmount = transaction.BalanceAmount - transaction.TotalAmount;
                    bool IsLast = false;

                    #region เช็คจากงวด
                    if (main != null)
                    {
                        if (main.LoanNumInstallments == transaction.InstallmentNo)
                        {
                            ContractStatusId = main.ContractStatusId;
                            main.ContractStatusId = 99;
                            IsLast = true;
                            _context.Update(main);
                            await _context.SaveChangesAsync();

                            await SaveToHistoryAsync(main, ContractStatusId);
                        }
                        // Ex 10 (จำนวนงวดที่ข้อมา) < 11 (จำนวนงวดที่ กรอกมาจากหน้าที่จะจ่าย)
                        else if (main.LoanNumInstallments < transaction.InstallmentNo)
                        {
                            string alert = $"กรุณาตรวจสอบงวดที่จะทำการจ่ายเงิน \n" +
                                $"ที่กรอกมาจากหน้าที่จ่ายคืองวด = {transaction.InstallmentNo} \n" +
                                $"ตอนข้อกู้ {main.LoanNumInstallments} งวด \n" +
                                $"ดังนั้นจึงทำรายการไม่ได้";
                            await JS.InvokeVoidAsync("displayTickerAlert", alert);
                            return;
                        }
                    }
                    #endregion

                    transaction.BalanceAmount = BalanceAmount;
                    transaction.PayDate = PaidDate;
                    transaction.CreatedBy = StateProvider?.CurrentUser.StaffId;
                    transaction.CreatedDate = DateTime.Now;

                    await psuLoan.AddPaymentTransaction(transaction);
                    await SetDataBySentEmailAsync(reqCon, transaction, IsLast);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        public async Task SearchDataAsync(string? text, bool OptionSearch, decimal typeId)
        {
            Search.Title = (!string.IsNullOrEmpty(text) ? text : string.Empty);
            Footer.CurrentPage = 1;
            LoanTypeID = typeId;
            int total = 0;
            if (!string.IsNullOrEmpty(text) && typeId != 0)
            {
                if (OptionSearchName)
                {
                    total = await _context.VLoanRequestContracts
                        .Where(c => c.DebtorNameTh!.Contains(text) ||
                        c.DebtorSnameTh!.Contains(text) ||
                        (c.DebtorNameEng!).ToLower().Contains(text.ToLower()) ||
                        (c.DebtorSnameEng!).ToLower().Contains(text.ToLower()) ||
                        (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(text) ||
                        (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(text.ToLower()))
                        .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                        .Where(c => c.LoanTypeId == typeId)
                        .CountAsync();
                }
                else
                {
                    total = await _context.VLoanRequestContracts
                        .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                        .Where(c => c.LoanTypeId == typeId)
                        .Where(c => c.ContractNo!.Contains(text))
                        .CountAsync();
                }
                await SumTable(total, text, typeId);
            }
            else if (!string.IsNullOrEmpty(text))
            {
                OptionSearchName = OptionSearch;
                Search.OptionSearchName = OptionSearchName;

                if (OptionSearchName)
                {
                    total = await _context.VLoanRequestContracts
                        .Where(c => c.DebtorNameTh!.Contains(text) ||
                        c.DebtorSnameTh!.Contains(text) ||
                        (c.DebtorNameEng!).ToLower().Contains(text.ToLower()) ||
                        (c.DebtorSnameEng!).ToLower().Contains(text.ToLower()) ||
                        (c.DebtorNameTh + " " + c.DebtorSnameTh).Contains(text) ||
                        (c.DebtorNameEng + " " + c.DebtorSnameEng).ToLower().Contains(text.ToLower()))
                        .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                        .CountAsync();
                }
                else
                {
                    total = await _context.VLoanRequestContracts
                        .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                        .Where(c => c.ContractNo!.Contains(text))
                        .CountAsync();
                }
                await SumTable(total, text, typeId);
            }
            else if (typeId != 0)
            {
                total = await _context.VLoanRequestContracts
                    .Where(c => !StatusId.Contains(c.CurrentStatusId!.Value))
                    .Where(c => c.LoanTypeId == typeId)
                    .CountAsync();

                await SumTable(total, text, typeId);
            }
            else
            {
                await StartTableAsync();
            }

        }

        private async Task SumTable(int total, string? text, decimal typeId)
        {
            if (total != 0)
            {
                SetUserView(total);
                await DataTableAsync(0, Footer.Limit, (!string.IsNullOrEmpty(text) ? text : string.Empty), typeId);
            }
            else
            {
                ListContract = new();
                SetUserView(1);
            }
        }

        private async Task SaveToHistoryAsync(ContractMain contract, decimal? ContractStatusId)
        {
            decimal? LoanStatusId = ContractStatusId;
            string ModifyBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

            await LogService.GetHisContractMainByContractIDAsync(contract.ContractId, LoanStatusId, ModifyBy);
        }

        private async Task SetDataBySentEmailAsync(VLoanRequestContract ReqCon, PaymentTransaction transaction, bool IsLast)
        {
            try
            {
                var StaffDetail = userService.GetUserDetail(ReqCon.DebtorStaffId);
                var DebtorName = userService.GetFullNameNoTitleName(ReqCon.DebtorStaffId);

                var GuarantorDetail = userService.GetUserDetail(ReqCon.ContractGuarantorStaffId);
                var GuarantoName = userService.GetFullNameNoTitleName(ReqCon.ContractGuarantorStaffId);

                ApplyLoanModel loan = new();
                loan.LoanTypeID = ReqCon.LoanTypeId;
                loan.LoanAmount = ReqCon.ContractLoanAmount != null ? ReqCon.ContractLoanAmount.Value : 0;

                #region ผู้กู้
                if (!string.IsNullOrEmpty(StaffDetail?.StaffEmail))
                {
                    var Email = string.Empty;
                    var Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);
                    Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                    var email = MailService.MailDebtorByAdminImportPayment(Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        transaction,
                        IsLast);
                    MailService.SendEmail(email);
                }
                #endregion

                #region ผู้ค้ำ
                if (!string.IsNullOrEmpty(GuarantorDetail?.StaffEmail))
                {
                    var Email = string.Empty;
                    var Name = userService.GetFullName(GuarantorDetail.StaffId);
                    Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                    var email = MailService.MailDebtorByAdminImportPayment(Name,
                        Email,
                        DebtorName,
                        GuarantoName,
                        loan,
                        transaction,
                        IsLast);
                    MailService.SendEmail(email);
                }
                #endregion
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task<int?> GetNumInstallmentsAsync(decimal? ContractId)
        {
            int? InstallmentsNo = null;
            if (ContractId != null)
            {
                int transactionCount = await _context.PaymentTransactions
                    .Where(c => c.ContractId == ContractId)
                    .Select(c => new PaymentTransaction
                    {
                        ContractId = c.ContractId,
                        InstallmentNo = c.InstallmentNo
                    })
                    .Distinct()
                    .CountAsync();

                if (transactionCount != 0)
                {
                    InstallmentsNo = transactionCount + 1;
                }
                else
                {
                    InstallmentsNo = 1;
                }
            }
            return InstallmentsNo;
        }
    }
}
