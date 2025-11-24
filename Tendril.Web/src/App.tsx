import { Routes, Route, Navigate } from "react-router-dom";

import { EventsPage } from './pages/EventsPage';
import { ScrapersPage } from "./pages/ScrapersPage";
import { ScraperEditorPage } from "./pages/ScraperEditorPage";
import { VenuesPage } from "./pages/VenuesPage";

export default function App() {
  return (
    <Routes>
      {/* Default redirect */}
      <Route path="/" element={<Navigate to="/events" replace />} />

      {/* Main app pages */}
      <Route path="/events" element={<EventsPage />} />
      <Route path="/scrapers" element={<ScrapersPage />} />
      <Route path="/scrapers/:id" element={<ScraperEditorPage />} />
      <Route path="/venues" element={<VenuesPage />} />

      {/* Catch-all */}
      <Route path="*" element={<Navigate to="/events" replace />} />
    </Routes>
  );
}
