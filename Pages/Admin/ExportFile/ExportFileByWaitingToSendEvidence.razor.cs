using ClosedXML.Excel;
using LoanApp.DatabaseModel.LoanEntities;

using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Pages.Admin.ExportFile
{
    public partial class ExportFileByWaitingToSendEvidence
    {
        #region CascadingParameter
        [CascadingParameter] private UserStateProvider? StateProvider { get; set; }

        #endregion

        #region Inject
        [Inject] private Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private INotificationService notificationService { get; set; } = null!;
        #endregion

        private List<ContractStatus> ContractStatuses { get; set; } = new();

        /// <summary>
        /// IOrderedQueryable
        /// </summary>
        private IOrderedQueryable<VLoanRequestContract> Query { get; set; } = null!;
        private List<VLoanRequestContract> RequestContracts { get; set; } = new();
        private List<SetNewRecordFirstPaymentModel> ListRecord { get; set; } = new();

        private List<decimal> Status { get; set; } = new() { 6, 200 };
        private decimal SelectedValue { get; set; } = 6;
        private bool IsloadingSelectStatuse { get; set; } = true;
        private string? AdminCampId { get; set; } = null;
        private bool LoadingUi { get; set; } = true;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    ContractStatuses = await psuLoan.GetAllContractStatus(Status);
                    ContractStatuses.Insert(0, new ContractStatus()
                    {
                        ContractStatusId = 0,
                        ContractStatusName = "ทุกสถานะ"
                    });

                    IsloadingSelectStatuse = false;

                    AdminCampId = StateProvider?.CurrentUser.CapmSelectNow;

                    if (string.IsNullOrEmpty(AdminCampId))
                    {
                        LoadingUi = false;
                        return;
                    }

                    Query = _context.VLoanRequestContracts
                        .Where(c => Status.Contains(c.CurrentStatusId!.Value))
                        .Where(c => c.DebtorCampusId == AdminCampId)
                        .OrderBy(c => c.CurrentStatusId)
                        .ThenBy(c => c.ContractId);

                    await GetQueryData(SelectedValue);
                    LoadingUi = false;
                }
                catch (Exception ex)
                {
                    LoadingUi = false;

                    _ = Task.Run(() => { notificationService.ErrorDefult(notificationService.ExceptionLog(ex)); });
                }

                StateHasChanged();
            }
        }

        private async Task GetQueryData(decimal contractStatusId)
        {
            try
            {
                RequestContracts = await Query
                    .Where(c => contractStatusId == 0 || c.CurrentStatusId == contractStatusId)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }
        }

        private async void OnSelectedItemChangedHandler(ContractStatus value)
        {
            await GetQueryData(value.ContractStatusId);
        }

        private async Task<RenderFragment?> GetFullNameOrgan(VLoanRequestContract loanRequestContract)
        {
            RenderFragment? render = null;
            try
            {
                VLoanStaffDetail? staffDetail = await psuLoan.GetUserDetailAsync(loanRequestContract.DebtorStaffId);

                if (staffDetail != null)
                {
                    render = FullNameOrganRender(staffDetail);
                }
            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }

            return render;
        }

        private async Task<RenderFragment?> GetPhone(VLoanRequestContract loanRequestContract)
        {
            RenderFragment? render = null;
            try
            {
                LoanStaffDetail? staffDetail = await psuLoan.GetLoanStaffDetailByStaffId(loanRequestContract.DebtorStaffId);

                if (staffDetail != null)
                {
                    render = PhoneRender(staffDetail);
                }
            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }

            return render;
        }

        private bool GetCheckContractId(VLoanRequestContract requestContract)
        {
            return ListRecord.Any(c => c.ContractId == requestContract.ContractId);
        }

        private void CheckboxClickedV2(decimal? ContractId, bool value, VLoanRequestContract requestContract)
        {
            if (ListRecord.Any())
            {
                var myTodo = ListRecord.Find(x => x.ContractId == ContractId);

                if (myTodo != null)
                {
                    ListRecord.Remove(myTodo);
                }
                else
                {
                    ListRecord.Add(Utility.AddListRecord(ContractId, value, requestContract));
                }
            }
            else
            {
                ListRecord.Add(Utility.AddListRecord(ContractId, value, requestContract));
            }
        }

        private void SetOrClearCheckedAsync(bool data)
        {
            ListRecord = new();
            if (data && RequestContracts.Any())
            {
                foreach (var item in RequestContracts)
                {
                    CheckboxClickedV2(item.ContractId, data, item);
                }
            }
        }

        private async Task ExportToExcel2Async(List<SetNewRecordFirstPaymentModel> data)
        {
            LoadingUi = true;
            await Task.Delay(1);
            StateHasChanged();

            var FileName = $"รอผู้กู้ส่งหลักฐาน_{dateService.ChangeDate(DateTime.Now, "dd-MM-yyyy", Utility.DateLanguage_TH)}";

            try
            {
                if (data.Any())
                {
                    int row = 1;
                    var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Sheet1");

                    #region Table Header
                    ws.Cell(row, 1).Value = "ลำดับที่";

                    ws.Cell(row, 2).Value = "ชื่อ";

                    ws.Cell(row, 3).Value = "สกุล";

                    ws.Cell(row, 4).Value = "หน่วยงาน";

                    ws.Cell(row, 5).Value = "ส่วนงาน";

                    ws.Cell(row, 6).Value = "วิทยาเขต";

                    ws.Cell(row, 7).Value = "เบอร์โทรที่ทำงาน";

                    ws.Cell(row, 8).Value = "เบอร์โทรศัพท์มือถือ";

                    ws.Cell(row, 9).Value = "เลขที่สัญญา";

                    ws.Cell(row, 10).Value = "ประเภทกู้ยืม";

                    ws.Cell(row, 11).Value = "ยอดกู้ (บาท)";

                    ws.Cell(row, 12).Value = "ยอดกู้รวมดอกเบี้ย (บาท)";

                    ws.Cell(row, 13).Value = "หนี้คงเหลือ (บาท)";

                    ws.Cell(row, 14).Value = "งวดที่กู้ (งวด)";

                    ws.Cell(row, 15).Value = "ชำระแล้ว (งวด)";

                    ws.Cell(row, 16).Value = "คงเหลือ (งวด)";

                    ws.Cell(row, 17).Value = "สถานะ";
                    #endregion

                    #region Table Boby
                    for (int i = 0; i < data.Count; i++)
                    {
                        var info = data[i];
                        row++;

                        VLoanRequestContract? reqCon = info.LoanRequestContract;
                        VLoanStaffDetail? vLoanStaffDetail = await psuLoan.GetUserDetailAsync(reqCon?.DebtorStaffId);

                        if (reqCon != null)
                        {
                            LoanStaffDetail? staffDetail = await psuLoan.GetLoanStaffDetailByStaffId(reqCon.DebtorStaffId);

                            decimal? amount = (reqCon.ContractLoanAmount != null ? reqCon.ContractLoanAmount : reqCon.LoanRequestLoanAmount);
                            decimal totalAmount = TransactionService.FindLoanTotalAmount(reqCon.ContractId);
                            decimal balanceAmount = await TransactionService.GetBalanceTotalAsync(reqCon);

                            decimal? numInstallments = await TransactionService.GetBalanceInstallmentNo(reqCon);

                            ws.Cell(row, 1).Value = i + 1;
                            ws.Cell(row, 2).Value = reqCon.DebtorNameTh;
                            ws.Cell(row, 3).Value = reqCon.DebtorSnameTh;
                            ws.Cell(row, 4).Value = vLoanStaffDetail?.DeptNameThai;
                            ws.Cell(row, 5).Value = vLoanStaffDetail?.FacNameThai;
                            ws.Cell(row, 6).Value = vLoanStaffDetail?.CampNameThai;
                            ws.Cell(row, 7).Value = staffDetail?.OfficeTel;
                            ws.Cell(row, 8).Value = staffDetail?.MobileTel;
                            ws.Cell(row, 9).Value = $"{reqCon.ContractNo}";
                            ws.Cell(row, 10).Value = reqCon.LoanTypeName;
                            ws.Cell(row, 11).Value = amount == null ? "ไม่พบข้อมูล" : string.Format("{0:n2}", amount);
                            ws.Cell(row, 12).Value = totalAmount == 0 ? "ไม่พบข้อมูล" : string.Format("{0:n2}", totalAmount);
                            ws.Cell(row, 13).Value = balanceAmount == 0 ? "ไม่พบข้อมูล" : string.Format("{0:n2}", balanceAmount);
                            ws.Cell(row, 14).Value = reqCon.ContractLoanNumInstallments;
                            ws.Cell(row, 15).Value = numInstallments;
                            ws.Cell(row, 16).Value = reqCon.ContractLoanNumInstallments - (numInstallments == null ? 0 : numInstallments);
                            ws.Cell(row, 17).Value = reqCon.CurrentStatusName;
                        }
                    }
                    #endregion

                    MemoryStream XLSStream = new();
                    wb.SaveAs(XLSStream);

                    byte[] bytes = XLSStream.ToArray();
                    XLSStream.Close();

                    await SaveFileAndImgService.SaveFileAsPath(bytes, FileName, "data:application/vnd.ms-excel;base64,");
                }
            }
            catch (Exception ex)
            {
                _ = Task.Run(() => { notificationService.ErrorDefult(notificationService.ExceptionLog(ex)); });
            }

            LoadingUi = false;
            await Task.Delay(1);
            StateHasChanged();
        }

        private string? GetTextSelectedValue(decimal val)
        {
            switch (val)
            {
                case 0: return "สัญญากู้ยืมเงิน (รอผู้กู้ส่งหลักฐาน และหลักฐานไม่ผ่าน) ";

                case 6: return "สัญญากู้ยืมเงิน (รอผู้กู้ส่งหลักฐาน) ";

                case 200: return "สัญญากู้ยืมเงิน (หลักฐานไม่ผ่าน) ";

                default: return null;
            }
        }
    }
}
