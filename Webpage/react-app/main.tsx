import { StrictMode } from "react";
import { createRoot } from "react-dom/client"; // ✅ CORRECT for React 18+
import { BrowserRouter } from "react-router-dom";
import App from "./App.tsx";
import "./index.css";

const root = document.getElementById("root");

if (root) {
    createRoot(root).render(
        <StrictMode>
            <BrowserRouter>
                <App />
            </BrowserRouter>
        </StrictMode>
    );
}
