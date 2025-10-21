// SearchBar.tsx
import React, { useState } from 'react';
import {SearchBarProps} from "../../../Types/SearchTypes.tsx";

const SearchBar: React.FC<SearchBarProps> = ({ onSearch }) => {
    const [searchQuery, setSearchQuery] = useState<string>('');

    const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setSearchQuery(event.target.value);
        onSearch(event.target.value);
    };

    return (
        <div>
            <input
                type="text"
                placeholder="Search..."
                value={searchQuery}
                onChange={handleChange}
            />
        </div>
    );
};

export default SearchBar;