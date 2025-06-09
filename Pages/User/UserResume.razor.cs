using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using LoanApp.Shared;
using Microsoft.JSInterop;
using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Pages.User
{
    public partial class UserResume
    {
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        private VLoanStaffDetail? StaffDetail { get; set; } = new();
        private VStaffAddress StaffAddress { get; set; } = new();
        private VStaffFamily StaffFamily { get; set; } = new();
        private MonthModel MonthStering { get; set; } = new();
        private List<SelectModel> Select { get; set; } = new();
        private SelectModel SelectBank { get; set; } = new();
        private LoanStaffDetail DebtorDetail { get; set; } = new();
        private LoanStaffDetail SelectDebtorDetail { get; set; } = new();

        private bool IsEditMobileTel { get; set; } = false;
        private bool IsEditOfficeTel { get; set; } = false;
        private string StaffID { get; set; } = string.Empty;
        private bool changType { get; set; } = false;
        private string MessBookBank { get; set; } = string.Empty;

        protected async override Task OnInitializedAsync()
        {
            IsEditMobileTel = false;
            IsEditOfficeTel = false;

            try
            {
                StaffID = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
                if (!string.IsNullOrEmpty(StaffID))
                {
                    StaffDetail = await psuLoan.GetUserDetailAsync(StaffID);
                    var Address = await psuLoan.GetUserAddressesAsync(StaffID);
                    var family = await psuLoan.GetUserFamilyAsync(StaffID);
                    Select = SetDataSelect();
                    await GetDebtorDetail();

                    if (Address != null)
                    {
                        StaffAddress = Address;
                    }
                    if (family != null)
                    {
                        StaffFamily = family;
                    }
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task GetDebtorDetail()
        {
            SelectDebtorDetail = new();
            DebtorDetail = new();

            LoanStaffDetail? loanStaffDetail = await psuLoan.GetLoanStaffDetailByStaffId(StaffID);

            if (loanStaffDetail != null)
            {
                DebtorDetail = loanStaffDetail;
                SelectDebtorDetail.MobileTel = DebtorDetail.MobileTel;
                SelectDebtorDetail.OfficeTel = DebtorDetail.OfficeTel;
            }
            DataBookBank(DebtorDetail);

            //DebtorDetail = _context.LoanStaffDetails
            //        .Where(c => c.StaffId == StaffID)
            //        .FirstOrDefault();

            //if (DebtorDetail == null)
            //{
            //    DebtorDetail = new LoanStaffDetail();
            //}
            //else
            //{
            //    SelectDebtorDetail.MobileTel = DebtorDetail.MobileTel;
            //    SelectDebtorDetail.OfficeTel = DebtorDetail.OfficeTel;
            //}
            //DataBookBank(DebtorDetail);


        }

        private List<SelectModel> SetDataSelect()
        {
            List<SelectModel> SModel = new()
            {
                new SelectModel { Name = "ธนาคารกรุงเทพ", ID = 1 },
                new SelectModel { Name = "ธนาคารกรุงไทย", ID = 2 },
                new SelectModel { Name = "ธนาคารไทยพาณิชย์", ID = 3 },
                new SelectModel { Name = "สหกรณ์ออมทรัพย์ ม.อ.", ID = 4 }
            };
            return SModel;
        }

        private void ToggleButton(bool close)
        {
            if (close)
            {
                changType = false;
            }
            else
            {
                changType = !changType;
            }
        }

        private void SelectType(SelectModel select)
        {
            SelectBank = new();
            SelectBank = select;
            SelectDebtorDetail.BookBankName = select.Name;

            ToggleButton(true);
        }

        private string GetStaffRemainWorking(VLoanStaffDetail? staff)
        {
            var mess = string.Empty;
            if (staff != null)
            {
                if (staff.StaffRemainWorkingYear > 0)
                {
                    mess = $"{mess} {staff.StaffRemainWorkingYear} ปี";
                }

                if (staff.StaffRemainWorkingMonth > 0)
                {
                    mess = $"{mess} {staff.StaffRemainWorkingMonth} เดือน";
                }

                if ((staff.StaffRemainWorkingYear == 0 &&
                    staff.StaffRemainWorkingMonth == 0) ||
                    (staff.StaffRemainWorkingYear == null &&
                    staff.StaffRemainWorkingMonth == null))
                {
                    mess = "-";
                }
            }
            return mess;
        }

        private string GetStaffSalaryId(string? staffId)
        {
            var StaffSalaryId = "ไม่พบข้อมูลรหัสเงินเดือน";
            VSStaff? _VSStaff = userService.GetVSStaff(staffId);

            if (_VSStaff != null)
            {
                if (!string.IsNullOrEmpty(_VSStaff.StaffSalaryId))
                {
                    StaffSalaryId = _VSStaff.StaffSalaryId;
                }
            }
            return StaffSalaryId;
        }

        private void DataBookBank(LoanStaffDetail Detail)
        {
            MessBookBank = string.Empty;
            if (string.IsNullOrEmpty(Detail.BookBankNo) &&
                string.IsNullOrEmpty(Detail.BookBankName) &&
                string.IsNullOrEmpty(Detail.BookBankBranch))
            {
                MessBookBank = "กรุณาระบุหมายเลขบัญชีธนาคารที่ต้องการรับเงินกู้ยืม";
            }
            else
            {
                MessBookBank = $"บัญชีเลขที่ {Detail.BookBankNo} {Detail.BookBankName} สาขา {Detail.BookBankBranch}";
            }
        }

        private void SetDataBook()
        {
            SelectDebtorDetail = new();
            SelectBank = new();

            if (!string.IsNullOrEmpty(DebtorDetail.StaffId))
            {
                SelectDebtorDetail = DebtorDetail;
                SelectDebtorDetail.MobileTel = DebtorDetail.MobileTel;
                SelectDebtorDetail.OfficeTel = DebtorDetail.OfficeTel;
                if (!string.IsNullOrEmpty(SelectDebtorDetail.BookBankName))
                {
                    int index = Select.FindIndex(a => a.Name.Contains(SelectDebtorDetail.BookBankName));
                    if (index != -1)
                    {
                        SelectBank = Select[index];
                    }
                }
            }
        }

        private async Task SaveToLoanDebtorDetailAsync(LoanStaffDetail SetDetail,
            string Action,
            bool IsButtonClose = false)
        {
            //var Detail = _context.LoanStaffDetails
            //            .Where(c => c.StaffId == StaffID)
            //            .FirstOrDefault();
            LoanStaffDetail? Detail = await _context.LoanStaffDetails
                .Where(c => c.StaffId == StaffID)
                .FirstOrDefaultAsync();

            SetDetail.StaffId = StaffID;

            if (Action == "BookBank")
            {
                #region เลขบัญชี
                if (!string.IsNullOrEmpty(SetDetail.BookBankNo))
                {
                    if (Detail == null)
                    {
                        // Add
                        //_context.LoanStaffDetails.Add(SetDetail);
                        await psuLoan.AddLoanStaffDetail(SetDetail);
                    }
                    else
                    {
                        // Update
                        Detail.BookBankBranch = SetDetail.BookBankBranch;
                        Detail.BookBankName = SetDetail.BookBankName;
                        Detail.BookBankNo = SetDetail.BookBankNo;
                        //_context.Update(Detail);

                        await psuLoan.UpdateLoanStaffDetail(Detail);
                    }
                    await _context.SaveChangesAsync();
                }
                #endregion

                navigationManager.NavigateTo($"/UserResume", true);
            }
            else if (Action == "MobileTel")
            {
                IsEditMobileTel = false;

                #region เบอร์โทร ส่วนตัว
                if (!IsButtonClose) // ตกลง
                {
                    if (Detail == null)
                    {
                        // Add
                        //_context.LoanStaffDetails.Add(SetDetail);
                        await psuLoan.AddLoanStaffDetail(SetDetail);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(SetDetail.MobileTel))
                        {
                            // Update
                            Detail.MobileTel = SetDetail.MobileTel;
                            //_context.Update(Detail);

                            await psuLoan.UpdateLoanStaffDetail(Detail);
                        }
                        else
                        {
                            string alert = $"กรุณาระบุเบอร์โทรศัพท์มือถือ";
                            await JS.InvokeVoidAsync("displayTickerAlert", alert);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                #endregion

                await GetDebtorDetail();
            }
            else if (Action == "OfficeTel")
            {
                IsEditOfficeTel = false;

                #region เบอร์โทร ที่ทำงาน
                if (!IsButtonClose) // ตกลง
                {
                    if (Detail == null)
                    {
                        // Add
                        //_context.LoanStaffDetails.Add(SetDetail);
                        await psuLoan.AddLoanStaffDetail(SetDetail);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(SetDetail.OfficeTel))
                        {
                            // Update
                            Detail.OfficeTel = SetDetail.OfficeTel;
                            //_context.Update(Detail);
                            await psuLoan.UpdateLoanStaffDetail(Detail);
                        }
                        else
                        {
                            string alert = $"กรุณาระบุเบอร์ที่ทำงาน";
                            await JS.InvokeVoidAsync("displayTickerAlert", alert);
                        }

                    }
                    await _context.SaveChangesAsync();
                }
                #endregion

                await GetDebtorDetail();
            }
        }
    }
}
