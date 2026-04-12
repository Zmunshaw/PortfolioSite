"use client";

import { useState } from "react";
import { SearchBarProps } from "../types";

export default function SearchBar({ onSearch }: SearchBarProps) {
    const [query, setQuery] = useState("");

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setQuery(e.target.value);
        onSearch(e.target.value);
    };

    return (
        <div>
            <input
                type="text"
                placeholder="Search..."
                value={query}
                onChange={handleChange}
                className="w-full rounded-full border border-zinc-300 px-5 py-3 text-base outline-none transition-shadow focus:shadow-md dark:border-zinc-700 dark:bg-zinc-900 dark:text-white"
            />
        </div>
    );
}
