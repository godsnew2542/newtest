using LoanApp.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Pages.User
{
    public partial class RequestLoan
    {
        [CascadingParameter]
        public Error Error { get; set; }

        public void ChangeGuarantor()
        {
            navigationManager.NavigateTo("/ChangeGuarantor");
        }

        public void AnotherRequest()
        {
            navigationManager.NavigateTo("./User/AnotherRequest");
        }

        public void CompoundLoan()
        {
            navigationManager.NavigateTo("./User/CompoundLoan");
        }
    }
}
