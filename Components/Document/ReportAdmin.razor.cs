using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Components.Document
{
    public partial class ReportAdmin
    {
        #region Parameter
        [Parameter] public List<ReportAdminModel> ListReportAdmin { get; set; } = new();
        [Parameter] public List<ReportAdminModel> ListReportAdminV2 { get; set; } = new();
        [Parameter] public string CampusID { get; set; } = string.Empty;
        [Parameter] public string Title { get; set; } = string.Empty;

        #endregion

        public string CampusName { get; set; } = string.Empty;

        protected override void OnInitialized()
        {
            if (!string.IsNullOrEmpty(CampusID))
            {
                var Campus = EntitiesCentralService.GetCampus(CampusID);

                if (Campus != null)
                {
                    CampusName = Campus.CampNameThai;
                }
            }
        }

        private static int GetYear(decimal? AD, int EB)
        {
            if (AD == null)
            {
                return 0;
            }

            int Year = Convert.ToInt32(AD) + EB;
            return Year;
        }

        public async Task<string> GetBoByHtmlAsync()
        {
            var html = await JS.InvokeAsync<string>("getBobyHtml", "pdf-ReportAdmin");
            return html;
        }
    }
}
