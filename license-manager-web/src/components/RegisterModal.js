'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { X } from 'lucide-react';

export default function RegisterModal({ isOpen, onClose, onSwitchToLogin }) {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [username, setUsername] = useState('');
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(false);
    const [loading, setLoading] = useState(false);
    const router = useRouter();

    if (!isOpen) return null;

    const handleRegister = async (e) => {
        e.preventDefault();
        setLoading(true);
        setError('');

        try {
            const res = await fetch('/api/auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password, username })
            });

            const data = await res.json();
            if (!res.ok) throw new Error(data.error || 'Registration failed');

            setSuccess(true);
            setTimeout(() => {
                onSwitchToLogin();
                setSuccess(false);
            }, 2000);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content auth-card" style={{ margin: 0, position: 'relative' }}>
                <button onClick={onClose} style={{ position: 'absolute', right: '15px', top: '15px', border: 'none', background: 'none', cursor: 'pointer' }}>
                    <X size={24} color="#666" />
                </button>

                <h2 style={{ marginBottom: '1.5rem', textAlign: 'center' }}>Create IMS Account</h2>

                {error && <div style={{ color: 'red', marginBottom: '1rem', textAlign: 'center', fontSize: '0.9rem' }}>{error}</div>}
                {success && <div style={{ color: 'green', marginBottom: '1rem', textAlign: 'center', fontSize: '0.9rem' }}>Account created! Switching to login...</div>}

                <form onSubmit={handleRegister}>
                    <div>
                        <label style={{ fontSize: '0.85rem', fontWeight: 600 }}>Display Name</label>
                        <input
                            type="text"
                            className="input"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            placeholder="John Doe"
                        />
                    </div>

                    <div>
                        <label style={{ fontSize: '0.85rem', fontWeight: 600 }}>Email Address</label>
                        <input
                            type="email"
                            className="input"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            placeholder="john@example.com"
                            required
                        />
                    </div>

                    <div>
                        <label style={{ fontSize: '0.85rem', fontWeight: 600 }}>Password</label>
                        <input
                            type="password"
                            className="input"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            placeholder="••••••••"
                            required
                        />
                    </div>

                    <button type="submit" className="btn btn-primary" style={{ width: '100%', marginTop: '0.5rem' }} disabled={loading}>
                        {loading ? 'Processing...' : 'Register'}
                    </button>
                </form>

                <div style={{ marginTop: '1.5rem', textAlign: 'center', fontSize: '0.9rem' }}>
                    Already have an account? <button onClick={onSwitchToLogin} style={{ color: 'var(--primary)', border: 'none', background: 'none', cursor: 'pointer', fontWeight: 600 }}>Login</button>
                </div>
            </div>
            <style jsx>{`
                .modal-overlay {
                    position: fixed;
                    top: 0;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    background: rgba(0, 0, 0, 0.4);
                    backdrop-filter: blur(5px);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 2000;
                    padding: 1rem;
                }
                .modal-content {
                    width: 100%;
                    max-width: 420px;
                    animation: slideUp 0.3s ease-out;
                }
                @keyframes slideUp {
                    from { transform: translateY(20px); opacity: 0; }
                    to { transform: translateY(0); opacity: 1; }
                }
            `}</style>
        </div>
    );
}
