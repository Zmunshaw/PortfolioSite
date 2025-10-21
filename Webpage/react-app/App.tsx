// src/App.tsx
import "./App.css";
import NavBar from "./ui/shared/NavBar.tsx";
import {FaTh, FaUserCircle, FaGithub} from 'react-icons/fa';
import {Route, Routes} from "react-router";
import PageHome from "./pages/Home.tsx";
import PageProjects from "./pages/Projects.tsx";
import PageBlog from "./pages/Blog.tsx";
import PageMe from "./pages/Me.tsx";
import APISearchEngine from "./api/SearchEngineAPI.tsx";

function App() {
    const links = [
        { label: 'Home', url: '/' },
        { label: 'Projects', url: '/projects' },
        { label: 'Blog', url: '/blog' },
        { label: 'Me', url: '/me' },
    ];

    const icons = [FaTh, FaGithub, FaUserCircle];

  return (
    <>
        <NavBar title="Dev" links={links} icons={icons} />
        <Routes>
            <Route path="/" element={<PageHome />} />
            <Route path="/projects" element={<PageProjects />} />
            <Route path="/blog" element={<PageBlog />} />
            <Route path="/me" element={<PageMe />} />
            <Route path="/api/searchengine/" element={<APISearchEngine />} />
        </Routes>
    </>
  );
}

export default App;
