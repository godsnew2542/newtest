using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.Model.Models;
using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace LoanApp.Pages.AdminCenter
{
    public partial class ManageDocument
    {
        [CascadingParameter] private Error Error { get; set; } = null!;

        [Parameter] public decimal TitleId { get; set; }

        private List<ContractStep> AttachmentSteps { get; set; } = new();
        private List<AttachmentType> AttachmentType { get; set; } = new();
        private Document Doc { get; set; } = new();
        private List<Document> ListDoc { get; set; } = new();

        private string? StepName { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await AttachmentDocumentAsync();
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }
        }

        private async Task AttachmentDocumentAsync()
        {
            AttachmentSteps = await _context.ContractSteps.ToListAsync();
            AttachmentType = await _context.AttachmentTypes.ToListAsync();
        }

        private void AddStep(decimal StepId, string? name)
        {
            Doc.DocumentId = StepId;
            StepName = name;
        }

        private async Task AddDataAsync(Document data)
        {
            if (!string.IsNullOrEmpty(data.DocumentName) && data.DocumentId != 0)
            {
                data.Id = ListDoc.Count + 1;
                ListDoc.Add(data);
            }
            else
            {
                string alert = $"กรุณาตรอบสอบข้อมูล";
                await JS.InvokeVoidAsync("displayTickerAlert", alert);
            }
            Doc = new Document();
        }

        private void Back()
        {
            navigationManager.NavigateTo("/Admin/ManageTypeLoan");
        }

        private async Task SubmitDataAsync()
        {
            try
            {
                if (ListDoc.Count != 0)
                {
                    for (int i = 0; i < ListDoc.Count; i++)
                    {
                        var doc = ListDoc[i];
                        AttachmentType attaType = new();

                        attaType.AttachmentNameThai = doc.DocumentName;
                        attaType.ContractStepId = doc.DocumentId;

                        _context.AttachmentTypes.Add(attaType);
                        await _context.SaveChangesAsync();
                    }
                    await SetMessageAsync();
                    navigationManager.NavigateTo("/Message");
                }
                else
                {
                    string alert = $"ไม่พบข้อมูลที่คุณเพิ่ม";
                    await JS.InvokeVoidAsync("displayTickerAlert", alert);
                }
            }
            catch (Exception ex)
            {
                await Error.ProcessError(ex);
            }

        }

        private void RemoveDoc(Document data)
        {
            var myTodo = ListDoc.FirstOrDefault(x => x.Id == data.Id);
            if (myTodo != null)
            {
                ListDoc.Remove(myTodo);
            }
        }

        private async Task SetMessageAsync()
        {
            List<object> Message = new();
            MessageModel mes = new();
            mes.Message = Message;
            mes.Action = "คุณได้เพิ่มเอกสารสำเร็จ";
            mes.ToPage = $"Admin/ManageTypeLoan";
            mes.HtmlText = string.Empty;

            for (int i = 0; i < AttachmentSteps.Count; i++)
            {
                var Step = AttachmentSteps[i];
                var StepName = Step.ContractStepName;
                bool AttachmentNotNull = false;
                List<object> TextDocList = new();

                for (int x = 0; x < ListDoc.Count; x++)
                {
                    var item = ListDoc[x];
                    if (item.DocumentId == Step.ContractStepId)
                    {
                        AttachmentNotNull = true;
                        TextDocList.Add(item.DocumentName);
                    }
                }
                if (AttachmentNotNull)
                {
                    mes.HtmlText = $"{mes.HtmlText} <div style='color: #2788DE;font-size:25 px;text-align: left;'>ขั้นตอนการ {StepName}</div>";
                    foreach (var textDoc in TextDocList)
                    {
                        mes.HtmlText = $"{mes.HtmlText} <div style='padding-left: 10px; text-align: left;'>- {textDoc}</div>";
                    }
                    mes.HtmlText = $"{mes.HtmlText} <hr/>";
                }
            }
            await sessionStorage.SetItemAsync("Message", mes);
        }
    }

    public class Document
    {
        public string DocumentName { get; set; } = string.Empty;
        public decimal DocumentId { get; set; } = 0;
        public decimal Id { get; set; } = 0;
    }
}