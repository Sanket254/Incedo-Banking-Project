import React from 'react';
import { BrowserRouter, Routes, Route, Navigate, useNavigate } from 'react-router-dom';
import AuthPage from './pages/AuthPage';
import CustomerDashboard from './pages/CustomerDashboard';
import AdminDashboard from './pages/AdminDashboard';
import { Landmark, LogOut } from 'lucide-react';
import './App.css';

const Navbar: React.FC = () => {
  const navigate = useNavigate();
  const userStr = localStorage.getItem('user');
  const user = userStr ? JSON.parse(userStr) : null;

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    navigate('/');
  };

  return (
    <nav className="navbar">
      <div className="navbar-brand" style={{ cursor: 'pointer' }} onClick={() => navigate(user?.role === 'Admin' ? '/admin' : '/dashboard')}>
        <Landmark size={24} />
        <span>Incedo Digital Banking</span>
      </div>
      {user && (
        <div className="navbar-user">
          <span>
            {user.firstName} {user.lastName}
          </span>
          <span className="badge">{user.role}</span>
          <button
            className="btn btn-outline"
            style={{ color: '#fff', borderColor: '#475569', padding: '0.4rem 0.8rem' }}
            onClick={handleLogout}
          >
            <LogOut size={16} /> Logout
          </button>
        </div>
      )}
    </nav>
  );
};

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <Routes>
        <Route path="/" element={<AuthPage />} />
        <Route path="/dashboard" element={<CustomerDashboard />} />
        <Route path="/admin" element={<AdminDashboard />} />
        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;