import { Container } from 'react-bootstrap';
import { Navigate, Route, Routes } from 'react-router-dom';
import { Navbar } from './components/navbar';
import { useBootstrapTheme } from './hooks';
import { EventsPage } from './pages/EventsPage';
import { ScraperEditorPage } from './pages/ScraperEditorPage';
import { ScrapersPage } from './pages/ScrapersPage';
import { VenuesPage } from './pages/VenuesPage';

export default function App() {
  const { theme, toggleTheme } = useBootstrapTheme();

  return (
    <>
      <Navbar theme={theme} onThemeToggle={toggleTheme} />
      <Container>
        <Routes>
          <Route path="/" element={<EventsPage />} />
          <Route path="/scrapers" element={<ScrapersPage />} />
          <Route path="/scrapers/:scraperId" element={<ScraperEditorPage />} />
          <Route path="/venues" element={<VenuesPage />} />

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Container>
    </>
  );
}
