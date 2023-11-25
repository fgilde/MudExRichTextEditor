using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MudExRichTextEditor
{
	public static class Quill
	{
		public const string Namespace = "QuillFunctions";
		public static string Ns(params string[] args) => $"{Namespace}.{string.Join(".", args)}";
		public static string Fn(params string[] args) => Ns(args.Select(n => char.ToLower(n[0]) + n[1..]).ToArray());


        internal static ValueTask<object> Create(
			IJSRuntime jsRuntime,
			ElementReference quillElement,
			ElementReference toolbar,
			bool readOnly,
			string placeholder,
			string theme,
			string debugLevel)
		{
			return jsRuntime.InvokeAsync<object>(Fn(nameof(Create)),
				quillElement, toolbar, readOnly,
				placeholder, theme, debugLevel);
		}

		internal static ValueTask<object> InsertImage(
			IJSRuntime jsRuntime,
			ElementReference quillElement,
			string imageURL)
		{
			return jsRuntime.InvokeAsync<object>(
				Fn(nameof(InsertImage)),
				quillElement, imageURL);
		}
	}
}