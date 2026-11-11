import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import { Landmark, LogIn, UserPlus } from 'lucide-react';

const AuthPage: React.FC = () => {
  const [isLogin, setIsLogin] = useState(true);
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState('Customer');
  const [error, setError] = useState('');
  const [successMsg, setSuccessMsg] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccessMsg('');

    try {
      if (isLogin) {
        const res = await axiosClient.post('/Auth/login', { email, password });
        localStorage.setItem('token', res.data.token);
        localStorage.setItem('user', JSON.stringify(res.data.customer));

        if (res.data.customer.role === 'Admin') {
          navigate('/admin');
        } else {
          navigate('/dashboard');
        }
      } else {
        const res = await axiosClient.post('/Auth/register', {
          firstName,
          lastName,
          email,
          password,
          role,
        });
        setSuccessMsg(
          `Registered! Account No: ${res.data.accountNumber}. Please sign in.`
        );
        setIsLogin(true);
      }
    } catch (err: any) {
      setError(
        err.response?.data?.message || 'Authentication failed. Check credentials.'
      );
    }
  };

  return (
    <div className="auth-wrapper">
      <div className="card" style={{ width: '100%', maxWidth: '430px' }}>
        <div style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
          <Landmark size={40} color="#2563eb" />
          <h2 style={{ marginTop: '0.5rem' }}>Incedo Digital Banking</h2>
          <p style={{ color: '#64748b', fontSize: '0.9rem' }}>
            {isLogin
              ? 'Sign in to your Savings & Fixed Deposit Portal'
              : 'Open an instant digital savings account'}
          </p>
        </div>

        {error && <div className="alert alert-error">{error}</div>}
        {successMsg && <div className="alert alert-success">{successMsg}</div>}

        <form onSubmit={handleSubmit}>
          {!isLogin && (
            <>
              <div className="form-group">
                <label>First Name</label>
                <input
                  type="text"
                  required
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder="Sanket"
                />
              </div>
              <div className="form-group">
                <label>Last Name</label>
                <input
                  type="text"
                  required
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder="Kashyap"
                />
              </div>
            </>
          )}

          <div className="form-group">
            <label>Email Address</label>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="sanket@incedo.com"
            />
          </div>

          <div className="form-group">
            <label>Password</label>
            <input
              type="password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
            />
          </div>

          {!isLogin && (
            <div className="form-group">
              <label>Account Role</label>
              <select value={role} onChange={(e) => setRole(e.target.value)}>
                <option value="Customer">Customer (Savings & FD)</option>
                <option value="Admin">Bank Admin</option>
              </select>
            </div>
          )}

          <button
            type="submit"
            className="btn btn-primary"
            style={{ width: '100%', marginTop: '0.5rem' }}
          >
            {isLogin ? (
              <>
                <LogIn size={18} /> Sign In
              </>
            ) : (
              <>
                <UserPlus size={18} /> Create Account
              </>
            )}
          </button>
        </form>

        <div style={{ textAlign: 'center', marginTop: '1.25rem' }}>
          <button
            type="button"
            className="btn btn-outline"
            style={{ width: '100%' }}
            onClick={() => {
              setIsLogin(!isLogin);
              setError('');
              setSuccessMsg('');
            }}
          >
            {isLogin
              ? "Don't have an account? Register Now"
              : 'Already registered? Sign In'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default AuthPage;