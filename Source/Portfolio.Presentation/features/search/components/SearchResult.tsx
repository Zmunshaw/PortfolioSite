import { SearchResultType } from "../types";

interface SearchResultProps {
    result: SearchResultType;
}

export default function SearchResult({ result }: SearchResultProps) {
    return (
        <div>
            <a
                href={result.url}
                target="_blank"
                rel="noopener noreferrer"
                className="text-lg text-[#1a0dab] no-underline hover:underline dark:text-blue-400"
            >
                {result.title}
            </a>
            <div className="text-sm text-[#006621] dark:text-green-400">
                {result.displayUrl ?? result.url}
            </div>
            <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
                {result.snippet}
            </p>
            {result.lastUpdated && (
                <div className="mt-1 text-xs text-zinc-400">
                    Last updated: {new Date(result.lastUpdated).toLocaleDateString()}
                </div>
            )}
        </div>
    );
}
