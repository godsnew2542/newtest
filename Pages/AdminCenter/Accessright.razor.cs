using AntDesign;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LoanApp.Pages.AdminCenter
{
    public partial class Accessright
    {
        #region CascadingParameter
        [CascadingParameter] private Error Error { get; set; } = null!;
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #endregion

        #region Inject
        [Inject] private ModalService ModalService { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuCentral psuCentral { get; set; } = null!;
        [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;

        #endregion

        private List<VLoanStaffDetail> ListStaffDetail { get; set; } = new();
        private VLoanStaffDetail? StaffDetail { get; set; } = new();
        private ApplyLoanModel Search { get; set; } = new();
        private MonthModel model_month { get; set; } = new();
        private List<LoanGroup> ListGroup { get; set; } = new();
        private List<CCampus> ListCampus { get; set; } = new();
        private SetRoleModel SetRole { get; set; } = new();
        private List<LoanStaffPrivilege> ListStaffPrivilege { get; set; } = new();
        private LoanStaffPrivilege StaffPrivilege { get; set; } = new();

        private bool IsAddRoleMessageFail { get; set; } = true;
        private string? searchValue { get; set; } = null;
        private bool loading { get; set; } = false;
        /// <summary>
        /// true [save ได้] false [save ไม่ได้]
        /// </summary>
        private bool IsAddPerson { get; set; } = true;
        private bool visibleDelete { get; set; } = false;
        private bool visibleAddPerson { get; set; } = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    ListGroup = await psuLoan.GetAllLoanGroup();
                    //List<CCampus> campus = await _contextCentral.CCampuses
                    //    .Where(c => c.CampId != "00")
                    //    .AsNoTracking()
                    //    .ToListAsync();

                    //foreach (var item in campus)
                    //{
                    //    CCampus cCampus = new() { CampId = item.CampId, CampNameThai = item.CampNameThai.Replace("วิทยาเขต", ""), };

                    //    ListCampus.Add(cCampus);
                    //}

                    ListCampus = (await psuCentral.GetAllCampuses())
                        .Select(x => new CCampus
                        {
                            CampId = x.CampId,
                            CampNameThai = x.CampNameThai.Replace("วิทยาเขต", "")
                        })
                        .ToList();

                    ListGroup.Insert(0, new() { GroupId = 0, GroupName = "เลือกบทบาท" });
                    ListCampus.Insert(0, new() { CampId = "00", CampNameThai = "เลือกวิทยาเขต" });

                    ListStaffPrivilege = await psuLoan.GetAllLoanStaffPrivilege();

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }
            }
        }

        private void ChangeValStaff(VLoanStaffDetail people)
        {
            ListStaffDetail = new();
            StaffDetail = new();

            var staffId = people.StaffId;
            string fullName = string.Empty;

            StaffDetail = people;
            //StaffDetail = await psuLoan.GetUserDetailAsync(staffId)

            if (StaffDetail != null)
            {
                //fullName = $"{StaffDetail?.TitleNameThai}{StaffDetail?.StaffNameThai} {StaffDetail?.StaffSnameThai}"
                fullName = userService.GetFullName(StaffDetail);
            }

            Search.DebtorId = staffId;
            Search.Debtor = fullName;
            searchValue = fullName;
        }

        private string ChangeDate(string? StringDate, string[] selectMonth)
        {
            var ShowDate = string.Empty;
            DateModel Date = Utility.ChangeDateMonth(StringDate, selectMonth);

            if (!string.IsNullOrEmpty(Date.Day))
            {
                ShowDate = $"{Date.Day} {Date.Month} {Date.Year}";
            }
            return ShowDate;
        }

        private string? GetCampusName(string? campusId)
        {
            string? CampusName = "-";
            if (campusId != "00")
            {
                CampusName = ListCampus.Find(x => x.CampId == campusId)?.CampNameThai;
            }
            return CampusName;
        }

        private async Task RemoveRoleAsync(LoanStaffPrivilege data)
        {
            decimal NotActive = 0;

            try
            {
                LoanStaffPrivilege? FindStaffPrivilege = await psuLoan.GetLoanStaffPrivilegeByStaffPrivilegeId(data.StaffPrivilegeId);

                if (FindStaffPrivilege != null)
                {
                    FindStaffPrivilege.Active = NotActive;

                    await psuLoan.UpdateLoanStaffPrivilege(FindStaffPrivilege);
                    ListStaffPrivilege = await psuLoan.GetAllLoanStaffPrivilege();
                }

                searchValue = null;
                ListStaffDetail = new();
                StaffDetail = new();
                await notificationService.SuccessDefult("อัปเดตข้อมูลสำเร็จ");
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task Submit(string? changedString)
        {
            loading = true;
            await Task.Delay(1);

            bool isFilter = true;
            StaffDetail = null;

            if (changedString == null)
            {
                isFilter = false;
            }
            else if ((changedString.Trim()).Length < Utility.SearchMinlength)
            {
                _ = Task.Run(() => { notificationService.WarningDefult($"กรุณาใส่ข้อมูล อย่างน้อย {Utility.SearchMinlength} ตัวขึ้นไป"); });
                isFilter = false;
            }

            if (!isFilter)
            {
                loading = false;
                StateHasChanged();
                return;
            }

            try
            {
                ListStaffDetail = await psuLoan.GetListVLoanStaffDetailByPageAccessright(changedString!.Trim(), new List<string>() { "3" });

                loading = false;
                StateHasChanged();

                if (!ListStaffDetail.Any())
                {
                    _ = Task.Run(() => { notificationService.WarningDefult("ไม่พบข้อมูล"); });
                }
                else if (ListStaffDetail.Count == 1)
                {
                    ChangeValStaff(ListStaffDetail[0]);
                }
            }
            catch (Exception ex)
            {
                loading = false;
                StateHasChanged();
                _ = Task.Run(() => { _ = Error.ProcessError(ex); });
            }
        }

        private async Task DataOnCallbackSuccess(bool isLogout)
        {
            if (isLogout)
            {
                #region log out
                await userService.RemoveUserRoleAsync();
                //navigationManager.NavigateTo(uri: "/SignOut", forceLoad: true);
                SignOutUser(true);

                #endregion
            }

            visibleAddPerson = false;
            _ = Task.Run(() => { notificationService.SuccessDefult("เพิ่มข้อมูลสำเร็จ"); });

            searchValue = null;
            StaffDetail = new();
            ListStaffDetail = new();
            StateHasChanged();
        }

        private void SetDataAddPerson(VLoanStaffDetail? staffDetail)
        {
            if (staffDetail == null)
            {
                return;
            }

            SetRole = new()
            {
                //CampId = ListCampus.Find(x => x.CampId == "00")!.CampId,
                //GroupId = ListGroup.Find(x => x.GroupId == 0)!.GroupId,
                CampId = "00",
                GroupId = 0,
                staffDetail = staffDetail
            };

            visibleAddPerson = true;
        }

        private async Task HandleOkDelete(MouseEventArgs e)
        {
            #region check ว่าจะลบตัวเอกออกจากบทบาท หรือไหม หากใช่จะต้อง login ใหม่เพื่อจะได้สิทธิ์ปัจจุบัน
            if (StateProvider?.CurrentUser.StaffId == StaffPrivilege.StaffId)
            {
                bool isCancel = await ModalService.ConfirmAsync(new ConfirmOptions()
                {
                    Title = "ยืนยันการลบสิทธิ์ของท่านใช่หรือไหม",
                    Content = $"** หากยืนยันจำเป็นต้อง เข้าสู่ระบบใหม่อีกครั้ง",
                    CancelButtonProps =
                    {
                        ChildContent = CancelButtonRender,
                        Type = ButtonType.Link,
                    },
                    OkButtonProps =
                    {
                        ChildContent = OkButtonRender,
                        Type = ButtonType.Link,
                    }
                });

                if (isCancel)
                {
                    return;
                }
            }

            #endregion

            visibleDelete = false;
            await RemoveRoleAsync(StaffPrivilege);

            #region log out
            if (StateProvider?.CurrentUser.StaffId == StaffPrivilege.StaffId)
            {
                await userService.RemoveUserRoleAsync();
                //navigationManager.NavigateTo(uri: "/SignOut", forceLoad: true);
                SignOutUser(true);
            }
            #endregion
        }

        private void SignOutUser(bool forceLoad)
        {
            navigationManager.NavigateTo(uri: "/SignOut", forceLoad: forceLoad);

        }

        private void HandleCancelDelete(MouseEventArgs e)
        {
            visibleDelete = false;
        }

        private async Task ReloadUI(bool isSuccess)
        {
            if (isSuccess)
            {
                try
                {
                    searchValue = null;
                    ListStaffPrivilege = await psuLoan.GetAllLoanStaffPrivilege();
                }
                catch (Exception ex)
                {
                    _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
                }
            }
        }

        private void ChangeText(string? text)
        {
            searchValue = text;
            if (text == null)
            {
                StaffDetail = null;
            }
        }

    }

    public class SetRoleModel
    {
        public decimal GroupId { get; set; } = 0;
        public string CampId { get; set; } = "00";
        public VLoanStaffDetail? staffDetail { get; set; } = null;
    }
}
