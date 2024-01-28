class MentionModule {

    constructor(dotnet) {
        this.dotnet = dotnet;
    }

    __getMentionConfig(quillOptions, initialConfig, editorElement) {
        const atValues = [
            { id: 1, value: "Fredrik Sundqvist" },
            { id: 2, value: "Patrik Sjölin" }
        ];
        const hashValues = [
            { id: 3, value: "Fredrik Sundqvist 2" },
            { id: 4, value: "Patrik Sjölin 2" }
        ];

        window.addEventListener('mention-hovered', (event, a, b) => {
            var isWithin = BlazorJS.EventHelper.isWithin(event, editorElement);
            debugger;
            var x = a;
            var y = b;
            if (isWithin) {
                console.log('hovered: ', event);
            }
        }, false);
        window.addEventListener('mention-clicked', (event) => { console.log('clicked: ', event) }, false);

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
                source: (searchTerm, renderList, mentionChar) => {
                    let values;

                    if (mentionChar === "@") {
                        values = atValues;
                    } else {
                        values = hashValues;
                    }

                    if (searchTerm.length === 0) {
                        renderList(values, searchTerm);
                    } else {
                        const matches = [];
                        for (let i = 0; i < values.length; i++)
                            if (
                                ~values[i].value.toLowerCase().indexOf(searchTerm.toLowerCase())
                            )
                                matches.push(values[i]);
                        renderList(matches, searchTerm);
                    }
                }
            }
        }
    }

    dispose() {
        alert('dispose');
    }
}

window.MentionModule = MentionModule;

export function initializeMentionModule(dotnet) {
    return new MentionModule(dotnet);
}