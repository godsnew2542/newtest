using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Components.AdminOption
{
    public partial class ReportTypeDataByStaff
    {
        [Parameter] public List<VReportTransaction> ReportTransactions { get; set; } = new();

        private List<TypeDataByStaffModel> TypeDataByStaff { get; set; } = new();

        protected override void OnInitialized()
        {
            TypeDataByStaff = new();

            if (ReportTransactions.Any())
            {
                List<TypeDataByStaffModel> data = ReportTransactions
                    .Where(x => !string.IsNullOrEmpty(x.StaffType))
                    .Select(c => new TypeDataByStaffModel()
                    {
                        StaffType = c.StaffType,
                        StaffTypeName = c.StaffTypeName,
                    })
                    .DistinctBy(x => x.StaffType)
                    .ToList();

                if (data.Any())
                {
                    foreach (var model in data)
                    {
                        List<VReportTransaction> resourtData = ReportTransactions
                            .Where(x => x.StaffType == model.StaffType)
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

    public class TypeDataByStaffModel
    {
        public byte? LoanTypeId { get; set; }
        public string? LoanTypeName { get; set; }
        public string? StaffType { get; set; }
        public string? StaffTypeName { get; set; }
        /// <summary>
        /// รายการ
        /// </summary>
        public decimal Count { get; set; } = 0;
        /// <summary>
        /// เงินต้น
        /// </summary>
        public decimal? SumPrincipleAmount { get; set; } = 0;
        /// <summary>
        /// ดอกเบี้ย
        /// </summary>
        public decimal? SumInterestAmont { get; set; } = 0;
        /// <summary>
        /// รวมจำนวนเงิน
        /// </summary>
        public decimal? SumTotalAmount { get; set; } = 0;
    }


}
