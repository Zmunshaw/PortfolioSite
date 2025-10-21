export interface SearchResultType {
    id: string;
    title: string;
    url: string;
    snippet: string;
    // optional metadata:
    displayUrl?: string;
    lastUpdated?: string;
}

export interface SearchBarProps {
    onSearch: (query: string) => void;
}
