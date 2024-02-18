class MentionModule {

    constructor(dotnet, options) {
        this.options = options;
        this.dotnet = dotnet;
        window.addEventListener('mention-hovered', this._onMentionHovered = this.onMentionHovered.bind(this));
        window.addEventListener('mention-clicked', this._onMentionClicked = this.onMentionClicked.bind(this));
    }

    onMentionHovered(event) {
        if (event?.value?.__editorId === this.editor.id) { // because maybe we have multiple editors on the same page
            event.value.data = event.value?.data || JSON.parse(event.value.__dataJson);
            this.dotnet.invokeMethodAsync('OnMentionHovered', event.value);
        }
    }

    onMentionClicked(event) {
        if (event?.value?.__editorId === this.editor.id) { // because maybe we have multiple editors on the same page
            event.value.data = event.value?.data || JSON.parse(event.value.__dataJson);
            this.dotnet.invokeMethodAsync('OnMentionClicked', event.value);
        }
    }

    getMentions() {
        //var module = this.editor.__quill.getModule('mention');
        var contents = this.editor.__quill.getContents();
        var result = contents.ops.filter(op => op.insert && op.insert.mention).map(op => {
            var item = op.insert.mention;
            return {
                denotationChar: item.denotationChar,
                id: item.id,
                value: item.value,
                data: item.data || JSON.parse(item.__dataJson)
            };
        });
        return result;
    }

    __getMentionConfig(quillOptions, initialConfig, editorElement) {
        this.editor = editorElement;
        return {
            mention: {
                allowedChars: /^[A-Za-z\sÅÄÖåäö]*$/,
                dataAttributes: ['id', 'value', 'denotationChar', 'link', 'target', 'disabled', '__dataJson', '__editorId'],
                mentionDenotationChars: this.options.denotationChars,
                onSelect: (item, insertItem) => {
                    this.dotnet.invokeMethodAsync('OnBeforeSelect', item);
                    insertItem(item);
                    this.dotnet.invokeMethodAsync('OnAfterSelect', item);
                },
                source: async (searchTerm, renderList, mentionChar) => {
                    renderList((await this.dotnet.invokeMethodAsync('GetSuggestions', mentionChar, searchTerm))
                        .map((item) => {
                            item.__editorId = editorElement.id;
                            item.__dataJson = JSON.stringify(item.data);
                            return item;
                        }));
                }
            }
        }
    }

    dispose() {
        window.removeEventListener('mention-hovered', this._onMentionHovered);
        window.removeEventListener('mention-clicked', this._onMentionClicked);
    }
}

window.MentionModule = MentionModule;

export function initializeMentionModule(dotnet, options) {
    return new MentionModule(dotnet, options);
}