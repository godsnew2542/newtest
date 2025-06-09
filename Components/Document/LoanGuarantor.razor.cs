using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Components.Document
{
    public partial class LoanGuarantor
    {
        #region Parameter
        [Parameter] public VLoanStaffDetail DebtorStaffDetail { get; set; } = new();
        [Parameter] public VLoanStaffDetail? GuarantorStaffDetail { get; set; } = new();
        [Parameter] public VStaffAddress StaffAssress { get; set; } = new();
        [Parameter] public LoanType Loan { get; set; } = new();
        [Parameter] public DocumentOptionModel Option { get; set; } = new();

        #endregion

        private bool IsLending { get; set; } = false; // fales = เงินกู้

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                IsLending = false;
                if (Loan.LoanInterest != null && Loan.LoanInterest == 0)
                {
                    IsLending = true; // เงินยืม
                    StateHasChanged();
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

            return mess;
        }

        public async Task<string> GetBoByHtmlAsync()
        {
            var html = await JS.InvokeAsync<string>("getBobyHtml", "pdf-LoanGuarantor");
            return html;
        }
    }
}
