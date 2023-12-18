# MudExRichTextEditor

MudExRichTextEditor is a custom reusable control that allows us to easily consume Quill combining in a MudBlazor project 
Features with MudBlazor Theme Support. This compnent also works without a MudBlazor Project:

- Exports editor contents in Text, HTML, and Quill’s native Delta format
- Allows value binding
- Provides a read only mode, suitable for displaying Quill’s native Delta format
- Allows custom toolbars to be defined
- Allows custom themes
- Has an inline editor mode that also allows custom toolbars

## What is Quill?
Quill is a free, [open source](https://github.com/quilljs/quill/) WYSIWYG editor built for the modern web. With its [modular architecture](https://quilljs.com/docs/modules/) and expressive [API](https://quilljs.com/docs/api/), it is completely customizable to fit any need.

![image](https://user-images.githubusercontent.com/9497415/122029542-dbcf1d80-cdc4-11eb-9034-6a57d22daac8.png)


## How to use

1. Install the NuGet package [MudExRichTextEditor](https://www.nuget.org/packages/MudExRichTextEditor/)

Add the following line to your _Imports.razor file

```c#
@using MudExRichTextEditor
```

And this is enough now you can simply use the component in your pages

```c#
@page "/"

<MudCheckBox Label="Read only" @bind-Checked="_readOnly" />

<MudExRichTextEdit @ref="@Editor"
                   ReadOnly="@_readOnly"
                   Height="444"
                   Class="m-2"
                   Placeholder="Edit html">
			 
</MudExRichTextEdit>

@code {
	// Quill editor
    bool _readOnly = false;
	MudExRichTextEdit Editor;
	

}
```