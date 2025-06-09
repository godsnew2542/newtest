using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using LoanApp.Shared;
using LoanApp.Components.Admin;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Services.IServices;

namespace LoanApp.Pages.Admin
{
    public partial class RecordFirstPayment
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private List<SetNewRecordFirstPaymentModel> ListRecordFirstPayment { get; set; } = new();
        private List<DistinctRecordFirstPayment> ListDistinctRecord { get; set; } = new();
        private List<DistinctRecordFirstPayment> ListDistinctDb { get; set; } = new();
        private SearchModel Search { get; set; } = new();
        private List<LoanType> LoanTypeList { get; set; } = new();
        private FirstPaymentTable RefFirstPaymentTable { get; set; } = new();

        private decimal[] AllStatusId { get; } = new[] { 9m };
        private bool IsLoading { get; set; } = false;
        private DateTime PaymentTime { get; set; } = DateTime.Now;
        private DateTime? contractDate { get; set; }
        private string? SearchView { get; set; } = string.Empty;
        private decimal TypeID { get; set; } = 0m;

        protected override async Task OnInitializedAsync()
        {
            TypeID = 0m;

            try
            {
                LoanTypeList = await psuLoan.GetAllLoanType();
                LoanTypeList.Insert(0, new LoanType()
                {
                    LoanTypeId = 0,
                    LoanTypeName = "ทุกประเภท"
                });
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        public static DistinctRecordFirstPayment AddDistinctRecord(SetNewRecordFirstPaymentModel contract,
            SetNewRecordFirstPaymentModel _Contract)
        {
            DistinctRecordFirstPayment DistinctRecord = new()
            {
                ContractId = contract.ContractId,
                ContractNo = contract.ContractNo,
                DistinctContractId = _Contract.ContractId,
                DistinctContractNo = _Contract.ContractNo,
            };

            return DistinctRecord;
        }

        public void SetCurrentData(List<SetNewRecordFirstPaymentModel> value)
        {
            ListRecordFirstPayment = new();
            ListRecordFirstPayment = value;
        }

        public static MailModel MessageDebtor(string name, string emailUser, string debtorName, string guarantoName, ApplyLoanModel loan, string id, DateTime? paiddate, int countrequire, DateTime? _contractDate, IUserService _userService, IDateService _dateService)
        {
            var loanData = _userService.GetLoanType(loan.LoanTypeID);
            var Time = _dateService.ChangeDate(_contractDate, "dd/MM/yyyy", Utility.DateLanguage_TH);
            var Paid = _dateService.ChangeDate(paiddate, "dd/MM/yyyy", Utility.DateLanguage_TH);

            MailModel email = new()
            {
                Title = $"(PSU LOAN) โอนเงินกู้เรียบร้อยแล้ว",
                Name = name,
                Email = emailUser,
                Time = DateTime.Now,
                Message = $"เรียนคุณ{name}" +
                $"\n " +
                $"\n ตามที่ คุณ{debtorName} (ผู้กู้) ได้ยื่นกู้ โดยคุณ{guarantoName} (ผู้ค้ำ) เป็นผู้ค้ำนั้น" +
                $"\n ได้ทำสัญญากู้ยืมเงินเรียบร้อยแล้ว เมื่อวันที่ {Time}" +
                $"\n " +
                $"โดยมีรายละเอียดการกู้ ดังนี้ " +
                $"\n เลขที่สัญญา {id} " +
                $"\n ประเภท{loanData?.LoanParentName} " +
                $"\n วงเงินที่ขอกู้จำนวน {string.Format("{0:n2}", loan.LoanAmount)} บาท " +
                $"\n ดอกเบี้ย {loan.LoanInterest} % " +
                $"\n จำนวนงวด {loan.LoanNumInstallments} งวด " +
                $"\n" +
                $"\n ขณะนี้กองคลังได้โอนเงินกู้เข้าบัญชีธนาคารเงินเดือน/บัญชีสำหรับรับเงินกู้ของผู้กู้เรียบร้อยแล้ว เมื่อวันที่ {Paid} " +
                $"\n" +
                $"\n {(countrequire == 0 ? "" : "กรุณาตรวจสอบยอดเงินที่ได้รับ และส่งหลักฐานใบเสร็จหลังรับเงินกู้ในระบบภายใน30วัน ให้ครบถ้วน")}" +
                $"\n" +
                $"\n สามารถดูรายละเอียดการกู้/การค้ำประกันได้ที่ loan.psu.ac.th " +
                $"\n อีเมลฉบับนี้เป็นการแจ้งข้อมูลจากระบบโดยอัตโนมัติ กรุณาอย่าตอบกลับ" +
                $"\n "
            };
            return email;
        }

        private void RemoveListRecordFirstPayment(SetNewRecordFirstPaymentModel myTodo)
        {
            ListRecordFirstPayment.Remove(myTodo);
        }

        private string AddBackgroundColor(SetNewRecordFirstPaymentModel data)
        {
            string Attr = string.Empty;

            if (data.IsDistinct)
            {
                Attr = "first-payment-bg-salmon";
            }

            if (data.IsDistinctDB)
            {
                Attr = "first-payment-bg-darkseagreen";
            }
            return Attr;
        }

        private async Task CheckRecordFirstPayment()
        {
            IsLoading = true;
            ListDistinctRecord = new();
            ListDistinctDb = new();
            List<decimal?> CheckContractId = new();

            if (ListRecordFirstPayment.Any())
            {
                foreach (var contract in ListRecordFirstPayment)
                {
                    contract.IsDistinct = false;
                    contract.IsDistinctDB = false;

                    var _Contract = ListRecordFirstPayment.Find(x => x.ContractNo == contract.ContractNo);

                    ContractMain? conMain = await psuLoan.GeContractMainByContractNo(contract.ContractNo);

                    if (conMain != null)
                    {

                        SetNewRecordFirstPaymentModel NewRecord = new()
                        {
                            ContractId = conMain.ContractId,
                            ContractNo = conMain.ContractNo
                        };

                        ListDistinctDb.Add(AddDistinctRecord(NewRecord, contract));

                        contract.IsDistinctDB = true;
                    }
                    else if (_Contract != null && (_Contract.ContractId != contract.ContractId))
                    {

                        if (CheckContractId.Any())
                        {
                            var _CheckData = CheckContractId.Find(x => x == contract.ContractId);
                            if (_CheckData == null)
                            {
                                ListDistinctRecord.Add(AddDistinctRecord(contract, _Contract));
                            }
                        }
                        else
                        {
                            ListDistinctRecord.Add(AddDistinctRecord(contract, _Contract));
                        }

                        contract.IsDistinct = true;
                        _Contract.IsDistinct = true;

                        CheckContractId.Add(_Contract.ContractId);
                    }
                }
            }

            IsLoading = false;
        }

        private ContractMain GetContractMain(decimal? contractId)
        {
            //ContractMain? ConMain = _context.ContractMains
            //    .Where(c => c.contractId == contractId)
            //    .FirstOrDefault();

            //if (ConMain == null)
            //{
            //    ConMain = new();
            //}

            return psuLoan.GeContractMainByContractId(contractId);
        }

        private async Task ChangeContractNo(DistinctRecordFirstPayment data)
        {
            var myTodo = ListRecordFirstPayment.Find(x => x.ContractId == data.ContractId);

            if (myTodo != null)
            {
                myTodo.ContractNo = data.ContractNo;

                /// Update UI
                if (RefFirstPaymentTable != null)
                {
                    RefFirstPaymentTable.RefreshContractNo(myTodo.ContractId, myTodo.ContractNo);
                }

                if (string.IsNullOrEmpty(myTodo.ContractNo))
                {
                    RemoveListRecordFirstPayment(myTodo);
                }
            }

            var distinctmyTodo = ListRecordFirstPayment.Find(x => x.ContractId == data.DistinctContractId);

            if (distinctmyTodo != null)
            {
                distinctmyTodo.ContractNo = data.DistinctContractNo;

                /// Update UI
                if (RefFirstPaymentTable != null)
                {
                    RefFirstPaymentTable.RefreshContractNo(distinctmyTodo.ContractId, distinctmyTodo.ContractNo);
                }

                if (string.IsNullOrEmpty(distinctmyTodo.ContractNo))
                {
                    RemoveListRecordFirstPayment(distinctmyTodo);
                }
            }

            await CheckRecordFirstPayment();
        }

        private void NewOnChange(DateTime? value)
        {
            PaymentTime = dateService.ConvertToDateTime(value);
        }

        private async Task SearchDataAsync(string? text, decimal typeId)
        {
            if (RefFirstPaymentTable != null)
            {
                Search.Title = (!string.IsNullOrEmpty(text) ? text : string.Empty);
                await RefFirstPaymentTable.DataTableV2(text, typeId, 1);

                if (ListRecordFirstPayment.Any())
                {
                    await RefreshUi(ListRecordFirstPayment);
                }
            }
        }

        private async Task SaveToDbAsync(List<SetNewRecordFirstPaymentModel> array)
        {
            decimal StepId = 3m;

            try
            {
                string staffId = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

                for (int i = 0; i < array.Count; i++)
                {
                    var data = array[i];
                    decimal StatusId = 8m;
                    decimal? ContractStatusId = 0;

                    ContractMain? contract = await _context.ContractMains
                        .Where(c => c.ContractId == data.ContractId)
                        .FirstOrDefaultAsync();

                    if (contract != null)
                    {
                        LoanType? loan = await psuLoan.GetLoanTypeAsync((byte?)contract.LoanTypeId);

                        int CountAttachmentRequired = await _context.VAttachmentRequireds
                            .Where(c => c.LoanTypeId == contract.LoanTypeId)
                            .Where(c => c.ContractStepId == StepId)
                            .CountAsync();

                        if (CountAttachmentRequired != 0)
                        {
                            StatusId = 6m;
                        }

                        contractDate = contract.ContractDate;
                        ContractStatusId = contract.ContractStatusId;

                        contract.ContractNo = data.ContractNo;
                        contract.PaidDate = PaymentTime;
                        contract.PaidStaffId = staffId;
                        contract.ContractStatusId = StatusId;
                        contract.LoanInstallment = SetLoanInstallment(contract.LoanAmount!.Value, (int)contract.LoanNumInstallments!, loan);
                        contract.BudgetYear = userService.GetFiscalYear(PaymentTime);

                        LoanLendingAmount lendingAmount = new()
                        {
                            ContractId = contract.ContractId,
                            LendingDate = contract.PaidDate,
                            LendingAmount = contract.LoanAmount,
                            AdminRecord = staffId,
                        };

                        List<InstallmentDetail> installmentList = SetInstallmentDetail(contract, PaymentTime, loan);

                        contract.LoanTotalAmount = installmentList.Sum(c => c.TotalAmount);

                        #region Add Or Update To DB
                        await psuLoan.UpdateContractMain(contract);
                        await SaveToHistoryAsync(contract, ContractStatusId);

                        await psuLoan.AddLoanLendingAmount(lendingAmount);
                        await psuLoan.AddMutilateDataInstallmentDetail(installmentList);

                        #endregion

                        // sent mail
                        SetDataBySentEmail(contract);
                    }
                }
                navigationManager.NavigateTo("/Admin/ManageLoanAgreement");
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private void SetDataBySentEmail(ContractMain contract)
        {
            try
            {
                var StaffDetail = userService.GetUserDetail(contract.DebtorStaffId);
                var DebtorName = userService.GetFullNameNoTitleName(contract.DebtorStaffId);

                var GuarantorDetail = userService.GetUserDetail(contract.GuarantorStaffId);
                var GuarantoName = userService.GetFullNameNoTitleName(contract.GuarantorStaffId);

                ApplyLoanModel loan = new();
                loan.LoanAmount = contract.LoanAmount != null ?
                    contract.LoanAmount.Value :
                    0;
                loan.LoanTypeID = (byte?)contract.LoanTypeId;
                loan.LoanInterest = contract.LoanInterest;
                loan.LoanNumInstallments = contract.LoanNumInstallments != null ?
                    (int)contract.LoanNumInstallments :
                    0;
                var ID = contract.ContractNo;
                var PaidDate = contract.PaidDate;

                int CountRequired = _context.VAttachmentRequireds
                    .Where(c => c.LoanTypeId == loan.LoanTypeID)
                    .Where(c => c.ContractStepId == 3)
                    .Count();

                var Name = string.Empty;
                var Email = string.Empty;

                #region ผู้กู้
                if (!string.IsNullOrEmpty(StaffDetail?.StaffEmail))
                {

                    Name = userService.GetFullNameNoTitleName(StaffDetail.StaffId);
                    Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                    var email = MessageDebtor(Name, Email, DebtorName, GuarantoName, loan, ID!, PaidDate, CountRequired, contractDate, userService, dateService);
                    MailService.SendEmail(email);
                }
                #endregion

                #region ผู้ค้ำ
                if (!string.IsNullOrEmpty(GuarantorDetail?.StaffEmail))
                {

                    Name = userService.GetFullNameNoTitleName(GuarantorDetail.StaffId);
                    Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                    var email = MessageDebtor(Name, Email, DebtorName, GuarantoName, loan, ID!, PaidDate, CountRequired, contractDate, userService, dateService);
                    MailService.SendEmail(email);
                }
                #endregion
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// จ่ายต่องวด
        /// </summary>
        /// <param name="LoanAmount"></param>
        /// <param name="LoanNumInstallments"></param>
        /// <param name="loan"></param>
        /// <returns></returns>
        private decimal SetLoanInstallment(decimal LoanAmount, int LoanNumInstallments, LoanType? loan)
        {
            var Installment = 0m;
            if (loan != null && LoanNumInstallments != 0)
            {
                Installment = TransactionService.GetTransactionByInstallment(LoanAmount, LoanNumInstallments, loan!.LoanInterest!.Value);
            }

            return Installment;
        }

        private async Task SaveToHistoryAsync(ContractMain contract, decimal? ContractStatusId)
        {
            try
            {
                decimal? Old_LoanStatusId = ContractStatusId;
                var ModifyBy = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

                await LogService.GetHisContractMainByContractIDAsync(contract.ContractId, Old_LoanStatusId, ModifyBy);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        ///  คำนวนดอกเบี้ย
        /// </summary>
        /// <param name="payDate"></param>
        /// <param name="NumInstallments"></param>
        /// <param name="BalanceAmount"></param>
        /// <param name="LoanInterest"></param>
        /// <param name="Contract"></param>
        /// <param name="PaidDate"></param>
        /// <returns></returns>
        private decimal GetInterest(DateTime payDate, int NumInstallments, decimal BalanceAmount, decimal? LoanInterest, ContractMain Contract, DateTime PaidDate)
        {
            var TotalInstallments = Convert.ToDecimal(Contract.LoanNumInstallments);
            var _BalanceAmount = BalanceAmount;

            decimal Interest = TransactionService.GetTransactionByInterest(TotalInstallments, NumInstallments, PaidDate, payDate, _BalanceAmount, LoanInterest!.Value);
            return Interest;
        }

        private List<InstallmentDetail> SetInstallmentDetail(ContractMain contract, DateTime date, LoanType? loan)
        {
            List<InstallmentDetail> InstallmentList = new();
            try
            {
                List<DateTime> ListDate = TransactionService.SetPayDateReturnDateTime(date, contract.LoanNumInstallments!.Value);

                decimal balanceAmount = contract.LoanAmount!.Value;

                if (loan != null)
                {
                    for (int i = 0; i < contract.LoanNumInstallments; i++)
                    {
                        var index = i + 1;
                        DateTime dueDate = ListDate[i];

                        var PaidInstallment = SetLoanInstallment(contract.LoanAmount.Value, (int)contract.LoanNumInstallments, loan);

                        var Interest = GetInterest(dueDate, index, balanceAmount, loan.LoanInterest, contract, date);

                        var Balance = PaidInstallment - Interest;

                        // งวดสุดท้าย
                        if (index == contract.LoanNumInstallments)
                        {
                            Balance = balanceAmount;
                            PaidInstallment = balanceAmount + Interest;
                        }
                        balanceAmount = balanceAmount - Balance;

                        InstallmentDetail installment = new()
                        {
                            ContractId = contract.ContractId,
                            InstallmentNo = index,
                            DueDate = dueDate,
                            PrincipleAmount = Balance,
                            InterestAmont = Interest,
                            TotalAmount = PaidInstallment,
                        };

                        InstallmentList.Add(installment);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return InstallmentList;
        }

        private async Task SelectLoanTypeIDAsync(ChangeEventArgs e)
        {
            TypeID = Convert.ToDecimal(e.Value!.ToString());
            await SearchDataAsync(Search.Title, TypeID);
        }

        private async Task RefreshUi(List<SetNewRecordFirstPaymentModel> result)
        {
            if (RefFirstPaymentTable != null)
            {
                await RefFirstPaymentTable.RefreshAllContractNo(result);
            }
        }
    }

    public class DistinctRecordFirstPayment
    {
        public decimal? ContractId { get; set; }
        public string? ContractNo { get; set; } = string.Empty;
        public decimal? DistinctContractId { get; set; }
        public string? DistinctContractNo { get; set; } = string.Empty;
    }
}
