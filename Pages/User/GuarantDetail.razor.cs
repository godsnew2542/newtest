using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using LoanApp.Shared;
using static LoanApp.Pages.User.AgreementDetailPage;
using LoanApp.DatabaseModel.LoanEntities;

namespace LoanApp.Pages.User
{
    public partial class GuarantDetail
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        #region Parameter
        /// <summary>
        /// RequestID ที่นำมาค้นข้อมูลต่างๆ เพื่อแสดง ใน page นี้
        /// </summary>
        [Parameter] public decimal RequestID { get; set; } = 0;
        [Parameter] public string StaffID { get; set; } = string.Empty;
        [Parameter] public int FromPage { get; set; } = 0;
        [Parameter] public string Role { get; set; } = string.Empty;
        /// <summary>
        /// LoanApp.Models..RoleTypeEnum
        /// </summary>
        [Parameter] public int newRole { get; set; } = 0;
        /// <summary>
        /// LoanApp.Models.BackRootPageEnum
        /// </summary>
        [Parameter] public int rootPage { get; set; } = 0;
        /// <summary>
        /// RequestID ต้นทางที่จะสามารถกลับไปยังหน้าเดิมได้
        /// </summary>
        [Parameter] public decimal rootRequestID { get; set; } = 0;

        #endregion

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private VLoanStaffDetail? DebtorStaff { get; set; } = new();
        private VLoanStaffDetail? GuarantStaff { get; set; } = new();
        private LoanType? LoanType { get; set; } = new();
        private VLoanRequestContract? Request { get; set; } = new();

        private string[] DayInstallments { get; set; } = new string[0];
        private bool LoadingResultImg { get; set; } = false;
        private List<ListDocModel> ResultDocList { get; set; } = new();

        protected async override Task OnInitializedAsync()
        {
            try
            {
                string GuarantStaff_Id = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
                if (!string.IsNullOrEmpty(StaffID))
                {
                    GuarantStaff_Id = StaffID;
                }

                if (RequestID != 0 && !string.IsNullOrEmpty(GuarantStaff_Id))
                {
                    Request = await psuLoan.GetVLoanRequestContractByRequestId(RequestID);

                    LoanType = await psuLoan.GetLoanTypeAsync(Request?.LoanTypeId);
                    DebtorStaff = await psuLoan.GetUserDetailAsync(Request?.DebtorStaffId);
                    GuarantStaff = await psuLoan.GetUserDetailAsync(GuarantStaff_Id);

                    DateTime? PaidDate = null;
                    if (Request?.PaidDate != null)
                    {
                        PaidDate = Request.PaidDate;
                    }
                    else
                    {
                        PaidDate = Request?.ContractDate;
                    }

                    DayInstallments = await TransactionService.SetDateWithPaymentTransactionAsync(Request?.ContractId,
                       Request?.ContractLoanNumInstallments,
                       PaidDate);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private void BackPage()
        {
            if (newRole != 0)
            {
                if (rootPage != 0)
                {
                    switch (rootPage)
                    {
                        case (int)BackRootPageEnum.Admin_RequestDetail:
                            if (rootRequestID == 0)
                            {
                                return;
                            }
                            else if (FromPage == 0)
                            {
                                navigationManager.NavigateTo($"/{RoleTypeEnum.Admin}/RequestDetail/{rootRequestID}");
                                return;
                            }

                            navigationManager.NavigateTo($"/{newRole}/GuarantDetailPage/{FromPage}/{StaffID}/{RequestID}/{rootPage}/{rootRequestID}");
                            return;


                        case (int)BackRootPageEnum.CheckGurantorAgreement:
                            navigationManager.NavigateTo($"/{newRole}/GuarantDetailPage/{FromPage}/{StaffID}/{RequestID}/{rootPage}/{rootRequestID}");
                            return;

                        case (int)BackRootPageEnum.LoanAgreementOld:
                            navigationManager.NavigateTo($"/{newRole}/GuarantDetailPage/{FromPage}/{StaffID}/{RequestID}/{rootPage}/{rootRequestID}");
                            return;
                    }
                }
            }
            else
            {
                var role = (!string.IsNullOrEmpty(Role) ? "Manager" : "Admin");
                switch (FromPage)
                {
                    case (int)PageControl.AdminCheckGurantorAgreement:
                        navigationManager.NavigateTo($"{role}/GuarantDetailPage/{FromPage}/{StaffID}/{RequestID}");
                        return;
                        //break
                    default: break;
                }
            }

            navigationManager.NavigateTo($"/GuarantDetailPage/{RequestID}");
        }

        public void PdfAgreement()
        {
            var page = "GuarantDetail";
            decimal StepID = 0;
            if (FromPage != 0)
            {
                if (newRole != 0)
                {
                    if (rootPage != 0)
                    {
                        switch (rootPage)
                        {
                            case (int)BackRootPageEnum.Admin_RequestDetail:
                                if (rootRequestID == 0)
                                {
                                    return;
                                }
                                navigationManager.NavigateTo($"/{newRole}/PdfAgreement/{page}/{StepID}/{RequestID}/{FromPage}/{StaffID}/{rootPage}/{rootRequestID}");
                                break;

                            case (int)BackRootPageEnum.CheckGurantorAgreement:
                                navigationManager.NavigateTo($"/{newRole}/PdfAgreement/{page}/{StepID}/{RequestID}/{FromPage}/{StaffID}/{rootPage}/{rootRequestID}");
                                break;
                        }
                    }
                }
                else if (Role != "Manager")
                {
                    navigationManager.NavigateTo($"Admin/PdfAgreement/{page}/{StepID}/{RequestID}/{FromPage}/{StaffID}");
                }
                else
                {
                    navigationManager.NavigateTo($"Manager/PdfAgreement/{page}/{StepID}/{RequestID}/{FromPage}/{StaffID}");
                }
            }
            else
            {
                navigationManager.NavigateTo($"./User/PdfAgreement/{page}/{RequestID}");
            }
        }

        private decimal GetBalanceTotal()
        {
            decimal Balance = 0;
            if (Request != null)
            {
                Balance = TransactionService.GetBalanceTotal(Request.ContractId, Request!.ContractLoanAmount!.Value);
            }
            return Balance;
        }

        private string GetLoanAmount(VLoanRequestContract? ReqCon)
        {
            decimal LoanAmount;
            string mess = "ยอดเงินกู้รวมดอกเบี้ย";
            if (ReqCon != null)
            {
                if (ReqCon.ContractLoanTotalAmount != null)
                {
                    LoanAmount = ReqCon.ContractLoanTotalAmount.Value;
                }
                else
                {
                    LoanAmount = TransactionService.FindLoanTotalAmount(ReqCon.ContractId);
                }
                return $"{mess} : {String.Format("{0:n2}", LoanAmount)} บาท";
            }
            return $"{mess} :";
        }

        private string GetNameGuarantor(VLoanRequestContract? ReqCon)
        {
            string? staffID = string.Empty;
            string mess = string.Empty;

            if (ReqCon != null)
            {
                if (!string.IsNullOrEmpty(ReqCon.ContractGuarantorStaffId))
                {
                    #region Check ผู้ค้ำว่าไปเซ็นสัญญาหรือยัง 
                    staffID = ReqCon.ContractGuarantorStaffId;
                    #endregion
                }
                else
                {
                    staffID = ReqCon.LoanRequestGuaranStaffId;
                }

                if (!string.IsNullOrEmpty(staffID))
                {
                    mess = $"{userService.GetFullName(staffID)} ({GetFacName(staffID)})";
                }
            }

            return mess;
        }

        private string? GetFacName(string staffId)
        {
            string? FacName = null;
            var Detail = userService.GetUserDetail(staffId);
            if (Detail != null)
            {
                FacName = Detail.FacNameThai;
            }
            return FacName;
        }

        private async Task SetLoanDocAsync()
        {
            LoadingResultImg = true;
            ResultDocList = new();
            await Task.Delay(1);
            var step1 = await SaveFileAndImgService.GetDocByStapAsync(1, Request, true);
            var step2 = await SaveFileAndImgService.GetDocByStapAsync(2, Request, true);
            var step3 = await SaveFileAndImgService.GetDocByStapAsync(3, Request, true);
            if (step1 != null)
            {
                ResultDocList.Add(step1);
            }
            if (step2 != null)
            {
                ResultDocList.Add(step2);
            }
            if (step3 != null)
            {
                ResultDocList.Add(step3);
            }

            LoadingResultImg = false;
            StateHasChanged();
        }
    }
}
