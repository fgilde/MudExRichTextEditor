(function () {
	window.QuillFunctions = {
		create: function (
			quillElement, toolBar, readOnly,
			placeholder, theme, debugLevel) {
				
			var options = {
				debug: debugLevel,
				modules: {
					toolbar: toolBar
				},
				placeholder: placeholder,
				readOnly: readOnly,
				theme: theme
			};

			new Quill(quillElement, options);
		},
		
		insertImage: function (quillElement, imageURL) {
			var Delta = Quill.import('delta');
			editorIndex = 0;

			if (quillElement.__quill.getSelection() !== null) {
				editorIndex = quillElement.__quill.getSelection().index;
			}

			return quillElement.__quill.updateContents(
				new Delta()
					.retain(editorIndex)
					.insert({ image: imageURL },
						{ alt: imageURL }));
		}
	};
})();