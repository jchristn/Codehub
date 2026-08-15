import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import { ToastProvider } from './context/ToastContext';
import Login from './components/Login';
import Dashboard from './components/Dashboard';
import NotFound from './views/NotFound';
import './App.css';

/**
 * Route wrapper that requires authentication.
 */
function PrivateRoute({ children }) {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) {
    return (
      <div className="app-loading">
        <div className="loading-spinner" />
        <p>Verifying session...</p>
      </div>
    );
  }
  return isAuthenticated ? children : <Navigate to="/" replace />;
}

/**
 * Route wrapper for public pages (login). Redirects authed users inward.
 */
function PublicRoute({ children }) {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) {
    return (
      <div className="app-loading">
        <div className="loading-spinner" />
        <p>Verifying session...</p>
      </div>
    );
  }
  return !isAuthenticated ? children : <Navigate to="/home" replace />;
}

function AppRoutes() {
  return (
    <Routes>
      <Route
        path="/"
        element={
          <PublicRoute>
            <Login />
          </PublicRoute>
        }
      />
      <Route
        path="/:section"
        element={
          <PrivateRoute>
            <Dashboard />
          </PrivateRoute>
        }
      />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}

function App() {
  return (
    <AuthProvider>
      <ToastProvider>
        <BrowserRouter basename="/dashboard">
          <AppRoutes />
        </BrowserRouter>
      </ToastProvider>
    </AuthProvider>
  );
}

export default App;
