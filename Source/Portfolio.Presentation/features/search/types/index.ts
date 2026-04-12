export interface SearchResultType {
    id: string;
    title: string;
    url: string;
    snippet: string;
    displayUrl?: string;
    lastUpdated?: string;
}

export interface SearchBarProps {
    onSearch: (query: string) => void;
}
