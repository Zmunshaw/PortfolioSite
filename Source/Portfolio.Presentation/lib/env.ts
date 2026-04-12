export const env = {
    SEARCH_API_URL: process.env.NEXT_PUBLIC_SEARCH_API_URL ?? "http://localhost:1234",
} as const;
