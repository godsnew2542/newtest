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
    public partial class LoanPartner
    {
        #region Parameter
        [Parameter] public LoanType Loan { get; set; } = new();
        [Parameter] public VLoanStaffDetail StaffDetail { get; set; } = new();
        [Parameter] public VStaffFamily StaffFamilies { get; set; } = new();
        [Parameter] public string Role { get; set; } = string.Empty;
        [Parameter] public string PageId { get; set; } = "pdf-partner";
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

        public async Task<string> GetBoByHtmlAsync()
        {
            var html = await JS.InvokeAsync<string>("getBobyHtml", PageId);
            return html;
        }

        private static string GetNamePartner(VStaffFamily families)
        {
            string FullName = string.Empty;
            if (families != null)
            {
                FullName = $"{families.FamilyPartnerFname}{families.FamilyPartnerMname} {families.FamilyPartnerOldsname}";
            }
            return FullName;
        }
    }
}
