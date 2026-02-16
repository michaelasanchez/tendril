import { Container } from 'react-bootstrap';
import { Route, Routes } from 'react-router-dom';
import { Navbar } from './components/navbar';
import { useBootstrapTheme } from './hooks';
import {
  CategoriesPage,
  EventsPage,
  ScraperEditorPage,
  ScrapersPage,
  TagsPage,
  VenuesPage,
} from './pages';

export default function App() {
  const { theme, toggleTheme } = useBootstrapTheme();

  return (
    <>
      <Navbar theme={theme} onThemeToggle={toggleTheme} />
      <Container>
        <Routes>
          <Route path="/scrapers" element={<ScrapersPage />} />
          <Route path="/scrapers/:scraperId" element={<ScraperEditorPage />} />
          <Route path="/categories" element={<CategoriesPage />} />
          <Route path="/tags" element={<TagsPage />} />
          <Route path="/venues" element={<VenuesPage />} />

          <Route path="*" element={<EventsPage />} />
        </Routes>
      </Container>
    </>
  );
}
