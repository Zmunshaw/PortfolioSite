import SearchBar from "./SearchBar.tsx";
import SearchResultContainer from "./SearchResultContainer.tsx";
import {SearchResultType} from "../../../Types/SearchTypes.tsx";
import {useState} from "react";
import search from './search.module.css'

const Search: React.FC = () => {
    const [results, setResults] = useState<SearchResultType[]>([]);
    const [error, setError] = useState<string>('');
    const [isLoading, setIsLoading] = useState<boolean>(false);

    const handleSearch = (query: string) => {
        const lowerCaseQuery = query.toLowerCase();
        setIsLoading(true);
        setError('');
        submitQuery(lowerCaseQuery);
    };

    return (
        <div style={search}>
            <SearchBar onSearch={handleSearch} />
            {isLoading && <p>Loading...</p>}
            {error && <p style={{ color: 'red' }}>{error}</p>}
            <SearchResultContainer results={results} />
        </div>
    );

    async function submitQuery(query: string) {
        try {
            const response = await fetch(`http://localhost:1234/search?q=${encodeURIComponent(query)}`);
            if (!response.ok) {
                throw new Error('Failed to fetch search results');
            }

            const data = await response.json();

            const formattedResults: SearchResultType[] = data.results.map((item: SearchResultType) => ({
                id: item.id,
                title: item.title,
                url: item.url,
                snippet: item.snippet,
            }));

            setResults(formattedResults);
        } catch (err: unknown) {
            if (err instanceof Error) {
                setError(err.message);
            } else {
                setError('Something went wrong');
            }
        }
        finally {
            setIsLoading(false);
        }
    }
};

export default Search;