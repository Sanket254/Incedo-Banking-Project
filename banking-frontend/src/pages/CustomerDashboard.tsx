import React, { useEffect, useState } from 'react';
import axiosClient from '../api/axiosClient';
import { ArrowDownCircle, ArrowUpCircle, Calculator, PiggyBank } from 'lucide-react';

interface Transaction {
  id: number;
  transactionType: string;
  amount: number;
  balanceAfter: number;
  description: string;
  transactionDate: string;
}

interface FixedDeposit {
  id: number;
  principalAmount: number;
  tenureMonths: number;
  interestRate: number;
  startDate: string;
  maturityDate: string;
  maturityAmount: number;
  status: string;
}

const CustomerDashboard: React.FC = () => {
  const [accountNumber, setAccountNumber] = useState('');
  const [balance, setBalance] = useState<number>(0);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [fds, setFds] = useState<FixedDeposit[]>([]);

  // Deposit / Withdraw state
  const [txAmount, setTxAmount] = useState('');
  const [txDesc, setTxDesc] = useState('');

  // FD Calculator & Open state
  const [fdPrincipal, setFdPrincipal] = useState('5000');
  const [fdTenure, setFdTenure] = useState('12');
  const [fdPreview, setFdPreview] = useState<any>(null);

  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const loadDashboardData = async () => {
    try {
      const [savingsRes, fdRes] = await Promise.all([
        axiosClient.get('/Savings/dashboard'),
        axiosClient.get('/FixedDeposit/my-fds'),
      ]);
      setAccountNumber(savingsRes.data.accountNumber);
      setBalance(savingsRes.data.balance);
      setTransactions(savingsRes.data.recentTransactions);
      setFds(fdRes.data);
    } catch (err: any) {
      setError('Failed to load dashboard data.');
    }
  };

  useEffect(() => {
    loadDashboardData();
  }, []);

  const handleTransaction = async (type: 'deposit' | 'withdraw') => {
    setError('');
    setMessage('');
    try {
      const res = await axiosClient.post(`/Savings/${type}`, {
        amount: Number(txAmount),
        description: txDesc,
      });
      setMessage(res.data.message);
      setTxAmount('');
      setTxDesc('');
      loadDashboardData();
    } catch (err: any) {
      setError(err.response?.data?.message || `Failed to ${type}.`);
    }
  };

  const handlePreviewFd = async () => {
    setError('');
    try {
      const res = await axiosClient.post('/FixedDeposit/calculate', {
        principalAmount: Number(fdPrincipal),
        tenureMonths: Number(fdTenure),
      });
      setFdPreview(res.data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Could not calculate FD preview.');
    }
  };

  const handleOpenFd = async () => {
    setError('');
    setMessage('');
    try {
      const res = await axiosClient.post('/FixedDeposit/open', {
        principalAmount: Number(fdPrincipal),
        tenureMonths: Number(fdTenure),
      });
      setMessage(res.data.message);
      setFdPreview(null);
      loadDashboardData();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to open Fixed Deposit.');
    }
  };

  const handlePrematureWithdraw = async (fdId: number) => {
    if (!window.confirm(`Close FD #${fdId} early? A 1% interest penalty applies.`)) {
      return;
    }
    setError('');
    setMessage('');
    try {
      const res = await axiosClient.post(`/FixedDeposit/${fdId}/premature-withdraw`);
      setMessage(
        `${res.data.message} Total Payout: ₹${res.data.totalPayout.toLocaleString()}`
      );
      loadDashboardData();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Could not close Fixed Deposit.');
    }
  };

  return (
    <div className="container">
      {error && <div className="alert alert-error">{error}</div>}
      {message && <div className="alert alert-success">{message}</div>}

      {/* Top Balance Banner */}
      <div className="stat-card" style={{ marginBottom: '1.5rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap' }}>
          <div>
            <div className="stat-label">Savings Account Balance</div>
            <div className="stat-value">₹{balance.toLocaleString('en-IN', { minimumFractionDigits: 2 })}</div>
          </div>
          <div style={{ textAlign: 'right' }}>
            <div className="stat-label">Account Number</div>
            <div style={{ fontSize: '1.25rem', fontWeight: 600, marginTop: '0.5rem', color: '#38bdf8' }}>
              {accountNumber}
            </div>
          </div>
        </div>
      </div>

      {/* Action Cards: Deposit/Withdraw + Open Fixed Deposit */}
      <div className="grid-2">
        {/* Card 1: Deposit & Withdraw */}
        <div className="card">
          <h3 style={{ marginBottom: '1rem' }}>Quick Deposit / Withdrawal</h3>
          <div className="form-group">
            <label>Amount (₹)</label>
            <input
              type="number"
              min="1"
              value={txAmount}
              onChange={(e) => setTxAmount(e.target.value)}
              placeholder="Enter amount (e.g. 2500)"
            />
          </div>
          <div className="form-group">
            <label>Remarks / Description</label>
            <input
              type="text"
              value={txDesc}
              onChange={(e) => setTxDesc(e.target.value)}
              placeholder="e.g. Monthly Savings / Utility Bill"
            />
          </div>
          <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1rem' }}>
            <button
              className="btn btn-success"
              style={{ flex: 1 }}
              onClick={() => handleTransaction('deposit')}
            >
              <ArrowDownCircle size={18} /> Deposit
            </button>
            <button
              className="btn btn-danger"
              style={{ flex: 1 }}
              onClick={() => handleTransaction('withdraw')}
            >
              <ArrowUpCircle size={18} /> Withdraw
            </button>
          </div>
        </div>

        {/* Card 2: Fixed Deposit Calculator & Instant Booking */}
        <div className="card">
          <h3 style={{ marginBottom: '1rem' }}>Open a New Fixed Deposit</h3>
          <div className="form-group">
            <label>Principal Amount (Min ₹1,000)</label>
            <input
              type="number"
              min="1000"
              value={fdPrincipal}
              onChange={(e) => setFdPrincipal(e.target.value)}
            />
          </div>
          <div className="form-group">
            <label>Tenure Duration</label>
            <select value={fdTenure} onChange={(e) => setFdTenure(e.target.value)}>
              <option value="6">6 Months (6.00% p.a.)</option>
              <option value="12">12 Months / 1 Year (6.75% p.a.)</option>
              <option value="24">24 Months / 2 Years (7.25% p.a.)</option>
              <option value="36">36 Months / 3 Years (7.25% p.a.)</option>
            </select>
          </div>

          {fdPreview && (
            <div className="alert alert-success" style={{ fontSize: '0.85rem' }}>
              <strong>Rate:</strong> {fdPreview.interestRate}% p.a. |{' '}
              <strong>Interest Earned:</strong> ₹{fdPreview.interestEarned} |{' '}
              <strong>Maturity Value:</strong> ₹{fdPreview.maturityAmount}
            </div>
          )}

          <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1rem' }}>
            <button className="btn btn-outline" style={{ flex: 1 }} onClick={handlePreviewFd}>
              <Calculator size={18} /> Calculate Maturity
            </button>
            <button className="btn btn-primary" style={{ flex: 1 }} onClick={handleOpenFd}>
              <PiggyBank size={18} /> Open FD Now
            </button>
          </div>
        </div>
      </div>

      {/* My Fixed Deposits Portfolio Table */}
      <div className="card">
        <h3 style={{ marginBottom: '1rem' }}>My Fixed Deposits ({fds.length})</h3>
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>FD #</th>
                <th>Principal</th>
                <th>Tenure</th>
                <th>Rate</th>
                <th>Maturity Value</th>
                <th>Maturity Date</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {fds.length === 0 ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', color: '#64748b' }}>
                    No Fixed Deposits opened yet.
                  </td>
                </tr>
              ) : (
                fds.map((fd) => (
                  <tr key={fd.id}>
                    <td>#{fd.id}</td>
                    <td>₹{fd.principalAmount.toLocaleString()}</td>
                    <td>{fd.tenureMonths} mos</td>
                    <td>{fd.interestRate}%</td>
                    <td style={{ fontWeight: 600 }}>₹{fd.maturityAmount.toLocaleString()}</td>
                    <td>{new Date(fd.maturityDate).toLocaleDateString()}</td>
                    <td>
                      <span className="badge">{fd.status}</span>
                    </td>
                    <td>
                      {fd.status === 'Active' && (
                        <button
                          className="btn btn-danger"
                          style={{ padding: '0.35rem 0.7rem', fontSize: '0.8rem' }}
                          onClick={() => handlePrematureWithdraw(fd.id)}
                        >
                          Break FD Early
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Recent Transactions Table */}
      <div className="card">
        <h3 style={{ marginBottom: '1rem' }}>Recent Account Transactions</h3>
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Type</th>
                <th>Description</th>
                <th>Amount</th>
                <th>Balance After</th>
              </tr>
            </thead>
            <tbody>
              {transactions.length === 0 ? (
                <tr>
                  <td colSpan={5} style={{ textAlign: 'center', color: '#64748b' }}>
                    No transactions recorded yet.
                  </td>
                </tr>
              ) : (
                transactions.map((tx) => (
                  <tr key={tx.id}>
                    <td>{new Date(tx.transactionDate).toLocaleString()}</td>
                    <td>
                      <span className="badge">{tx.transactionType}</span>
                    </td>
                    <td>{tx.description}</td>
                    <td
                      style={{
                        fontWeight: 600,
                        color:
                          tx.transactionType === 'Withdrawal' || tx.transactionType === 'FD_Open'
                            ? '#dc2626'
                            : '#059669',
                      }}
                    >
                      {tx.transactionType === 'Withdrawal' || tx.transactionType === 'FD_Open'
                        ? `-₹${tx.amount.toLocaleString()}`
                        : `+₹${tx.amount.toLocaleString()}`}
                    </td>
                    <td>₹{tx.balanceAfter.toLocaleString()}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default CustomerDashboard;