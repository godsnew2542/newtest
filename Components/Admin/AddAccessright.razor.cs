using AntDesign;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;
using LoanApp.Model.Models;
using LoanApp.Pages.AdminCenter;
using LoanApp.Services.IServices.LoanDb;
using LoanApp.Services.Services.LoanDb;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LoanApp.Components.Admin
{
    public partial class AddAccessright
    {
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }
        [CascadingParameter] private Error Error { get; set; } = null!;

        #region Parameter
        [Parameter] public EventCallback<bool> OnCallback { get; set; }

        /// <summary>
        /// บันทีกสำเร็จ ส้งว่าต้อง logout หรือไหม  true = logout
        /// </summary>
        [Parameter] public EventCallback<bool> OnCallbackSuccess { get; set; }
        [Parameter] public EventCallback<bool> OnCallbackReloadUI { get; set; }
        [Parameter] public SetRoleModel SetRole { get; set; } = new();
        [Parameter] public List<LoanGroup> ListGroup { get; set; } = new();
        [Parameter] public VLoanStaffDetail? StaffDetail { get; set; } = new();
        [Parameter] public List<CCampus> ListCampus { get; set; } = new();

        #endregion

        #region Inject
        [Inject] private ModalService ModalService { get; set; } = null!;
        [Inject] private Services.IServices.INotificationService notificationService { get; set; } = null!;
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        #endregion

        private bool IsAddPerson { get; set; } = true;
        private bool IsAddRoleMessageFail { get; set; } = true;

        private void OnSelectedItemChangedCcampus(CCampus? cCampus)
        {
            if (cCampus != null)
            {
                SetRole.CampId = cCampus.CampId;
            }
            else
            {
                SetRole.CampId = "00";
            }
            CheckAddPerson(SetRole);
        }

        private void CheckAddPerson(SetRoleModel setRoleModel)
        {

            if (setRoleModel.GroupId == 0)
            {
                IsAddPerson = true;
            }
            else if (setRoleModel.GroupId == 1)
            {
                IsAddPerson = false;
            }
            else if (setRoleModel.GroupId != 0 && setRoleModel.GroupId != 1)
            {
                if (setRoleModel.CampId == "00")
                {
                    IsAddPerson = true;
                }
                else
                {
                    IsAddPerson = false;
                }
            }
            else
            {
                IsAddPerson = true;
            }
        }

        private void OnSelectedItemChangedLoanGroup(LoanGroup? loanGroup)
        {
            if (loanGroup != null)
            {
                SetRole.GroupId = loanGroup.GroupId;
                if (SetRole.GroupId == 1 || SetRole.GroupId == 0)
                {
                    SetRole.CampId = "00";
                }
            }
            else
            {
                SetRole.GroupId = 0;
                SetRole.CampId = "00";
            }
            CheckAddPerson(SetRole);
        }

        private async Task HandleCancelAddPerson(MouseEventArgs e)
        {
            await OnCallback.InvokeAsync(false);
        }

        private async Task HandleOkAddPerson()
        {
            bool isFail = await CheckRoleMessage(StaffDetail?.StaffId, SetRole);

            if (!isFail)
            {

                bool isLogout = StateProvider?.CurrentUser.StaffId == StaffDetail?.StaffId ? true : false;

                #region check ว่าจะเพิ่มตัวเอก หรือไหม หากใช่จะต้อง login ใหม่เพื่อจะได้สิทธิ์ปัจจุบัน
                if (isLogout)
                {
                    bool isCancel = await ModalService.ConfirmAsync(new ConfirmOptions()
                    {
                        Title = "ยืนยันการเพิ่มสิทธิ์ของท่านใช่หรือไหม",
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

                try
                {
                    await ConfirmedAsync(StaffDetail?.StaffId, SetRole);

                    await OnCallbackSuccess.InvokeAsync(isLogout);

                    //await OnCallback.InvokeAsync(false);
                }
                catch (Exception ex)
                {
                    await notificationService.Error(notificationService.ExceptionLog(ex));
                }
            }
            else
            {
                await notificationService.WarningDefult(mess: "ไม่สามารถเพิ่มสิทธิ์นี้ได้ เนื่องจาก พบสิทธิการใช้งานนี้แล้ว");
            }
        }

        /// <summary>
        /// true ไม่ผ่าน
        /// </summary>
        /// <param name="staffId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private async Task<bool> CheckRoleMessage(string? staffId, SetRoleModel data)
        {
            IsAddRoleMessageFail = true;
            var CampId = data.CampId;

            if (data.GroupId == 1)
            {
                CampId = "00";
            }

            if (!string.IsNullOrEmpty(staffId))
            {
                LoanStaffPrivilege? FindStaffPrivilege = await psuLoan.CheckPeopleLoanStaffPrivilege(staffId, CampId, data.GroupId);

                if (FindStaffPrivilege != null)
                {
                    if (FindStaffPrivilege.Active == 0)
                    {
                        IsAddRoleMessageFail = false;
                    }
                }
                else
                {
                    IsAddRoleMessageFail = false;
                }
            }
            return IsAddRoleMessageFail;
        }

        private async Task ConfirmedAsync(string? staffId, SetRoleModel data)
        {
            decimal Active = 1;
            var CampId = data.CampId;

            if (data.GroupId == 1)
            {
                CampId = "00";
            }

            if (!string.IsNullOrEmpty(staffId))
            {
                try
                {
                    LoanStaffPrivilege? FindStaffPrivilege = await psuLoan.CheckPeopleLoanStaffPrivilege(staffId, CampId, data.GroupId);

                    if (FindStaffPrivilege != null)
                    {
                        FindStaffPrivilege.Active = Active;
                        await psuLoan.UpdateLoanStaffPrivilege(FindStaffPrivilege);
                    }
                    else
                    {
                        LoanStaffPrivilege loanPrivilege = new()
                        {
                            StaffId = staffId,
                            CampId = CampId,
                            GroupId = data.GroupId,
                            Active = Active,
                        };
                        await psuLoan.AddLoanStaffDetail(loanPrivilege);
                    }

                    await OnCallbackReloadUI.InvokeAsync(true);
                    StaffDetail = new();
                }
                catch (Exception ex)
                {
                    await Error.ProcessError(ex);
                }
            }
        }
    }
}
