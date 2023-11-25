using System;
using System.Threading.Tasks;
using BlazorJS;
using Microsoft.AspNetCore.Components;

namespace MudExRichTextEditor;

public partial class MudExRichTextEdit
{
	private bool _initialized = false;

	[Parameter] public RenderFragment EditorContent { get; set; }
	[Parameter] public RenderFragment ToolbarContent { get; set; }
	[Parameter] public bool ReadOnly { get; set; } = false;
	[Parameter] public string Placeholder { get; set; } = "Insert text here...";
	[Parameter] public string Theme { get; set; } = "snow";
	[Parameter] public string DebugLevel { get; set; } = "info";

	internal ElementReference QuillElement;
	internal ElementReference ToolBar;

	protected override async Task OnInitializedAsync()
	{
		//await JsRuntime.InvokeVoidAsync("eval", "BlazorJS.isLoaded = function() { return false; }");
		await JsRuntime.LoadFilesAsync(
			"./_content/MudExRichTextEditor/lib/quill/quill.bubble.css",
			"./_content/MudExRichTextEditor/lib/quill/quill.snow.css",
			"./_content/MudExRichTextEditor/lib/quill/quill.js",
			"./_content/MudExRichTextEditor/BlazorQuill.js",
			"./_content/MudExRichTextEditor/quill-blot-formatter.min.js"
		);
		await JsRuntime.WaitForNamespaceAsync(Quill.Namespace, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(300));
		await base.OnInitializedAsync();
		await CreateEditor();
	}


	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && _initialized)		
			await CreateEditor();		
	}
	

	public async Task<string> GetHTML()
		=> await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement.__quill.root.innerHTML, QuillElement);
	public async Task<string> GetText()
		=> await JsRuntime.DInvokeAsync<string>((_, quillElement) => quillElement.__quill.getText(), QuillElement);
	public async Task<string> GetContent()
		=> await JsRuntime.DInvokeAsync<string>((window, quillElement) => window.JSON.stringify(quillElement.__quill.getContents()), QuillElement);
	public async Task EnableEditor(bool mode) 
		=> await JsRuntime.DInvokeVoidAsync((_, quillElement, mode) => quillElement.__quill.enable(mode), QuillElement, mode);

	public async Task LoadContent(string content)
	{
		await JsRuntime.DInvokeVoidAsync((window, quillElement, content) =>
		{
			var parsedContent = window.JSON.parse(content);
			quillElement.__quill.setContents(parsedContent, "api");
		}, QuillElement, content);
	}


	public async Task InsertImage(string ImageURL)
	{
		await Quill.InsertImage(
			JsRuntime, QuillElement, ImageURL);
	}

	private async Task CreateEditor()
	{
		await Quill.Create(
			JsRuntime,
			QuillElement,
			ToolBar,
			ReadOnly,
			Placeholder,
			Theme,
			DebugLevel);
	}
}