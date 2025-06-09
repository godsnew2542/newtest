using Microsoft.AspNetCore.Components;
using LoanApp.Model.Models;
using LoanApp.Shared;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models.Data;

namespace LoanApp.Components.AdminOption
{
    public partial class PayOffOption
    {
        #region Parameter
        [Parameter] public EventCallback<FormAdminOptionModel> OnPayOffChange { get; set; }
        [Parameter] public PaymentTransaction Transaction { get; set; } = new();
        [Parameter] public FormAdminOptionModel FormOption { get; set; } = new();
        [Parameter] public bool IsEqualsLoanNumInstallments { get; set; } = false;

        #endregion

        private string GenId { get; set; } = "Upload_file";
        private string myFile { get; set; } = "my_file";

        private decimal TempBalanceAmount { get; set; } = 0;
        private bool IsEditBalanceAmount { get; set; } = false;
        private decimal? BalanceValueTemp { get; set; } = 0;


        public async Task CurrentRemoveListAsync(int value)
        {
            var SelectListUpload = await SaveFileAndImgService.ReadStorageSelectUploadAsync();
            if (SelectListUpload != null)
            {
                var myTodo = FormOption.PayOff.ReferenceFile.First(x => x.Id == SelectListUpload.Id);
                FormOption.PayOff.ReferenceFile.Remove(myTodo);
                File.Delete(SelectListUpload.Url!);
                await SaveFileAndImgService.RemoveStorageAsync();
            }
        }

        protected override void OnInitialized()
        {
            FormOption.Display = new();
            FormOption.PayOff.InstallmentNo = Transaction.InstallmentNo;
            FormOption.PayOff.PrincipleAmount = Transaction.PrincipleAmount;
            FormOption.PayOff.InterestAmont = Transaction.InterestAmont;
            DisplayModel? display = _context.VLoanRequestContracts
                    .Where(c => c.LoanRequestId.Equals(FormOption.LoanRequestId))
                    .Select(c => new DisplayModel
                    {
                        LoanTotalAmount = (c.ContractLoanTotalAmount != null ?
                        c.ContractLoanTotalAmount :
                        TransactionService.FindLoanTotalAmount(FormOption.ContractId))
                    })
                    .FirstOrDefault();

            if (display != null)
            {
                FormOption.Display = display;
            }

            if (IsEqualsLoanNumInstallments)
            {
                FormOption.PayOff.TotalAmount = Transaction.TotalAmount;
            }
            else
            {
                FormOption.PayOff.TotalAmount = ResultTotalAmount(Transaction.PrincipleAmount, Transaction.InterestAmont);
            }

            FormOption.PayOff.BalanceAmount = TransactionService.GetBalanceTotal(FormOption.ContractId, FormOption.LoanAmount, false);

            TempBalanceAmount = (FormOption.PayOff.BalanceAmount == null ? 0 : FormOption.PayOff.BalanceAmount.Value);

            FormOption.PayOff.BalanceValue = TempBalanceAmount;
        }

        /// <summary>
        /// รวมชำระ
        /// </summary>
        /// <param name="principleAmount">เงินต้น</param>
        /// <param name="interestAmont">ดอกเบี้ย</param>
        /// <returns></returns>
        private static decimal? ResultTotalAmount(decimal? principleAmount, decimal? interestAmont)
        {
            decimal? result = principleAmount + interestAmont;
            return result == null ? 0 : result;
        }

        private decimal SplitFormatNumber(decimal? _value)
        {
            var Fnumber = FormatNumber(_value);
            var pp = Fnumber.Split(".");
            decimal value;
            if (pp[1] == "00")
            {
                value = Convert.ToDecimal(_value);
            }
            else
            {
                value = Convert.ToDecimal(Fnumber);
            }
            return value;
        }

        private async Task InstallmentNunberChangeAsync(decimal? value)
        {
            FormOption.PayOff.InstallmentNo = value;
            await OnPayOffChange.InvokeAsync(FormOption);
        }

        private async Task PrincipleAmountChangeAsync(decimal? _value)
        {
            FormOption.PayOff.PrincipleAmount = SplitFormatNumber(_value);
            FormOption.PayOff.TotalAmount = ResultTotalAmount(FormOption.PayOff.PrincipleAmount, FormOption.PayOff.InterestAmont);

            //FormOption.PayOff.BalanceValue = TempBalanceAmount - FormOption.PayOff.TotalAmount;
            FormOption.PayOff.BalanceValue = TempBalanceAmount - FormOption.PayOff.PrincipleAmount;


            await OnPayOffChange.InvokeAsync(FormOption);
        }

        private async Task InterestAmontChangeAsync(decimal? _value)
        {
            FormOption.PayOff.InterestAmont = SplitFormatNumber(_value);
            FormOption.PayOff.TotalAmount = ResultTotalAmount(FormOption.PayOff.PrincipleAmount, FormOption.PayOff.InterestAmont);

            //FormOption.PayOff.BalanceValue = TempBalanceAmount - FormOption.PayOff.TotalAmount;

            await OnPayOffChange.InvokeAsync(FormOption);
        }

        private async Task PayDateChangeAsync(DateTime? Date)
        {
            FormOption.PayOff.Date = dateService.ConvertToDateTime(Date);
            await OnPayOffChange.InvokeAsync(FormOption);
        }

        private string FormatNumber(decimal? data)
        {
            string value = "0";
            if (data != null)
            {
                value = data.Value.ToString("n2");
            }
            return value;
        }

        private void SetCurrentDataAsync(DTEventArgs value)
        {
            UploadModel uploadModel = new()
            {
                Id = FormOption.PayOff.ReferenceFile.Count + 1,
                Name = value.Params[0].ToString(),
                Url = value.Params[1].ToString(),
                TempImgName = value.Params[2].ToString()
            };

            FormOption.PayOff.ReferenceFile.Add(uploadModel);
        }

        private void BalanceAmountChange(decimal? _value)
        {
            BalanceValueTemp = SplitFormatNumber(_value);
        }

        private async Task SetBalanceAmountAsync(decimal? _value)
        {
            FormOption.PayOff.BalanceValue = _value;
            IsEditBalanceAmount = false;
            await OnPayOffChange.InvokeAsync(FormOption);
        }
    }
}
