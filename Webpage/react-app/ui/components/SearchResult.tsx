import React from "react";
import {SearchResultType} from "../../../Types/SearchTypes.tsx";

interface SearchResultProps {
    result: SearchResultType;
}

const SearchResult: React.FC<SearchResultProps> = ({result}) => {
    const fullUrl = result.url.startsWith('http://') || result.url.startsWith('https://')
        ? result.url
        : 'https://' + result.url;

    const handleLinkClick = (e: React.MouseEvent) => {
        e.preventDefault();
        window.open(fullUrl, '_blank', 'noopener,noreferrer');
    };

    return (
        <div className="search-result" key={result.id}>
            <a
                href={result.url}
                onClick={handleLinkClick}
                className="cursor-pointer text-blue-600 hover:underline"
            >
                {result.title}
            </a>
            <div>
                {result.displayUrl || result.url}
            </div>
            <p>{result.snippet}</p>
            {result.lastUpdated && (
                <div>
                    Last updated: {new Date(result.lastUpdated).toLocaleDateString()}
                </div>
            )}
        </div>
    );
};

export default SearchResult;