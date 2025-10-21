import React from "react";
import {SearchResultType} from "../../../Types/SearchTypes.tsx";

interface SearchResultProps {
    result: SearchResultType;
}

const SearchResult: React.FC<SearchResultProps> = ({ result } ) => {
    return (
        <div className="search-result" key={result.id}>
            <a href={result.url} target="_blank" rel="noopener noreferrer">
                {result.title}
            </a>
            <div >
                {result.displayUrl || result.url}
            </div>
            <p >{result.snippet}</p>
            {result.lastUpdated && (
                <div>
                    Last updated: {new Date(result.lastUpdated).toLocaleDateString()}
                </div>
            )}
        </div>
    );
};

export default SearchResult;