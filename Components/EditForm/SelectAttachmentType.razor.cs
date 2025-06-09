using LoanApp.DatabaseModel.LoanEntities;
using Microsoft.AspNetCore.Components;

namespace LoanApp.Components.EditForm
{
    public partial class SelectAttachmentType
    {
        [Parameter] public decimal TitleId { get; set; }

        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;

        private List<AttachmentType> AttachmentType { get; set; } = new();
        private List<decimal> SelestAttachmentId { get; set; } = new();
        private List<decimal> AttachmentTypeConfirm { get; set; } = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    AttachmentType = await psuLoan.GetListAttachmentTypeByContractStepId(TitleId);

                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
                }
            }
        }

        private void CheckboxClicked(decimal clubID, object checkedValue)
        {
            if ((bool)checkedValue)
            {
                if (!SelestAttachmentId.Contains(clubID))
                {
                    SelestAttachmentId.Add(clubID);
                }
            }
            else
            {
                if (SelestAttachmentId.Contains(clubID))
                {
                    SelestAttachmentId.Remove(clubID);
                }
            }
        }

        private async Task ConfirmAsync()
        {
            AttachmentTypeConfirm = new();
            var NameStorage = $"AttachmentType_{TitleId}";

            var checkData = await sessionStorage.GetItemAsStringAsync(NameStorage);
            if (checkData != null)
            {
                await sessionStorage.RemoveItemAsync(NameStorage);
            }

            if (SelestAttachmentId.Count != 0)
            {
                foreach (var id in SelestAttachmentId)
                {
                    AttachmentTypeConfirm.Add(id);
                }
            }
            await sessionStorage.SetItemAsync(NameStorage, AttachmentTypeConfirm);
        }
    }
}
