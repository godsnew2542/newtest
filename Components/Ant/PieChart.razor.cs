using AntDesign.Charts;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Components.Ant
{
    public partial class PieChart
    {
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        [Parameter] public decimal[] Status { get; set; } = Array.Empty<decimal>();

        object[]? data1 { get; set; } = null;

        protected override async Task OnInitializedAsync()
        {
            string StaffID = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);

            if (!string.IsNullOrEmpty(StaffID))
            {
                List<VLoanRequestContract> ListTypeAgreement = await _context.VLoanRequestContracts
                .Where(c => Status.Contains(c.CurrentStatusId!.Value))
                .Where(c => c.DebtorStaffId == StaffID)
                .ToListAsync();

                if (ListTypeAgreement.Count != 0)
                {
                    List<PieChartModel> listAgeementDetail = new();

                    for (int i = 0; i < ListTypeAgreement.Count; i++)
                    {
                        var AgreementDetail = ListTypeAgreement[i];
                        PieChartModel PieC = new()
                        {
                            Type = AgreementDetail.LoanTypeName,
                            Value = AgreementDetail.LoanRequestLoanAmount
                        };

                        listAgeementDetail.Add(PieC);

                    }
                    data1 = listAgeementDetail.ToArray();
                }
            }
        }

        readonly PieConfig config1 = new()
        {
            ForceFit = true,
            Responsive = true,
            Height = 200,
            Radius = 0.8,
            AngleField = "value",
            ColorField = "type",
            Label = new PieLabelConfig
            {
                Visible = true,
                /* Type = "inner"*/
                Type = "spider",
                AutoRotate = true
            },
            Legend = new Legend { Position = "Top", Visible = false },
        };
    }
    public class PieChartModel
    {
        public string? Type { get; set; }
        public decimal? Value { get; set; }
    }
}
