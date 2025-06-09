using Microsoft.AspNetCore.Components;
using LoanApp.Shared;
using Microsoft.EntityFrameworkCore;
using LoanApp.DatabaseModel.LoanEntities;
using AntDesign.TableModels;
using LoanApp.Model.Helper;
using LoanApp.Services.Services.LoanDb;
using LoanApp.Model.Models;
using LoanApp.Pages.Admin;

namespace LoanApp.Pages.User
{
    public partial class GuarantAgreement
    {
        #region CascadingParameter
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }
        #endregion

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;


        private List<VLoanRequestContract> ListLoanNow { get; set; } = new();
        private List<VLoanRequestContract> ListLoanSuccess { get; set; } = new();
        private List<VLoanRequestContract> LoanAgreementGuaran { get; set; } = new();
        private List<VLoanStaffDetail> ListStaffIdOld { get; set; } = new();

        private decimal[] StatusIdLoanNow { get; } = new[] { 0m, 100m, 3m, 98m, 99m };
        private decimal[] StatusIdLoanSuccess { get; } = new[] { 3m, 98m, 99m };
        private string FormathDate { get; set; } = "dd-MM-yyyy";
        private string FormathTime { get; set; } = "HH:mm";

        /// <summary>
        /// แสดงเฉพาะสัญญาที่คงอยู่
        /// </summary>
        private bool LoanCheckbook { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            string StaffID = userService.FindStaffId(StateProvider?.CurrentUser.StaffId);

            try
            {
                if (!string.IsNullOrEmpty(StaffID))
                {
                    //var loanNow = await _context.VLoanRequestContracts
                    //    .Where(c => c.LoanRequestGuaranStaffId!.Equals(StaffID) ||
                    //    c.ContractGuarantorStaffId!.Equals(StaffID))
                    //    .Where(c => !StatusIdLoanNow.Contains(c.CurrentStatusId!.Value))
                    //    .Where(c => (c.ContractDate == null) ||
                    //    ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                    //    .ToListAsync();

                    //var loanSuccess = await _context.VLoanRequestContracts
                    //    .Where(c => c.LoanRequestGuaranStaffId!.Equals(StaffID) ||
                    //    c.ContractGuarantorStaffId!.Equals(StaffID))
                    //    .Where(c => StatusIdLoanSuccess.Contains(c.CurrentStatusId!.Value))
                    //    .Where(c => (c.ContractDate == null) ||
                    //    ((c.ContractDate != null) && (c.ContractDate.Value.Year >= Utility.ShowDataYear)))
                    //    .ToListAsync();

                    List<VLoanRequestContract> loanNow = await psuLoan.GetListGuarantorStaffFormVLoanRequestContractByNotStatusReq(StaffID, StatusIdLoanNow.ToList(), Utility.ShowDataYear);

                    List<VLoanRequestContract> loanSuccess = await psuLoan.GetListGuarantorStaffFormVLoanRequestContract(StaffID, StatusIdLoanSuccess.ToList(), Utility.ShowDataYear);

                    if (loanNow.Any())
                    {
                        ListLoanNow = Utility.CheckChangeGuarantor(StaffID, loanNow);
                    }

                    if (loanSuccess.Any())
                    {
                        ListLoanSuccess = Utility.CheckChangeGuarantor(StaffID, loanSuccess);
                    }
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (StateProvider != null && StateProvider.CurrentUser.CapmSelectNow == null)
                {
                    VLoanStaffDetail? vLoanStaffDetail = await psuLoan.GetUserDetailAsync(StateProvider?.CurrentUser.StaffId);

                    if (vLoanStaffDetail != null)
                    {
                        ListStaffIdOld = await psuLoan.GetListStaffIdByStaffPersId(staffPersId: vLoanStaffDetail.StaffPersId, staffId: StateProvider?.CurrentUser.StaffId, isRemoveStaffIdNow: true);

                        if (ListStaffIdOld.Any())
                        {
                            LoanAgreementGuaran = await psuLoan.GetAllLoanForGuarantorOnly(staffIds: ListStaffIdOld.Select(x => x.StaffId).ToList());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check เปลี่ยนผู้ค้ำ หลังทำสัญญา
        /// </summary>
        /// <param name="staffID"></param>
        private List<VLoanRequestContract> CheckChangeGuarantor(string staffID, List<VLoanRequestContract> data)
        {
            List<decimal> removeLoanRequestId = new();

            foreach (var item in data)
            {
                var id = item.LoanRequestId;
                //Check ผู้ค้ำว่าไปเซ็นสัญญาหรือยัง
                if (!string.IsNullOrEmpty(item.ContractGuarantorStaffId))
                {
                    //Check ผู้ค้ำก่อน/ หลัง เซ็นสัญญา ว่าเป็นคนเดี่ยวกันไหม
                    if (item.LoanRequestGuaranStaffId != item.ContractGuarantorStaffId)
                    {
                        //กรณีไม่ใช่คนเดี่ยวกันให้ Check หลังเซ็นสัญญาว่าเป็น คนที่เข้าใช้งานอยู่ไหม
                        if (item.ContractGuarantorStaffId != staffID)
                        {
                            removeLoanRequestId.Add(id);
                        }
                    }
                }
            }

            if (removeLoanRequestId.Any())
            {
                foreach (var id in removeLoanRequestId)
                {
                    var myTodo = data.Find(x => x.LoanRequestId == id);

                    if (myTodo != null)
                    {
                        data.Remove(myTodo);
                    }
                }
            }
            return data;
        }

        private void SeeDetail(RowData<VLoanRequestContract> row)
        {
            VLoanRequestContract agreement = row.Data;
            navigationManager.NavigateTo($"/GuarantDetailPage/{agreement.LoanRequestId}");
        }

        private async Task TopageLoanAgreementOld()
        {
            LoanAgreementOldModel oldModel = new()
            {
                StaffId = StateProvider?.CurrentUser.StaffId,
                StaffList = ListStaffIdOld.Select(x => x.StaffId).ToList(),
                //DebtorRequestId = LoanAgreementDebtor.Select(x => x.LoanRequestId).ToList(),
                GuaranRequestId = LoanAgreementGuaran.Select(x => x.LoanRequestId).ToList(),
                //IsShowDebtorSuccess = true,
                IsShowGuaranSuccess = true,
            };
            await sessionStorage.SetItemAsync("OldAgreement", oldModel);

            navigationManager.NavigateTo($"/{(int)RoleTypeEnum.Staff}/LoanAgreementOld/{(int)BackRootPageEnum.User_Guarantor}");
        }
    }
}
