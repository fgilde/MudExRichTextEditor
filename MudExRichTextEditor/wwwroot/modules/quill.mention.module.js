class MentionModule {

    constructor(dotnet, options) {
        this.options = options;
        this.dotnet = dotnet;
        this.type = options.type;
        window.addEventListener('mention-hovered', this._onMentionHovered = this.onMentionHovered.bind(this));
        window.addEventListener('mention-clicked', this._onMentionClicked = this.onMentionClicked.bind(this));
    }

    onMentionHovered(event) {
        if (event?.value?.__editorId === this.editor.id && event?.value?.__type === this.type) { // because maybe we have multiple editors on the same page
            event.value.data = event.value?.data || JSON.parse(event.value.__dataJson);
            this.dotnet.invokeMethodAsync('OnMentionHovered', event.value);
        }
    }

    onMentionClicked(event) {
        if (event?.value?.__editorId === this.editor.id && event?.value?.__type === this.type) { // because maybe we have multiple editors on the same page
            event.value.data = event.value?.data || JSON.parse(event.value.__dataJson);
            this.dotnet.invokeMethodAsync('OnMentionClicked', event.value);
        }
    }

    getMentions() {
        return this.editor.__quill.getContents().ops.filter(op => op.insert && op.insert.mention && op.insert.mention.__type === this.type)
            .map(op => this.__getMentionItem(op.insert.mention));
    }

    __getMentionItem(item) {
        return {
            denotationChar: item.denotationChar,
            id: item.id,
            value: item.value,
            data: item.data || JSON.parse(item.__dataJson)
        };
    }

    _checkPosition() {
        const container = this.editor.querySelector('.ql-mention-list-container');
        const quill = this.editor.__quill;
        const range = quill.getSelection();
        if (range) {
            if (range.length === 0) {
                const bounds = quill.getBounds(range.index);
                const editorBounds = quill.container.getBoundingClientRect();

                const absoluteTop = editorBounds.top + bounds.top;
                const absoluteLeft = editorBounds.left + bounds.left;

                container.style.position = 'fixed';
                container.style.top = absoluteTop + 'px';
                container.style.left = absoluteLeft + 'px';
            }
        }
    }



    __getMentionConfig(quillOptions, initialConfig, editorElement) {
        //ql-mention-list-container
        this.editor = editorElement;
        var existing = quillOptions?.modules?.mention;

        var newConfig = {
            allowedChars: /^[A-Za-z\sÅÄÖåäö]*$/,
            dataAttributes: ['id', 'value', 'denotationChar', 'link', 'target', 'disabled', '__dataJson', '__editorId', '__type'],
            mentionDenotationChars: this.options.denotationChars,
            onSelect: (item, insertItem) => {
                if (item?.__editorId === this.editor.id && item?.__type === this.type) {
                    var value = this.__getMentionItem(item);
                    this.dotnet.invokeMethodAsync('OnBeforeSelect', value);
                    insertItem(item);
                    this.dotnet.invokeMethodAsync('OnAfterSelect', value);
                }
            },
            source: async (searchTerm, renderList, mentionChar) => {
                if (this.options?.denotationChars?.includes(mentionChar)) {         
                    renderList((await this.dotnet.invokeMethodAsync('GetSuggestions', mentionChar, searchTerm))
                        .map((item) => {
                            item.__editorId = editorElement.id;
                            item.__type = this.type;
                            item.__dataJson = JSON.stringify(item.data);
                            return item;
                        }));
                    this._checkPosition();
                }
            }
        }


        var result = {
            mention: {
                ...existing,
                ...newConfig,
                mentionDenotationChars: existing?.mentionDenotationChars
                    ? [...new Set([...existing.mentionDenotationChars, ...newConfig.mentionDenotationChars])]
                    : newConfig.mentionDenotationChars,
                onSelect: (item, insertItem) => {
                    existing?.onSelect?.(item, insertItem);
                    newConfig.onSelect(item, insertItem);
                },
                source: async (searchTerm, renderList, mentionChar) => {
                    if (existing?.source) {
                        await existing.source(searchTerm, renderList, mentionChar);
                    }
                    await newConfig.source(searchTerm, renderList, mentionChar);
                }
            }
        };

        return result;

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