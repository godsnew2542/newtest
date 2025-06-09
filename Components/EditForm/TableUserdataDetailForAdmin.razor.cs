using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Components.EditForm
{
    public partial class TableUserdataDetailForAdmin
    {
        [Parameter]
        public string StaffID { get; set; } = string.Empty;

        [Parameter]
        public string TableHeadRole { get; set; } = string.Empty;

        [Parameter]
        public List<VLoanRequestContract> Agreement { get; set; } = new();


        private string GetGuarantorName(VLoanRequestContract ReqCon)
        {
            var staffID = string.Empty;
            if (ReqCon.ContractGuarantorStaffId != null)
            {
                staffID = ReqCon.ContractGuarantorStaffId;
            }
            else
            {
                staffID = ReqCon.LoanRequestGuaranStaffId;
            }
            return userService.GetFullName(staffID);
        }
        
        private string GetDebtorName(VLoanRequestContract ReqCon)
        {
            var staffID = ReqCon.DebtorStaffId;
            return userService.GetFullName(staffID);
        }
    }
}
