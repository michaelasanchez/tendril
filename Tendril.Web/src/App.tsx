const CLIENT_ID = import.meta.env.VITE_AUTH_CLIENT_ID;

import { Container } from 'react-bootstrap';
import { Navigate, Outlet, Route, Routes, useNavigate } from 'react-router';
import { SquareButton } from './components/button';
import { Navbar } from './components/navbar';
import { useBootstrapTheme } from './hooks';
import { useAuth } from './hooks/useAuth';
import {
  CategoriesPage,
  EventsPage,
  ScraperEditorPage,
  ScrapersPage,
  SummaryPage,
  TagsPage,
  VenuesPage,
} from './pages';

function AdminLayout({
  authorized,
  loading,
}: {
  authorized: boolean;
  loading: boolean;
}) {
  if (loading) return null; // or a spinner
  if (!authorized) return <Navigate to="/" replace />;

  return <Outlet />;
}

function LoginLayout({
  loading,
  user,
  login,
}: {
  loading: boolean;
  user: any;
  login: () => void;
}) {
  if (loading) return null;
  if (user) return <Navigate to="/admin/scrapers" replace />;

  return (
    <div
      style={{
        height: '30em',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      {!user && <SquareButton onClick={login}>Login</SquareButton>}
    </div>
  );
}

export default function App() {
  const { theme, toggleTheme } = useBootstrapTheme();
  const { user, loading, login, logout } = useAuth(CLIENT_ID);

  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <>
      <Navbar
        theme={theme}
        onLogout={handleLogout}
        onThemeToggle={toggleTheme}
        authorized={!!user}
      />
      <Container>
        <Routes>
          <Route
            path="/login"
            element={
              <LoginLayout loading={loading} user={user} login={login} />
            }
          />
          <Route
            path="/admin"
            element={<AdminLayout authorized={!!user} loading={loading} />}
          >
            <Route index element={<Navigate to="/admin/scrapers" replace />} />
            <Route path="scrapers" element={<ScrapersPage />} />
            <Route
              path="scrapers/:scraperId/:tabId?"
              element={<ScraperEditorPage />}
            />
            <Route path="categories" element={<CategoriesPage />} />
            <Route path="tags" element={<TagsPage />} />
            <Route path="venues" element={<VenuesPage />} />
            <Route path="summary" element={<SummaryPage />} />
          </Route>

          <Route path="*" element={<EventsPage />} />
        </Routes>
      </Container>
    </>
  );
}
