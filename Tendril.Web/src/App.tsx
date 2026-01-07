import { Container } from 'react-bootstrap';
import { Navigate, Route, Routes } from 'react-router-dom';
import { EventsPage } from './pages/EventsPage';
import { ScraperEditorPage } from './pages/ScraperEditorPage';
import { ScrapersPage } from './pages/ScrapersPage';
import { VenuesPage } from './pages/VenuesPage';
import { useBootstrapTheme } from './hooks';

export default function App() {
  useBootstrapTheme('light');

  return (
    <Container>
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
    </Container>
  );
}
