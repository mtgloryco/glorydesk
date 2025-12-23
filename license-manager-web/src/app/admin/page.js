'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import {
    ShieldAlert, Activity, CreditCard, RefreshCw,
    Calendar, Trash2, CheckCircle, Mail, MessageSquare, Clock,
    Eye, CheckCircle2, XCircle
} from 'lucide-react';

export default function AdminDashboard() {
    const [licenses, setLicenses] = useState([]);
    const [contacts, setContacts] = useState([]);
    const [activeTab, setActiveTab] = useState('licenses');
    const [loading, setLoading] = useState(true);
    const [selectedProof, setSelectedProof] = useState(null);
    const router = useRouter();

    useEffect(() => {
        const token = localStorage.getItem('token');
        const user = JSON.parse(localStorage.getItem('user') || '{}');
        if (!token || user.role !== 'admin') {
            router.push('/login');
            return;
        }
        fetchAdminData();
        fetchContacts();
    }, []);

    const fetchAdminData = async () => {
        const token = localStorage.getItem('token');
        try {
            const res = await fetch('/api/admin/licenses', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            const data = await res.json();
            setLicenses(data);
        } catch (err) { console.error(err); }
        finally { setLoading(false); }
    };

    const fetchContacts = async () => {
        try {
            const res = await fetch('/api/contacts');
            const data = await res.json();
            setContacts(data);
        } catch (err) { console.error(err); }
    };

    const handleApprove = async (id) => {
        if (!confirm('Approve this payment and generate RSA license key?')) return;
        const token = localStorage.getItem('token');
        try {
            const res = await fetch('/api/admin/licenses', {
                method: 'PATCH',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ id, action: 'approve' })
            });
            if (res.ok) fetchAdminData();
            else alert('Approval failed');
        } catch (err) { alert('Error'); }
    };

    const handleUpdateStatus = async (id, status) => {
        const token = localStorage.getItem('token');
        try {
            await fetch('/api/admin/licenses', {
                method: 'PATCH',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ id, action: 'update_status', status })
            });
            fetchAdminData();
        } catch (err) { alert('Failed'); }
    };

    return (
        <div style={{ background: '#f8fafc', minHeight: '100vh' }}>
            <nav style={{ background: '#111827', padding: '1rem 2rem', position: 'sticky', top: 0, zIndex: 100 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div style={{ fontWeight: 800, fontSize: '1.2rem', display: 'flex', alignItems: 'center', gap: '10px', color: '#fff' }}>
                        <ShieldAlert color="#ef4444" /> <span>MSS ADMIN</span>
                    </div>
                    <div style={{ display: 'flex', gap: '1rem' }}>
                        <TabBtn active={activeTab === 'licenses'} onClick={() => setActiveTab('licenses')}>Activations</TabBtn>
                        <TabBtn active={activeTab === 'contacts'} onClick={() => setActiveTab('contacts')}>Messages</TabBtn>
                        <button onClick={() => { localStorage.clear(); router.push('/login'); }} className="btn" style={{ background: '#ef4444', color: '#fff', fontSize: '0.8rem' }}>Logout</button>
                    </div>
                </div>
            </nav>

            <div className="container" style={{ padding: '3rem 1.5rem' }}>
                <header style={{ marginBottom: '2.5rem' }}>
                    <h1 style={{ fontSize: '2rem', fontWeight: 900, marginBottom: '0.5rem' }}>Authority Control</h1>
                    <p style={{ color: '#64748b' }}>Validate payments and issue cryptographically signed IMS licenses.</p>
                </header>

                {activeTab === 'licenses' ? (
                    <>
                        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '1.5rem', marginBottom: '3rem' }}>
                            <StatCard title="Total" value={licenses.length} color="#3b82f6" icon={<Activity size={20} />} />
                            <StatCard title="Pending" value={licenses.filter(l => l.status === 'Pending').length} color="#f59e0b" icon={<Clock size={20} />} />
                            <StatCard title="Active" value={licenses.filter(l => l.status === 'Active').length} color="#10b981" icon={<CheckCircle2 size={20} />} />
                        </div>

                        <section className="glass-card" style={{ background: '#fff' }}>
                            <h3 style={{ marginBottom: '1.5rem' }}>License Request Log</h3>
                            <div className="table-container">
                                <table>
                                    <thead>
                                        <tr>
                                            <th>User / Tier</th>
                                            <th>Status</th>
                                            <th>Hardware ID</th>
                                            <th>Payment Proof</th>
                                            <th>Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {licenses.map(lic => (
                                            <tr key={lic._id}>
                                                <td>
                                                    <div style={{ fontWeight: 700 }}>{lic.userDetails?.username}</div>
                                                    <div style={{ fontSize: '0.75rem', color: '#64748b' }}>{lic.userDetails?.email}</div>
                                                    <span style={{ fontSize: '0.65rem', background: '#f1f5f9', padding: '2px 6px', borderRadius: '4px', marginTop: '4px', display: 'inline-block' }}>{lic.tier}</span>
                                                </td>
                                                <td><span className={`status-badge status-${lic.status?.toLowerCase()}`}>{lic.status}</span></td>
                                                <td><code style={{ fontSize: '0.7rem' }}>{lic.hardwareId}</code></td>
                                                <td>
                                                    {lic.paymentProof ? (
                                                        <button onClick={() => setSelectedProof(lic.paymentProof)} className="btn" style={{ background: '#ecfdf5', color: '#047857', padding: '6px 12px', fontSize: '0.7rem' }}>
                                                            <Eye size={14} /> View Proof
                                                        </button>
                                                    ) : (
                                                        <span style={{ fontSize: '0.7rem', color: '#94a3b8' }}>No proof yet</span>
                                                    )}
                                                </td>
                                                <td>
                                                    <div style={{ display: 'flex', gap: '6px' }}>
                                                        {lic.status === 'Pending' && (
                                                            <button
                                                                onClick={() => handleApprove(lic._id)}
                                                                disabled={!lic.paymentProof}
                                                                className="btn btn-primary"
                                                                style={{ padding: '6px 12px', fontSize: '0.7rem' }}
                                                            >
                                                                Approve & Sign
                                                            </button>
                                                        )}
                                                        <button onClick={() => handleUpdateStatus(lic._id, lic.status === 'Active' ? 'Revoked' : 'Active')} className="btn" style={{ background: '#f1f5f9', padding: '6px' }}>
                                                            {lic.status === 'Active' ? <Trash2 size={14} color="#ef4444" /> : <CheckCircle size={14} color="#10b981" />}
                                                        </button>
                                                    </div>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        </section>
                    </>
                ) : (
                    <section className="glass-card" style={{ background: '#fff' }}>
                        <h3 style={{ marginBottom: '2rem' }}>Support Messages</h3>
                        {contacts.map(msg => (
                            <div key={msg._id} style={{ borderBottom: '1px solid #f1f5f9', padding: '1.5rem 0' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                                    <div style={{ fontWeight: 700 }}>{msg.name} ({msg.email})</div>
                                    <div style={{ fontSize: '0.7rem', color: '#94a3b8' }}>{new Date(msg.createdAt).toLocaleString()}</div>
                                </div>
                                <p style={{ fontSize: '0.9rem', color: '#334155' }}>{msg.message}</p>
                            </div>
                        ))}
                    </section>
                )}
            </div>

            {/* Proof Modal */}
            {selectedProof && (
                <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '2rem' }}>
                    <div style={{ background: '#fff', padding: '1rem', borderRadius: '16px', width: '90%', maxWidth: '1000px', height: '90%', position: 'relative' }}>
                        <button onClick={() => setSelectedProof(null)} style={{ position: 'absolute', top: '-40px', right: 0, background: 'none', border: 'none', color: '#fff', cursor: 'pointer' }}><XCircle size={32} /></button>

                        {selectedProof.startsWith('data:application/pdf') ? (
                            <iframe src={selectedProof} style={{ width: '100%', height: '100%', border: 'none', borderRadius: '8px' }} />
                        ) : (
                            <div style={{ width: '100%', height: '100%', overflow: 'auto', display: 'flex', justifyContent: 'center' }}>
                                <img src={selectedProof} alt="Payment Proof" style={{ maxWidth: '100%', height: 'auto', borderRadius: '8px' }} />
                            </div>
                        )}
                    </div>
                </div>
            )}

            <style jsx global>{`
                .status-pending { background: #fef3c7; color: #92400e; }
            `}</style>
        </div>
    );
}

function TabBtn({ active, onClick, children }) {
    return (
        <button
            onClick={onClick}
            style={{
                background: active ? '#3b82f6' : 'transparent',
                color: '#fff',
                border: 'none',
                padding: '0.4rem 1rem',
                borderRadius: '6px',
                cursor: 'pointer',
                fontSize: '0.85rem',
                fontWeight: active ? 700 : 500
            }}
        >
            {children}
        </button>
    );
}

function StatCard({ title, value, color, icon }) {
    return (
        <div className="glass-card" style={{ background: '#fff', borderLeft: `4px solid ${color}` }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                    <p style={{ color: '#64748b', fontSize: '0.7rem', fontWeight: 800, textTransform: 'uppercase' }}>{title}</p>
                    <h2 style={{ fontSize: '1.8rem', fontWeight: 900, margin: '5px 0' }}>{value}</h2>
                </div>
                <div style={{ color, opacity: 0.6 }}>{icon}</div>
            </div>
        </div>
    );
}
