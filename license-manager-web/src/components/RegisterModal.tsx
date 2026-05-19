'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { X, Check } from 'lucide-react';
import { TermsModal, PrivacyModal } from './LegalModals';

export default function RegisterModal({ isOpen, onClose, onSwitchToLogin }) {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [username, setUsername] = useState('');
    const [agreed, setAgreed] = useState(false);

    // Modal states
    const [showTerms, setShowTerms] = useState(false);
    const [showPrivacy, setShowPrivacy] = useState(false);

    const [error, setError] = useState('');
    const [success, setSuccess] = useState(false);
    const [loading, setLoading] = useState(false);
    const router = useRouter();

    if (!isOpen) return null;

    const handleRegister = async (e) => {
        e.preventDefault();
        if (!agreed) {
            setError('You must agree to the Terms and Privacy Policy to register.');
            return;
        }

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

                    <div style={{ margin: '1rem 0', display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                        <div
                            onClick={() => setAgreed(!agreed)}
                            style={{
                                width: '20px', height: '20px', flexShrink: 0,
                                borderRadius: '4px', border: `1px solid ${agreed ? 'var(--primary)' : '#ccc'}`,
                                background: agreed ? 'var(--primary)' : '#fff', cursor: 'pointer',
                                display: 'flex', alignItems: 'center', justifyContent: 'center'
                            }}
                        >
                            {agreed && <Check size={14} color="#fff" />}
                        </div>
                        <p style={{ fontSize: '0.85rem', color: '#555', lineHeight: 1.4 }}>
                            By signing up, you agree to our{' '}
                            <button type="button" onClick={() => setShowTerms(true)} className="link-btn">Terms of Use</button> and{' '}
                            <button type="button" onClick={() => setShowPrivacy(true)} className="link-btn">Privacy Policy</button>.
                        </p>
                    </div>

                    <button type="submit" className="btn btn-primary" style={{ width: '100%', marginTop: '0.5rem', opacity: agreed ? 1 : 0.6 }} disabled={loading || !agreed}>
                        {loading ? 'Processing...' : 'Register'}
                    </button>
                </form>

                <div style={{ marginTop: '1.5rem', textAlign: 'center', fontSize: '0.9rem' }}>
                    Already have an account? <button onClick={onSwitchToLogin} style={{ color: 'var(--primary)', border: 'none', background: 'none', cursor: 'pointer', fontWeight: 600 }}>Login</button>
                </div>
            </div>

            {/* Nested Modals */}
            <TermsModal isOpen={showTerms} onClose={() => setShowTerms(false)} />
            <PrivacyModal isOpen={showPrivacy} onClose={() => setShowPrivacy(false)} />

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
                .link-btn {
                    background: none;
                    border: none;
                    color: var(--primary);
                    font-weight: 600;
                    cursor: pointer;
                    padding: 0;
                    text-decoration: underline;
                    font-size: 0.85rem;
                }
                .link-btn:hover { color: var(--secondary); }
                @keyframes slideUp {
                    from { transform: translateY(20px); opacity: 0; }
                    to { transform: translateY(0); opacity: 1; }
                }
            `}</style>
        </div>
    );
}

