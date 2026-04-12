"use client";

import { useState } from "react";
import { SearchResultType } from "../types";
import { fetchSearchResults } from "../api/search-api";
import SearchBar from "./SearchBar";
import SearchResultList from "./SearchResultList";

export default function Search() {
    const [results, setResults] = useState<SearchResultType[]>([]);
    const [error, setError] = useState("");
    const [isLoading, setIsLoading] = useState(false);

    const handleSearch = async (query: string) => {
        if (!query.trim()) {
            setResults([]);
            return;
        }

        setIsLoading(true);
        setError("");

        try {
            const data = await fetchSearchResults(query.toLowerCase());
            setResults(data);
        } catch (err) {
            setError(err instanceof Error ? err.message : "Something went wrong");
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="mx-auto w-full max-w-3xl px-5 py-5">
            <SearchBar onSearch={handleSearch} />
            {isLoading && <p className="mt-4 text-zinc-500">Loading...</p>}
            {error && <p className="mt-4 text-red-500">{error}</p>}
            <SearchResultList results={results} />
        </div>
    );
}
