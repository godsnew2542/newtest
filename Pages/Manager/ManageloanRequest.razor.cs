using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Helper;
using LoanApp.Model.Models;
using LoanApp.Services.IServices.LoanDb;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LoanApp.Pages.Manager
{
    public partial class ManageloanRequest
    {
        [CascadingParameter] private UserStateProvider? userStateProvider { get; set; }

        [Inject] private IPsuLoan psuLoan { get; set; } = null!;

        private List<VLoanRequestContract> ReqCon { get; set; } = new();
        private SearchModel Search { get; set; } = new();
        private PanelFooterModel Footer { get; set; } = new();
        private List<LoanType> LoanTypeList { get; set; } = new();
        private VLoanRequestContract index { get; set; } = new();

        private decimal? StatusId { get; set; } = 5;
        public string SearchView { get; set; } = string.Empty;
        private decimal[] AllStatusId { get; } = new[] { 3m, 9m };
        private List<decimal> SelestReqId { get; set; } = new();

        protected override void OnInitialized()
        {
            Search.Title = string.Empty;
            index.LoanTypeId = 0;

            if (index.CurrentStatusId != 0)
            {
                index.CurrentStatusId = StatusId;
                SearchData(SearchView, index);
            }
            else
            {
                StartTable();
            }

            LoanTypeList = _context.LoanTypes
                .Where(c => c.Active == 1)
                .ToList();
            LoanTypeList.Insert(0, new LoanType() { LoanTypeId = 0, LoanTypeName = "ทุกประเภท" });
        }

        public void ShowData(VLoanRequestContract item)
        {
            navigationManager.NavigateTo($"/Manager/RequestDetailManager/{item.LoanRequestId}");
        }

        public void StartTable()
        {
            var total = CountVLoanRequestContracts();
            SetUserView(total);
            DataTable(0, Footer.Limit, Search.Title, index);
        }

        public int CountVLoanRequestContracts()
        {
            var total = _context.VLoanRequestContracts
                .Where(c => c.CurrentStatusId == StatusId &&
                !AllStatusId.Contains(c.CurrentStatusId.Value))
                .OrderBy(c => c.AdminUploadDate).Count();
            return total;
        }

        protected void SetUserView(int count)
        {
            if (count > 0)
            {
                Footer.Count = count;
                Footer.TotalPages = (int)Math.Ceiling(count / (double)Footer.Limit);
            }
        }

        protected void SelectPageSize(ChangeEventArgs e)
        {
            Footer.Limit = Convert.ToInt32(e.Value.ToString());
            Footer.TotalPages = (int)Math.Ceiling(Footer.Count / (double)Footer.Limit);
            Footer.CurrentPage = 1;
            UpdateList(Footer.CurrentPage);
        }

        protected void UpdateList(int CurPage)
        {
            var end = (Footer.Limit * CurPage);
            var statr = (Footer.Limit * CurPage) - Footer.Limit;
            Footer.CurrentPage = CurPage;
            DataTable(statr, Footer.Limit, Search.Title, index);
        }

        protected void NavigateTo(string Direction)
        {
            if (Direction == "Prev" && Footer.CurrentPage != 1)
            {
                Footer.CurrentPage -= 1;
            }
            if (Direction == "Next" && Footer.CurrentPage != Footer.TotalPages)
            {
                Footer.CurrentPage += 1;
            }
            if (Direction == "First")
            {
                Footer.CurrentPage = 1;
            }
            if (Direction == "Last")
            {
                Footer.CurrentPage = Footer.TotalPages;
            }

            UpdateList(Footer.CurrentPage);
        }

        protected void SelectCurrentPage(ChangeEventArgs e)
        {
            Footer.CurrentPage = Convert.ToInt32(e.Value.ToString());
            UpdateList(Footer.CurrentPage);
        }

        public void SearchData(string? text, VLoanRequestContract index)
        {
            Search.Title = !string.IsNullOrEmpty(text) ? text : string.Empty;
            Footer.CurrentPage = 1;

            if (!string.IsNullOrEmpty(text))
            {
                var total = _context.VLoanRequestContracts
                    .Where(c => c.DebtorNameTh.Contains(text) || c.DebtorSnameTh.Contains(text))
                    .Count();
                if (total != 0)
                {
                    SetUserView(total);
                    DataTable(0, Footer.Limit, text, index);
                }
                else
                {
                    ReqCon = new();
                    SetUserView(1);
                }
            }
            else if (!string.IsNullOrEmpty(text) && index.LoanTypeId != 0)
            {
                var total = _context.VLoanRequestContracts.Where(c =>
                                c.LoanTypeId == index.LoanTypeId &&
                                c.DebtorNameTh.Contains(text) ||
                                c.DebtorSnameTh.Contains(text)
                                ).Count();
                if (total != 0)
                {
                    SetUserView(total);
                    DataTable(0, Footer.Limit, text, index);
                }
                else
                {
                    ReqCon = new();
                    SetUserView(1);
                }
            }
            else if (index.LoanTypeId != 0)
            {
                var total = _context.VLoanRequestContracts
                    .Where(c => !AllStatusId.Contains(c.CurrentStatusId.Value) &&
                    c.LoanTypeId == index.LoanTypeId)
                    .Count();

                if (total != 0)
                {
                    SetUserView(total);
                    DataTable(0, Footer.Limit, text, index);
                }
                else
                {
                    ReqCon = new();
                    SetUserView(1);
                }
            }
            else
            {
                StartTable();
            }
        }

        public void DataTable(int start, int end, string searchName, VLoanRequestContract index)
        {
            ReqCon = new();
            if (!string.IsNullOrEmpty(searchName))
            {
                ReqCon = _context.VLoanRequestContracts
                    .Where(c => c.CurrentStatusId == StatusId &&
                    !AllStatusId.Contains(c.CurrentStatusId.Value) &&
                    (c.DebtorNameTh.Contains(searchName) ||
                    c.DebtorSnameTh.Contains(searchName)))
                    .OrderBy(c => c.AdminUploadDate)
                    .Skip(start)
                    .Take(end)
                    .ToList();
            }
            else if (!string.IsNullOrEmpty(searchName) && index.LoanTypeId != 0)
            {
                ReqCon = _context.VLoanRequestContracts
                    .Where(c => c.CurrentStatusId == StatusId &&
                    !AllStatusId.Contains(c.CurrentStatusId.Value) &&
                    c.LoanTypeId == index.LoanTypeId &&
                    (c.DebtorNameTh.Contains(searchName) ||
                    c.DebtorSnameTh.Contains(searchName)))
                    .OrderBy(c => c.AdminUploadDate)
                    .Skip(start)
                    .Take(end)
                    .ToList();
            }
            else if (index.LoanTypeId != 0)
            {
                ReqCon = _context.VLoanRequestContracts
                    .Where(c => c.CurrentStatusId == StatusId &&
                    !AllStatusId.Contains(c.CurrentStatusId.Value) &&
                    c.LoanTypeId == index.LoanTypeId)
                    .OrderBy(c => c.AdminUploadDate)
                    .Skip(start)
                    .Take(end)
                    .ToList();
            }
            else
            {
                var total = CountVLoanRequestContracts();
                SetUserView(total);
                ReqCon = _context.VLoanRequestContracts
                        .Where(c => c.CurrentStatusId == StatusId &&
                        !AllStatusId.Contains(c.CurrentStatusId.Value))
                        .OrderBy(c => c.AdminUploadDate)
                        .Skip(start)
                        .Take(end)
                        .ToList();
            }
        }

        protected void SelectLoanTypeID(ChangeEventArgs e)
        {
            var TypeID = Convert.ToInt32(e.Value.ToString());
            index.LoanTypeId = (byte?)TypeID;
            SearchData(Search.Title, index);
        }

        private void CheckboxClicked(decimal reqId, object checkedValue)
        {
            if ((bool)checkedValue)
            {
                if (!SelestReqId.Contains(reqId))
                {
                    SelestReqId.Add(reqId);
                }
            }
            else
            {
                if (SelestReqId.Contains(reqId))
                {
                    SelestReqId.Remove(reqId);
                }
            }
        }

        private async Task SetOrClearCheckedAsync(bool data, List<VLoanRequestContract> LreqCon)
        {
            if (LreqCon.Count > 0)
            {
                for (int i = 0; i < LreqCon.Count; i++)
                {
                    var id = LreqCon[i].LoanRequestId;
                    if (data)
                    {
                        bool isExist = SelestReqId.Contains(id);
                        if (!isExist)
                        {
                            SelestReqId.Add(id);
                            await JS.InvokeVoidAsync("SetCheckedCheckBox", id);
                        }
                    }
                    else
                    {
                        bool isExist = SelestReqId.Contains(id);
                        if (isExist)
                        {
                            SelestReqId.Remove(id);
                            await JS.InvokeVoidAsync("ClearCheckedCheckBox", id);
                        }
                    }
                }
            }
        }

        public async Task ConFirmPageAsync(bool pass, List<decimal> LreqId)
        {
            string Action = string.Empty;
            string Remark = string.Empty;
            bool Icon = true;
            decimal? StatusId = 9;

            if (pass) // true
            {
                //กรณีอนุมัติการกู้
                Action = "ดำเนินการอนุมัติเสร็จสิ้น";
                Remark = $"ลงนามเห็นควรอนุมัติสัญญาการกู้ จำนวน {LreqId.Count} ราย";
            }
            else
            {
                //กรณีไม่อนุมัติการกู้
                Action = "ไม่อนุมัติการกู้";
                Remark = $"ลงนามไม่เห็นเห็นควรอนุมัติสัญญาการกู้ จำนวน {LreqId.Count} ราย";
                Icon = false;
                StatusId = 3;
            }

            for (int i = 0; i < LreqId.Count; i++)
            {
                var item = SelestReqId[i];
                var GetReqCon = userService.GetVLoanRequestContract(item);

                await SaveToDbAsync(StatusId, GetReqCon.ContractId);
                await SetDataBySentEmailAsync(pass, GetReqCon);
            }

            await SentMessageAsync(Action, Remark, Icon);
        }

        private async Task SaveToDbAsync(decimal? StatusId, decimal? ContractId)
        {
            string ModifyBy = userService.FindStaffId(userStateProvider?.CurrentUser.StaffId);
            ContractMain? contract = _context.ContractMains
                .Where(c => c.ContractId == ContractId)
                .FirstOrDefault();

            if (contract != null)
            {
                contract.ApproveStaffId = ModifyBy;
                contract.ApproveDate = DateTime.Now;
                contract.ContractRemark = string.Empty;
                contract.ContractStatusId = StatusId;

                _context.Update(contract);
                await _context.SaveChangesAsync();

                decimal LoanStatusId = 5;
                await LogService.GetHisContractMainByContractIDAsync(contract.ContractId, LoanStatusId, ModifyBy);
            }
        }

        private async Task SetDataBySentEmailAsync(bool pass, VLoanRequestContract ReqCon)
        {
            string messOther = "ไม่ผ่านการพิจารณาโดยผู้บริหาร";

            if (pass)
            {
                messOther = "ได้ผ่านการพิจารณาโดยผู้บริหาร";
            }

            var StaffDetail = userService.GetUserDetail(ReqCon.DebtorStaffId);
            var DebtorName = userService.GetFullName(ReqCon.DebtorStaffId);

            var GuarantorDetail = userService.GetUserDetail(ReqCon.ContractGuarantorStaffId);
            var GuarantoName = userService.GetFullName(ReqCon.ContractGuarantorStaffId);

            ApplyLoanModel loan = new();
            loan.LoanTypeID = ReqCon.LoanTypeId;
            loan.LoanAmount = ReqCon.ContractLoanAmount != null ?
                ReqCon.ContractLoanAmount.Value :
                0;
            loan.LoanNumInstallments = ReqCon.ContractLoanNumInstallments != null ?
                (int)ReqCon.ContractLoanNumInstallments.Value :
                0;

            #region ผู้กู้
            if (!string.IsNullOrEmpty(StaffDetail?.StaffEmail))
            {
                var Name = userService.GetFullName(StaffDetail.StaffId);
                string? Email = MailService.GetEmailPsu(StaffDetail.StaffEmail);

                //string user = userService.FindUserName(StateProvider?.CurrentUser.UserName);
                //if (Utility.UserDev.Contains(user))
                //{
                //    Name = $"(mailTest-{user}) {Name}";
                //    Email = Utility.SendMailTest;
                //}

                var email = MailService.MailDebtorByManagerStatus9(Name,
                    Email,
                    DebtorName,
                    GuarantoName,
                    loan,
                    messOther);
                MailService.SendEmail(email);
            }
            #endregion

            #region ผู้ค้ำ
            if (!string.IsNullOrEmpty(GuarantorDetail?.StaffEmail))
            {
                var Name = userService.GetFullName(GuarantorDetail.StaffId);
                string? Email = MailService.GetEmailPsu(GuarantorDetail.StaffEmail);

                //string user = userService.FindUserName(StateProvider?.CurrentUser.UserName);
                //if (Utility.UserDev.Contains(user))
                //{
                //    Name = $"(mailTest-{user}) {Name}";
                //    Email = Utility.SendMailTest;
                //}

                var email = MailService.MailDebtorByManagerStatus9(Name,
                    Email,
                    DebtorName,
                    GuarantoName,
                    loan,
                    messOther);
                MailService.SendEmail(email);
            }
            #endregion

            #region Admin
            List<VLoanStaffPrivilege> vLoanStaffPrivilege = await psuLoan.GetVLoanStaffPrivilegeByCampId(StaffDetail?.CampId);
            List<string> listEmailAdmin = psuLoan.GetAllEmailAdmin(vLoanStaffPrivilege);

            if (listEmailAdmin.Any())
            {
                var emailAdmin = MailService.MailAdminByManagerStatus9("แอดมิน วิทยาเขต",
                    //Utility.MailAdmin,
                    string.Empty,
                    DebtorName,
                    GuarantoName,
                    loan,
                    messOther,
                    listEmailAdmin);
                MailService.SendEmail(emailAdmin);
            }

            #endregion 
        }

        public async Task SentMessageAsync(string Action, string Remark, bool Icon)
        {
            List<object> Message = new();
            MessageModel mes = new()
            {
                Action = Action,
                Remark = Remark,
                Icon = Icon,
                ToPage = $"/Manager/ManageLoanRequest",
                Message = Message,
            };

            await sessionStorage.SetItemAsync("Message", mes);
            navigationManager.NavigateTo("/Message");
        }

    }
}
