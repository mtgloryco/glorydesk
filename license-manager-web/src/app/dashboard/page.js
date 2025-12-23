'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  Shield, Plus, LogOut, Clock, Layers, Monitor,
  Copy, CheckCircle2, Upload, HelpCircle, Info, Image as ImageIcon
} from 'lucide-react';

export default function UserDashboard() {
  const [licenses, setLicenses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [requesting, setRequesting] = useState(false);
  const [hardwareId, setHardwareId] = useState('');
  const [selectedPlan, setSelectedPlan] = useState('freemium');
  const [copiedKey, setCopiedKey] = useState(null);
  const router = useRouter();

  useEffect(() => {
    fetchLicenses();
  }, []);

  const fetchLicenses = async () => {
    const token = localStorage.getItem('token');
    if (!token) { router.push('/login'); return; }

    try {
      const res = await fetch('/api/licenses', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (res.status === 401) { router.push('/login'); return; }
      const data = await res.json();
      setLicenses(data);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  };

  const handleRequestLicense = async () => {
    if (!hardwareId) { alert('Please enter your Hardware ID.'); return; }

    const token = localStorage.getItem('token');
    setRequesting(true);
    try {
      const res = await fetch('/api/licenses', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ planType: selectedPlan, hardwareId })
      });
      const data = await res.json();
      if (!res.ok) alert(data.error);
      else {
        setHardwareId('');
        fetchLicenses();
      }
    } catch (err) { alert('Request failed'); }
    finally { setRequesting(false); }
  };

  const handleUploadProof = async (licenseId, file) => {
    if (!file) return;

    // Convert image to base64
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = async () => {
      const base64 = reader.result;
      const token = localStorage.getItem('token');
      try {
        const res = await fetch('/api/licenses', {
          method: 'PATCH',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({ id: licenseId, proofImage: base64 })
        });
        if (res.ok) fetchLicenses();
        else alert('Upload failed');
      } catch (e) { alert('Error uploading proof'); }
    };
  };

  const copyToClipboard = (key) => {
    if (!key) return;
    navigator.clipboard.writeText(key);
    setCopiedKey(key);
    setTimeout(() => setCopiedKey(null), 2000);
  };

  const handleLogout = () => {
    localStorage.clear();
    router.push('/login');
  };

  return (
    <div style={{ background: '#f7fafc', minHeight: '100vh' }}>
      <nav style={{ background: '#fff', borderBottom: '1px solid #eee', position: 'sticky', top: 0, zIndex: 10 }}>
        <div className="container" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '1rem 1.5rem' }}>
          <Link href="/" style={{ textDecoration: 'none', color: 'inherit' }}>
            <div style={{ fontWeight: 'bold', fontSize: '1.2rem', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Shield color="var(--primary)" /> <span>IMS Hub</span>
            </div>
          </Link>
          <div style={{ display: 'flex', gap: '1.5rem', alignItems: 'center' }}>
            <button onClick={handleLogout} className="btn" style={{ fontSize: '0.9rem', color: '#666' }}><LogOut size={18} /> Logout</button>
          </div>
        </div>
      </nav>

      <div className="container" style={{ padding: '3rem 1.5rem' }}>
        <header style={{ marginBottom: '3rem' }}>
          <h1 className="title">License Manager</h1>
          <p style={{ color: '#666' }}>Request, activate, and download your IMS Professional signatures.</p>
        </header>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '2rem', marginBottom: '3rem' }}>
          {/* Activation Tool */}
          <div className="glass-card" style={{ background: '#fff' }}>
            <h3 style={{ marginBottom: '1.5rem', display: 'flex', alignItems: 'center', gap: '10px' }}><Plus size={20} color="var(--primary)" /> New Request</h3>

            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.8rem', fontWeight: 700, color: '#444', marginBottom: '0.5rem' }}>SELECT PLAN</label>
              <select
                value={selectedPlan}
                onChange={(e) => setSelectedPlan(e.target.value)}
                style={{ width: '100%', padding: '12px', borderRadius: '10px', border: '1px solid #eee', background: '#f9f9f9', fontSize: '0.95rem' }}
              >
                <option value="freemium">Freemium Starter ($1.5)</option>
                <option value="1-month">Professional Monthly ($4.2)</option>
                <option value="1-year">Enterprise Yearly ($42)</option>
              </select>
            </div>

            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.8rem', fontWeight: 700, color: '#444', marginBottom: '0.5rem' }}>HARDWARE ID</label>
              <div style={{ position: 'relative' }}>
                <Monitor size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: '#888' }} />
                <input
                  type="text"
                  value={hardwareId}
                  onChange={(e) => setHardwareId(e.target.value)}
                  placeholder="Paste from Desktop App"
                  style={{ width: '100%', padding: '12px 12px 12px 40px', borderRadius: '10px', border: '1px solid #eee', fontSize: '0.9rem' }}
                />
              </div>
            </div>

            <button onClick={handleRequestLicense} className="btn btn-primary" style={{ width: '100%', padding: '1rem' }} disabled={requesting || !hardwareId}>
              {requesting ? 'Processing...' : 'Submit Request'}
            </button>
          </div>

          {/* Guidelines */}
          <div className="glass-card" style={{ background: 'linear-gradient(135deg, #1a202c, #2d3748)', color: '#fff' }}>
            <h3 style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '10px' }}><Info size={20} color="var(--primary)" /> Workflow</h3>
            <ol style={{ paddingLeft: '1.2rem', fontSize: '0.9rem', display: 'grid', gap: '1rem', color: '#cbd5e0' }}>
              <li>Generate <strong>Hardware ID</strong> in the desktop app.</li>
              <li>Submit a new request here for your device.</li>
              <li>Upload your <strong>Payment Proof</strong> (screenshot).</li>
              <li>Wait for Admin approval (approx. 1-12 hours).</li>
              <li>Copy the <strong>Signed Key</strong> into your desktop app.</li>
            </ol>
          </div>
        </div>

        {/* Repos */}
        <section className="glass-card" style={{ background: '#fff' }}>
          <h3 style={{ marginBottom: '2rem', display: 'flex', alignItems: 'center', gap: '10px' }}><Layers size={20} color="var(--primary)" /> Activation Registry</h3>
          <div className="table-container">
            <table>
              <thead>
                <tr>
                  <th>Request ID</th>
                  <th>Tier</th>
                  <th>Status</th>
                  <th>Action / Key</th>
                  <th>Expiration</th>
                </tr>
              </thead>
              <tbody>
                {licenses.map(lic => (
                  <tr key={lic._id}>
                    <td><code style={{ fontSize: '0.75rem', color: '#777' }}>{lic.licenseId.substring(0, 8)}...</code></td>
                    <td><span className="status-badge" style={{ background: '#ebf8ff', color: '#2b6cb0' }}>{lic.tier}</span></td>
                    <td>
                      <span className={`status-badge status-${lic.status?.toLowerCase()}`} style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                        {lic.status === 'Pending' && <Clock size={12} />}
                        {lic.status}
                      </span>
                    </td>
                    <td>
                      {lic.status === 'Active' ? (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                          <code style={{ background: '#f1f5f9', padding: '6px 10px', borderRadius: '6px', fontSize: '0.7rem', maxWidth: '120px', overflow: 'hidden', textOverflow: 'ellipsis' }}>{lic.licenseKey}</code>
                          <button onClick={() => copyToClipboard(lic.licenseKey)} className="btn" style={{ padding: '4px', color: copiedKey === lic.licenseKey ? '#48bb78' : 'var(--primary)' }}>
                            {copiedKey === lic.licenseKey ? <CheckCircle2 size={16} /> : <Copy size={16} />}
                          </button>
                        </div>
                      ) : (
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                          {lic.paymentProof ? (
                            <div style={{ fontSize: '0.7rem', color: '#48bb78', display: 'flex', alignItems: 'center', gap: '4px' }}>
                              <ImageIcon size={12} /> Proof Uploaded
                            </div>
                          ) : (
                            <label className="btn" style={{ background: '#edf2f7', fontSize: '0.7rem', padding: '6px 10px', cursor: 'pointer' }}>
                              <Upload size={14} /> Upload Proof
                              <input type="file" hidden accept="image/*,.pdf" onChange={(e) => handleUploadProof(lic._id, e.target.files[0])} />
                            </label>
                          )}
                        </div>
                      )}
                    </td>
                    <td style={{ fontSize: '0.85rem' }}>
                      {lic.expirationDate ? new Date(lic.expirationDate).toLocaleDateString() : '--'}
                    </td>
                  </tr>
                ))}
                {licenses.length === 0 && !loading && (
                  <tr><td colSpan="5" style={{ textAlign: 'center', padding: '4rem', color: '#999' }}>Generate a request to get started.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
      </div>

      <style jsx global>{`
        .status-pending { background: #fffaf0; color: #dd6b20; border: 1px solid #feebc8; }
        .nav-scrolled { box-shadow: 0 4px 12px rgba(0,0,0,0.05); }
      `}</style>
    </div>
  );
}
