using LoanApp.Model.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LoanApp.Components.Ant
{
    public partial class StepsAnt
    {
        [Parameter] public int Current { get; set; } = 0;
        [Parameter] public StepsModel[] Stepes { get; set; } = Array.Empty<StepsModel>();
		private bool IsMobile { get; set; } = false;

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if(firstRender)
			{
				IsMobile = await JS.InvokeAsync<bool>("isDevice");
				
				StateHasChanged();
			}
		}
	}
}
