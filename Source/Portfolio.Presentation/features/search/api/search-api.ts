import { env } from "@/lib/env";
import { SearchResultType } from "../types";

export async function fetchSearchResults(query: string): Promise<SearchResultType[]> {
    const response = await fetch(
        `${env.SEARCH_API_URL}/search?q=${encodeURIComponent(query)}`
    );

    if (!response.ok) {
        throw new Error("Failed to fetch search results");
    }

    const data = await response.json();

    return data.results.$values.map((item: SearchResultType) => ({
        id: item.id,
        title: item.title,
        url: item.url,
        snippet: item.snippet,
        displayUrl: item.displayUrl,
        lastUpdated: item.lastUpdated,
    }));
}
