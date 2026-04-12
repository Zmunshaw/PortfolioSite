import { SearchResultType } from "../types";
import SearchResult from "./SearchResult";

interface SearchResultListProps {
    results: SearchResultType[];
}

export default function SearchResultList({ results }: SearchResultListProps) {
    return (
        <ul className="m-0 list-none p-0">
            {results.map((result) => (
                <li
                    key={result.id}
                    className="mb-6 border-b border-zinc-200 pb-4 dark:border-zinc-700"
                >
                    <SearchResult result={result} />
                </li>
            ))}
        </ul>
    );
}
