const CLIENT_ID = import.meta.env.VITE_AUTH_CLIENT_ID;

import { lazy, Suspense } from 'react';
import { Container, Spinner } from 'react-bootstrap';
import { Navigate, Outlet, Route, Routes, useNavigate } from 'react-router';
import { SquareButton } from './components/button';
import { Navbar } from './components/navbar';
import { useBootstrapTheme } from './hooks';
import { useAuth } from './hooks/useAuth';
import { EventsPage } from './pages';

const AttemptHistoryPage = lazy(() => import('./pages/admin/AttemptHistoryPage'));
const AutomatePage = lazy(() => import('./pages/admin/AutomatePage'));
const CategoriesPage = lazy(() => import('./pages/admin/CategoriesPage'));
const ReviewPage = lazy(() => import('./pages/admin/ReviewPage'));
const ScraperEditorPage = lazy(() => import('./pages/admin/ScraperEditorPage'));
const ScrapersPage = lazy(() => import('./pages/admin/ScrapersPage'));
const TagsPage = lazy(() => import('./pages/admin/TagsPage'));
const VenuesPage = lazy(() => import('./pages/admin/VenuesPage'));

function AdminLayout({
  authorized,
  loading,
}: {
  authorized: boolean;
  loading: boolean;
}) {
  if (loading) return <Spinner animation="border" />;
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

// analytics.ts?
export const trackEvent = async (eventId: string, metadata = {}) => {
  try {
    await fetch('/api/track-click', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        event_id: eventId,
        path: window.location.pathname,
        metadata: metadata,
        timestamp: new Date().toISOString(),
      }),
    });
  } catch (err) {
    console.error('Tracking failed', err); // Fail silently so you don't break the UI
  }
};

export default function App() {
  const { theme, toggleTheme } = useBootstrapTheme();
  const { user, loading, login, logout } = useAuth(CLIENT_ID);

  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  // const handleCapture = (e) => {
  //   const trackTarget = e.target.closest('[data-track]');
  //   if (trackTarget) {
  //     const { track, ...metadata } = trackTarget.dataset;
  //     trackEvent(track, metadata);
  //   }
  // };

  return (
    <>
      <Navbar
        theme={theme}
        onLogout={handleLogout}
        onThemeToggle={toggleTheme}
        authorized={!!user}
      />
      <Container
      //onClickCapture={handleCapture}
      >
        <Suspense fallback={<Spinner animation="border" />}>
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
              <Route
                index
                element={<Navigate to="/admin/scrapers" replace />}
              />
              <Route path="scrapers" element={<ScrapersPage />} />
              <Route
                path="scrapers/:scraperId/:tabId?"
                element={<ScraperEditorPage />}
              />
              <Route path="categories" element={<CategoriesPage />} />
              <Route path="tags" element={<TagsPage />} />
              <Route path="venues" element={<VenuesPage />} />
              <Route path="automate" element={<AutomatePage />} />
              <Route path="review" element={<ReviewPage />} />
              <Route path="summary" element={
            <AttemptHistoryPage />} />
            </Route>

            <Route path="*" element={<EventsPage />} />
          </Routes>
        </Suspense>
      </Container>
    </>
  );
}
