using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LoanApp.Components.Document
{
    public partial class LoanAttrachment
    {
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #region Parameter
        [Parameter] public VLoanStaffDetail StaffDetail { get; set; } = new VLoanStaffDetail();
        [Parameter] public VStaffAddress StaffAssress { get; set; } = new VStaffAddress();
        [Parameter] public LoanType Loan { get; set; } = new LoanType();
        [Parameter] public ApplyLoanModel Other { get; set; } = new ApplyLoanModel();
        [Parameter] public DocumentOptionModel Option { get; set; } = new();
        [Parameter] public string TitleDocument { get; set; } = "สัญญา";

        #endregion

        #region Inject
        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan PsuLoan { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;

        #endregion

        private string ShowMessInterest { get; set; } = "พร้อมดอกเบี้ยในอัตราร้อยละ";
        private bool IsLending { get; set; } = false; // fales = เงินกู้
        private string? CapmSelectNow { get; set; } = null;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    CapmSelectNow = StateProvider?.CurrentUser.CapmSelectNow;

                    if (string.IsNullOrEmpty(CapmSelectNow))
                    {
                        CapmSelectNow = (await PsuLoan.GetUserDetailAsync(StateProvider?.CurrentUser.StaffId))?.CampId;
                    }
                    else if (CapmSelectNow == "00")
                    {
                        CapmSelectNow = null;
                    }

                    IsLending = false;
                    if (Loan.LoanInterest != null && Loan.LoanInterest == 0)
                    {
                        IsLending = true; // เงินยืม
                    }

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
                }
            }
        }

        private static string GetStaffType(string? staffType)
        {
            string mess = string.Empty;
            StaffTypeModel SType = new();

            if (string.IsNullOrEmpty(staffType))
            {
                return mess;
            }


            if (SType.GovernmentOfficer.Contains(staffType))
            {
                mess = "[/] ข้าราชการ";
            }
            else
            {
                mess = "[] ข้าราชการ";
            }

            if (SType.Employee.Contains(staffType))
            {
                mess = $"{mess} [/] ลูกจ้างประจำ";
            }
            else
            {
                mess = $"{mess} [] ลูกจ้างประจำ";
            }

            if (SType.UniversityStaff.Contains(staffType))
            {
                mess = $"{mess} [/] พนักงานมหาวิทยาลัย";
            }
            else
            {
                mess = $"{mess} [] พนักงานมหาวิทยาลัย";
            }

            if (SType.IncomeEmployee.Contains(staffType))
            {
                mess = $"{mess} [/] พนักงานเงินรายได้";
            }
            else
            {
                mess = $"{mess} [] พนักงานเงินรายได้";
            }

            return mess;
        }

        private string GetNumberToText(string text)
        {
            return userService.ArabicNumberToText(text);
        }

        private string GenerateText(decimal? text)
        {
            string value = string.Empty;

            if (text == null || text == 0)
            {
                return value;
            }

            return userService.ArabicNumberToText($"{text}");
        }

        private string GetNumInstallmentsTH(string text)
        {
            var InstallmentsTH = GetNumberToText(text);
            int index = (InstallmentsTH.Length) - 7;
            return InstallmentsTH.Substring(0, index);
        }

        private static string MarriedOther(string? id, string? name)
        {
            string mess = string.Empty;
            List<string> listId = new() { "1", "2", "3" };

            if (string.IsNullOrEmpty(id))
            {
                return mess;
            }
            //id != "1" && id != "2" && id != "3"
            if (!listId.Contains(id))
            {
                mess = $"{name}";
            }
            return mess;
        }

        private static string MarriedType(string? id)
        {
            string mess = string.Empty;

            if (string.IsNullOrEmpty(id))
            {
                return mess;
            }


            if (id == "1")
            {
                mess = $"[/] โสด";
            }
            else
            {
                mess = $"[] โสด";
            }

            if (id == "2")
            {
                mess = $"{mess} [/] สมรส";
            }
            else
            {
                mess = $"{mess} [] สมรส";
            }

            if (id == "3")
            {
                mess = $"{mess} [/] หม้าย";
            }
            else
            {
                mess = $"{mess} [] หม้าย";
            }

            mess = $"{mess} อื่นๆ ";
            return mess;
        }

        public async Task<string> GetBoByHtmlAsync()
        {
            var html = await JS.InvokeAsync<string>("getBobyHtml", "pdf-LoanAttrachmentByIT");
            return html;
        }
    }
}
