using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Components.AdminOption
{
    public partial class ReportLoanTypeDataByStaff
    {
        [Parameter] public List<VReportTransaction> ReportTransactions { get; set; } = new();

        private List<TypeDataByStaffModel> TypeDataByStaff { get; set; } = new();

        protected override void OnInitialized()
        {
            TypeDataByStaff = new();

            if (ReportTransactions.Any())
            {
                List<TypeDataByStaffModel> data = ReportTransactions
                    .Where(x => x.LoanTypeId != null)
                    .Select(c => new TypeDataByStaffModel()
                    {
                        LoanTypeId = c.LoanTypeId,
                        LoanTypeName = c.LoanTypeName,
                    })
                    .DistinctBy(x => x.LoanTypeId)
                    .ToList();

                if (data.Any())
                {
                    foreach (var model in data)
                    {
                        List<VReportTransaction> resourtData = ReportTransactions
                            .Where(x => x.LoanTypeId == model.LoanTypeId)
                            .ToList();

                        model.Count = resourtData.Count();
                        model.SumPrincipleAmount = resourtData.Sum(x => x.PrincipleAmount);
                        model.SumInterestAmont = resourtData.Sum(x => x.InterestAmont);
                        model.SumTotalAmount = resourtData.Sum(x => x.TotalAmount);
                    }

                    TypeDataByStaff = data;
                }
            }
        }

        public List<TypeDataByStaffModel> ResultDataUI()
        {
            return TypeDataByStaff;
        }
    }
}
