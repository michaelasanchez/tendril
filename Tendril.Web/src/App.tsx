const CLIENT_ID = import.meta.env.VITE_AUTH_CLIENT_ID;

import { Container } from 'react-bootstrap';
import { Route, Routes } from 'react-router';
import { Navbar } from './components/navbar';
import { useBootstrapTheme } from './hooks';
import { useAuth } from './hooks/useAuth';
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

  const { user, loading, login, logout } = useAuth(CLIENT_ID);

  return (
    <>
      <Navbar
        theme={theme}
        onLogin={login}
        onLogout={logout}
        onThemeToggle={toggleTheme}
        authorized={!!user}
      />
      <Container>
        <Routes>
          <Route
            path="/scrapers"
            element={<ScrapersPage authLoading={loading} authorized={!!user} />}
          />
          <Route
            path="/scrapers/:scraperId/:tabId?"
            element={
              <ScraperEditorPage authLoading={loading} authorized={!!user} />
            }
          />
          <Route path="/categories" element={<CategoriesPage />} />
          <Route path="/tags" element={<TagsPage />} />
          <Route path="/venues" element={<VenuesPage />} />

          {/* <Route path="/test" element={<TestPage />} /> */}

          <Route path="*" element={<EventsPage />} />
        </Routes>
      </Container>
    </>
  );
}
