import { Routes, Route, Navigate } from "react-router-dom";

import { EventsPage } from './pages/EventsPage';
import { ScrapersPage } from "./pages/ScrapersPage";
import { ScraperEditorPage } from "./pages/ScraperEditorPage";
import { VenuesPage } from "./pages/VenuesPage";
import { useBootstrapTheme } from "./hooks/useBootstrapTheme";

export default function App() {
  useBootstrapTheme();
  
  return (
    <Routes>
      {/* Default redirect */}
      <Route path="/" element={<Navigate to="/events" replace />} />

      {/* Main app pages */}
      <Route path="/events" element={<EventsPage />} />
      <Route path="/scrapers" element={<ScrapersPage />} />
      <Route path="/scrapers/:scraperId" element={<ScraperEditorPage />} />
      <Route path="/venues" element={<VenuesPage />} />

      {/* Catch-all */}
      <Route path="*" element={<Navigate to="/events" replace />} />
    </Routes>
  );
}
