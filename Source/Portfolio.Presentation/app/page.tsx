import { Search } from "@/features/search";

export default function Home() {
  return (
    <div className="flex flex-1 flex-col items-center pt-20">
      <h1 className="mb-8 text-4xl font-bold">Search</h1>
      <Search />
    </div>
  );
}
