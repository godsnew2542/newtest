using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanApp.Components.Loading
{
    public partial class Loading
    {
        [Parameter]
        public string Message { get; set; } = string.Empty;
    }
}
