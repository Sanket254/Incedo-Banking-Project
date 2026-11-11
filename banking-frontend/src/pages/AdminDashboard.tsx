import React, { useEffect, useState } from 'react';
import axiosClient from '../api/axiosClient';
import { 
  Users, 
  Wallet, 
  PiggyBank, 
  TrendingUp, 
  PlayCircle, 
  CheckCircle2, 
} from 'lucide-react';

interface SummaryData {
  totalCustomers: number;
  totalSavingsBalance: number;
  activeFdCount: number;
  activeFdPrincipal: number;
  totalInterestPaid: number;
}

interface CustomerRecord {
  customerId: number;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  accountNumber: string;
  savingsBalance: number;
  createdAt: string;
}

interface FdRecord {
  fdId: number;
  customerId: number;
  customerName: string;
  customerEmail: string;
  principalAmount: number;
  tenureMonths: number;
  interestRate: number;
  startDate: string;
  maturityDate: string;
  maturityAmount: number;
  status: string;
  closedDate?: string;
}

const AdminDashboard: React.FC = () => {
  const [summary, setSummary] = useState<SummaryData | null>(null);
  const [customers, setCustomers] = useState<CustomerRecord[]>([]);
  const [fds, setFds] = useState<FdRecord[]>([]);
  
  // Batch processing state
  const currentMonthStr = new Date().toISOString().substring(0, 7); // e.g., "2026-11"
  const [periodMonth, setPeriodMonth] = useState(currentMonthStr);
  const [isProcessing, setIsProcessing] = useState(false);
  const [batchResult, setBatchResult] = useState<any>(null);

  const [error, setError] = useState('');
  const [activeTab, setActiveTab] = useState<'customers' | 'fds'>('customers');

  const fetchAdminData = async () => {
    try {
      const [sumRes, custRes, fdRes] = await Promise.all([
        axiosClient.get('/Admin/summary'),
        axiosClient.get('/Admin/customers'),
        axiosClient.get('/Admin/fixed-deposits'),
      ]);
      setSummary(sumRes.data);
      setCustomers(custRes.data);
      setFds(fdRes.data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Access denied or failed to load Admin metrics.');
    }
  };

  useEffect(() => {
    fetchAdminData();
  }, []);

  const handleProcessInterest = async () => {
    if (!window.confirm(`Trigger monthly interest accrual for period "${periodMonth}"?`)) {
      return;
    }
    setError('');
    setBatchResult(null);
    setIsProcessing(true);

    try {
      const res = await axiosClient.post(`/Admin/process-interest?periodMonth=${periodMonth}`);
      setBatchResult(res.data);
      fetchAdminData();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Batch interest execution failed.');
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <div className="container">
      <div style={{ marginBottom: '1.5rem' }}>
        <h2>Bank Administration & Reporting Portal</h2>
        <p style={{ color: '#64748b', fontSize: '0.9rem' }}>
          Real-time institutional liquidity, account balances, and automated interest operations
        </p>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {/* Top 4 KPI Metric Cards */}
      {summary && (
        <div className="grid-4">
          <div className="card" style={{ borderLeft: '4px solid #2563eb' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div className="stat-label">Total Customers</div>
              <Users size={20} color="#2563eb" />
            </div>
            <div className="stat-value" style={{ color: '#0f172a' }}>{summary.totalCustomers}</div>
          </div>

          <div className="card" style={{ borderLeft: '4px solid #059669' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div className="stat-label">Total Savings Liquidity</div>
              <Wallet size={20} color="#059669" />
            </div>
            <div className="stat-value" style={{ color: '#0f172a' }}>
              ₹{summary.totalSavingsBalance.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </div>
          </div>

          <div className="card" style={{ borderLeft: '4px solid #d97706' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div className="stat-label">Active FDs ({summary.activeFdCount})</div>
              <PiggyBank size={20} color="#d97706" />
            </div>
            <div className="stat-value" style={{ color: '#0f172a' }}>
              ₹{summary.activeFdPrincipal.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </div>
          </div>

          <div className="card" style={{ borderLeft: '4px solid #7c3aed' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div className="stat-label">Total Interest Disbursed</div>
              <TrendingUp size={20} color="#7c3aed" />
            </div>
            <div className="stat-value" style={{ color: '#0f172a' }}>
              ₹{summary.totalInterestPaid.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
            </div>
          </div>
        </div>
      )}

      {/* Monthly Interest Accrual Batch Engine Box */}
      <div className="card" style={{ background: '#f8fafc', borderColor: '#cbd5e1' }}>
        <h3 style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.75rem' }}>
          <PlayCircle size={22} color="#2563eb" /> Batch Monthly Interest Accrual Runner
        </h3>
        <p style={{ color: '#64748b', fontSize: '0.875rem', marginBottom: '1rem' }}>
          Computes 1 month of interest for all active FDs, applies idempotency checks, credits recipient savings accounts, and automates term maturity.
        </p>

        <div style={{ display: 'flex', gap: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <div style={{ minWidth: '220px' }}>
            <label style={{ display: 'block', fontSize: '0.8rem', fontWeight: 600, marginBottom: '0.25rem' }}>
              Target Period (YYYY-MM)
            </label>
            <input
              type="month"
              value={periodMonth}
              onChange={(e) => setPeriodMonth(e.target.value)}
              style={{ padding: '0.55rem 0.8rem', borderRadius: '8px', border: '1px solid #cbd5e1', width: '100%' }}
            />
          </div>

          <button
            className="btn btn-primary"
            style={{ marginTop: '1.25rem' }}
            disabled={isProcessing}
            onClick={handleProcessInterest}
          >
            {isProcessing ? 'Processing Batch...' : 'Run Accrual Batch'}
          </button>
        </div>

        {batchResult && (
          <div className="alert alert-success" style={{ marginTop: '1rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontWeight: 600 }}>
              <CheckCircle2 size={18} /> {batchResult.message}
            </div>
            <div style={{ marginTop: '0.4rem', fontSize: '0.85rem' }}>
              Processed: <strong>{batchResult.processedCount}</strong> | 
              Skipped (Already Disbursed): <strong>{batchResult.skippedAlreadyProcessed}</strong> | 
              Matured Term FDs: <strong>{batchResult.maturedFdsCount}</strong> | 
              Total Credited: <strong>₹{batchResult.totalInterestCredited.toFixed(2)}</strong>
            </div>
          </div>
        )}
      </div>

      {/* Tab Selectors */}
      <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem' }}>
        <button
          className={`btn ${activeTab === 'customers' ? 'btn-primary' : 'btn-outline'}`}
          onClick={() => setActiveTab('customers')}
        >
          Customer Portfolio ({customers.length})
        </button>
        <button
          className={`btn ${activeTab === 'fds' ? 'btn-primary' : 'btn-outline'}`}
          onClick={() => setActiveTab('fds')}
        >
          Institutional Fixed Deposits ({fds.length})
        </button>
      </div>

      {/* Tab 1: Customers Table */}
      {activeTab === 'customers' && (
        <div className="card">
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Customer ID</th>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Role</th>
                  <th>Account Number</th>
                  <th>Savings Balance</th>
                  <th>Joined Date</th>
                </tr>
              </thead>
              <tbody>
                {customers.map((c) => (
                  <tr key={c.customerId}>
                    <td>#{c.customerId}</td>
                    <td style={{ fontWeight: 600 }}>{c.firstName} {c.lastName}</td>
                    <td>{c.email}</td>
                    <td><span className="badge">{c.role}</span></td>
                    <td style={{ fontFamily: 'monospace' }}>{c.accountNumber}</td>
                    <td style={{ fontWeight: 600, color: '#059669' }}>
                      ₹{c.savingsBalance.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                    </td>
                    <td>{new Date(c.createdAt).toLocaleDateString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 2: Fixed Deposits Table */}
      {activeTab === 'fds' && (
        <div className="card">
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>FD #</th>
                  <th>Client</th>
                  <th>Principal</th>
                  <th>Tenure</th>
                  <th>Rate</th>
                  <th>Maturity Amount</th>
                  <th>Maturity Date</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {fds.map((f) => (
                  <tr key={f.fdId}>
                    <td>#{f.fdId}</td>
                    <td>
                      <div style={{ fontWeight: 600 }}>{f.customerName}</div>
                      <div style={{ fontSize: '0.75rem', color: '#64748b' }}>{f.customerEmail}</div>
                    </td>
                    <td>₹{f.principalAmount.toLocaleString()}</td>
                    <td>{f.tenureMonths} mos</td>
                    <td>{f.interestRate}%</td>
                    <td style={{ fontWeight: 600 }}>₹{f.maturityAmount.toLocaleString()}</td>
                    <td>{new Date(f.maturityDate).toLocaleDateString()}</td>
                    <td>
                      <span
                        className="badge"
                        style={{
                          borderColor: f.status === 'Active' ? '#059669' : f.status === 'Matured' ? '#2563eb' : '#dc2626',
                          color: f.status === 'Active' ? '#059669' : f.status === 'Matured' ? '#2563eb' : '#dc2626',
                        }}
                      >
                        {f.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminDashboard;