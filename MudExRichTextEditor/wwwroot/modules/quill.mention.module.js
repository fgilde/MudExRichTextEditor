class MentionModule {
    
    constructor(dotnet) {
        this.dotnet = dotnet;
        window.addEventListener('mention-hovered', this._onMentionHovered = this.onMentionHovered.bind(this));
        window.addEventListener('mention-clicked', this._onMentionClicked = this.onMentionClicked.bind(this));
    }

    onMentionHovered(event) {
        console.log(event);
        this.dotnet.invokeMethodAsync('OnMentionHovered', event.value);
    }

    onMentionClicked(event) {
        this.dotnet.invokeMethodAsync('OnMentionClicked', event.value);
    }

    __getMentionConfig(quillOptions, initialConfig, editorElement) {
        return {
            mention: {
                allowedChars: /^[A-Za-z\sÅÄÖåäö]*$/,
                mentionDenotationChars: ["@", "#"],
                clicked: (item) => {
                    debugger;
                },
                onSelect: (item, insertItem) => {
                    this.dotnet.invokeMethodAsync('OnBeforeSelect', item);
                    insertItem(item);
                    this.dotnet.invokeMethodAsync('OnAfterSelect', item);
                },
                source: async (searchTerm, renderList, mentionChar) => {
                    const matchedPeople = await this.dotnet.invokeMethodAsync('GetSuggestions', mentionChar, searchTerm);
                    renderList(matchedPeople);
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

export function initializeMentionModule(dotnet) {
    return new MentionModule(dotnet);
}