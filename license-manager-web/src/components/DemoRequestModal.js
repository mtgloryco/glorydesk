'use client';
import { useState } from 'react';
import { X, Check, Copy, AlertCircle, Upload, Loader2, Clock, Calendar } from 'lucide-react';

export default function DemoRequestModal({ isOpen, onClose }) {
    const [step, setStep] = useState(1); // 1: Form, 2: Success
    const [formData, setFormData] = useState({
        name: '',
        email: '',
        hardwareId: '',
        duration: '6h',
        proofImage: null
    });
    const [loading, setLoading] = useState(false);
    const [result, setResult] = useState(null); // { success: true, licenseKey?: string, message?: string }
    const [error, setError] = useState('');
    const [showHwidHelp, setShowHwidHelp] = useState(false);

    if (!isOpen) return null;

    const handleFileChange = (e) => {
        const file = e.target.files[0];
        if (file) {
            if (file.size > 2 * 1024 * 1024) { // 2MB Limit
                setError('Image too large. Max 2MB.');
                return;
            }
            const reader = new FileReader();
            reader.onloadend = () => {
                setFormData({ ...formData, proofImage: reader.result });
                setError('');
            };
            reader.readAsDataURL(file);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        try {
            const res = await fetch('/api/demo-request', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            });

            const data = await res.json();

            if (!res.ok) throw new Error(data.error || 'Failed to submit request');

            setResult(data);
            setStep(2);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    const copyKey = () => {
        if (result?.licenseKey) {
            navigator.clipboard.writeText(result.licenseKey);
            alert('License key copied!');
        }
    };

    return (
        <div className="modal-overlay">
            <div className="modal-content glass-card demo-modal">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
                    <h2 style={{ fontSize: '1.5rem', fontWeight: 800, background: 'linear-gradient(to right, #0070f3, #7928ca)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent' }}>
                        {step === 1 ? 'Request Demo License' : 'Request Status'}
                    </h2>
                    <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#666' }}>
                        <X size={24} />
                    </button>
                </div>

                {step === 1 && (
                    <form onSubmit={handleSubmit}>
                        {error && (
                            <div style={{ background: '#fff5f5', color: '#c53030', padding: '0.8rem', borderRadius: '8px', marginBottom: '1rem', fontSize: '0.9rem', display: 'flex', alignItems: 'center', gap: '8px' }}>
                                <AlertCircle size={16} /> {error}
                            </div>
                        )}

                        <div style={{ marginBottom: '1rem' }}>
                            <label className="input-label">Full Name</label>
                            <input
                                type="text"
                                className="input"
                                placeholder="John Doe"
                                value={formData.name}
                                onChange={e => setFormData({ ...formData, name: e.target.value })}
                                required
                            />
                        </div>

                        <div style={{ marginBottom: '1rem' }}>
                            <label className="input-label">Email Address</label>
                            <input
                                type="email"
                                className="input"
                                placeholder="john@example.com"
                                value={formData.email}
                                onChange={e => setFormData({ ...formData, email: e.target.value })}
                                required
                            />
                        </div>

                        <div style={{ marginBottom: '1rem' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.6rem' }}>
                                <label className="input-label" style={{ marginBottom: 0 }}>Hardware ID</label>
                                <button
                                    type="button"
                                    onClick={() => setShowHwidHelp(!showHwidHelp)}
                                    style={{ background: 'none', border: 'none', color: '#0070f3', fontSize: '0.85rem', cursor: 'pointer', textDecoration: 'underline' }}
                                >
                                    How to find this?
                                </button>
                            </div>

                            {showHwidHelp && (
                                <div style={{ background: '#f0f9ff', padding: '1rem', borderRadius: '8px', border: '1px solid #bae6fd', marginBottom: '0.8rem', fontSize: '0.9rem', color: '#0c4a6e' }}>
                                    <p style={{ fontWeight: 600, marginBottom: '0.5rem' }}>Steps to find your Hardware ID:</p>
                                    <ol style={{ marginLeft: '1.2rem', lineHeight: '1.6' }}>
                                        <li>Open the <b>IMS Desktop Application</b>.</li>
                                        <li>If you have no license, the <b>License Screen</b> appears automatically.</li>
                                        <li>Find the field labeled <b>Hardware ID</b>.</li>
                                        <li>Copy that ID and paste it here.</li>
                                    </ol>
                                    <div style={{ marginTop: '1rem', border: '1px solid #ddd', borderRadius: '4px', overflow: 'hidden' }}>
                                        <img src="/demo-hwid.png" alt="Hardware ID Location" style={{ width: '100%', display: 'block' }} />
                                    </div>
                                </div>
                            )}

                            <input
                                type="text"
                                className="input"
                                placeholder="Paste your Hardware ID here..."
                                value={formData.hardwareId}
                                onChange={e => setFormData({ ...formData, hardwareId: e.target.value })}
                                required
                            />
                        </div>
                        <div style={{ marginBottom: '1.5rem' }}>
                            <label className="input-label" style={{ marginBottom: '0.8rem' }}>Select Duration</label>
                            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px' }}>

                                {/* 6 Hours */}
                                <label className={`duration-card ${formData.duration === '6h' ? 'selected' : ''}`}>
                                    <input
                                        type="radio"
                                        name="duration"
                                        value="6h"
                                        checked={formData.duration === '6h'}
                                        onChange={e => setFormData({ ...formData, duration: e.target.value })}
                                        style={{ position: 'absolute', opacity: 0 }}
                                    />
                                    <Clock size={20} className="icon" />
                                    <span className="dur-title">6 Hours</span>
                                    <span className="dur-price">Free</span>
                                </label>

                                {/* 1 Day */}
                                <label className={`duration-card ${formData.duration === '1d' ? 'selected' : ''}`}>
                                    <input
                                        type="radio"
                                        name="duration"
                                        value="1d"
                                        checked={formData.duration === '1d'}
                                        onChange={e => setFormData({ ...formData, duration: e.target.value })}
                                        style={{ position: 'absolute', opacity: 0 }}
                                    />
                                    <Calendar size={20} className="icon" />
                                    <span className="dur-title">1 Day</span>
                                    <span className="dur-price">$0.50</span>
                                </label>

                                {/* 2 Days */}
                                <label className={`duration-card ${formData.duration === '2d' ? 'selected' : ''}`}>
                                    <input
                                        type="radio"
                                        name="duration"
                                        value="2d"
                                        checked={formData.duration === '2d'}
                                        onChange={e => setFormData({ ...formData, duration: e.target.value })}
                                        style={{ position: 'absolute', opacity: 0 }}
                                    />
                                    <Calendar size={20} className="icon" />
                                    <span className="dur-title">2 Days</span>
                                    <span className="dur-price">$0.98</span>
                                </label>
                            </div>
                        </div>

                        {(formData.duration === '1d' || formData.duration === '2d') && (
                            <div style={{ marginBottom: '1.5rem', background: '#f8fafc', padding: '1rem', borderRadius: '12px', border: '1px solid #eee' }}>
                                <label className="input-label">Proof of Payment</label>
                                <div style={{ fontSize: '0.85rem', color: '#666', marginBottom: '0.8rem' }}>
                                    Please transfer <b>{formData.duration === '1d' ? '$0.50' : '$0.98'}</b> to <b>MOMO:0792748347</b> and upload the screenshot.
                                </div>
                                <div className="file-upload-wrap">
                                    <input type="file" accept="image/*" onChange={handleFileChange} id="proof-upload" style={{ display: 'none' }} />
                                    <label htmlFor="proof-upload" className="file-btn">
                                        <Upload size={16} /> {formData.proofImage ? 'Change Image' : 'Upload Screenshot'}
                                    </label>
                                    {formData.proofImage && <span style={{ fontSize: '0.8rem', color: '#10b981', display: 'flex', alignItems: 'center', gap: '4px' }}><Check size={12} /> Image Selected</span>}
                                </div>
                            </div>
                        )}

                        <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
                            {loading ? <><Loader2 className="spin" size={18} /> Processing...</> : 'Request License'}
                        </button>
                    </form>
                )}

                {step === 2 && result && (
                    <div style={{ textAlign: 'center', padding: '1rem 0' }}>
                        {formData.duration === '6h' ? (
                            // Automated Success
                            <>
                                <div style={{ width: '60px', height: '60px', background: '#dcfce7', borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 1.5rem' }}>
                                    <Check size={32} color="#166534" />
                                </div>
                                <h3 style={{ fontSize: '1.2rem', fontWeight: 800, marginBottom: '0.5rem' }}>License Activated!</h3>
                                <p style={{ color: '#666', marginBottom: '1.5rem' }}>Your 6-hour demo license is ready. Enter this key in the desktop app immediately.</p>

                                <div style={{ background: '#f1f5f9', padding: '1rem', borderRadius: '8px', wordBreak: 'break-all', fontFamily: 'monospace', fontSize: '0.85rem', border: '1px solid #e2e8f0', marginBottom: '1rem', maxHeight: '150px', overflowY: 'auto' }}>
                                    {result.licenseKey}
                                </div>

                                <button onClick={copyKey} className="btn btn-primary btn-block" style={{ marginBottom: '1rem' }}>
                                    <Copy size={18} /> Copy License Key
                                </button>
                            </>
                        ) : (
                            // Manual Pending
                            <>
                                <div style={{ width: '60px', height: '60px', background: '#e0f2fe', borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 1.5rem' }}>
                                    <Clock size={32} color="#0369a1" />
                                </div>
                                <h3 style={{ fontSize: '1.2rem', fontWeight: 800, marginBottom: '0.5rem' }}>Request Received</h3>
                                <p style={{ color: '#666', marginBottom: '1.5rem' }}>We have received your request and payment proof. Our team will verify it and email your license key to <b>{formData.email}</b> shortly.</p>
                            </>
                        )}

                        <button onClick={onClose} className="btn btn-outline btn-block">Close</button>
                    </div>
                )}

            </div>

            <style jsx>{`
        .demo-modal { width: 100%; max-width: 500px; max-height: 90vh; overflow-y: auto; }
        .duration-card {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: 1rem 0.5rem;
            border: 2px solid #eee;
            border-radius: 12px;
            cursor: pointer;
            transition: all 0.2s;
            position: relative;
        }
        .duration-card:hover { border-color: #ccc; }
        .duration-card.selected {
            border-color: var(--primary);
            background: rgba(0, 112, 243, 0.05);
            color: var(--primary);
        }
        .icon { margin-bottom: 0.5rem; color: #999; }
        .duration-card.selected .icon { color: var(--primary); }
        .dur-title { font-weight: 700; font-size: 0.9rem; margin-bottom: 0.2rem; }
        .dur-price { font-size: 0.8rem; color: #666; }
        
        .file-upload-wrap { display: flex; align-items: center; gap: 1rem; }
        .file-btn {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 0.6rem 1rem;
            background: #fff;
            border: 1px solid #ddd;
            border-radius: 8px;
            font-size: 0.9rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
        }
        .file-btn:hover { background: #f9f9f9; border-color: #bbb; }
        
        .spin { animation: spin 1s linear infinite; }
        @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
      `}</style>
        </div>
    );
}
