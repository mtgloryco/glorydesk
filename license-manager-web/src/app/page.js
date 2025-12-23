'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import {
    Shield, Smartphone, Zap, CloudOff, Package, Check,
    ArrowRight, Menu, X, Laptop, Database, RefreshCw,
    Mail, Send, Box
} from 'lucide-react';
import { LICENSE_CONFIG } from '@/lib/config';
import LoginModal from '@/components/LoginModal';
import RegisterModal from '@/components/RegisterModal';

export default function LandingPage() {
    const [isMenuOpen, setIsMenuOpen] = useState(false);
    const [showLogin, setShowLogin] = useState(false);
    const [showRegister, setShowRegister] = useState(false);
    const [scrolled, setScrolled] = useState(false);

    const [contactName, setContactName] = useState('');
    const [contactEmail, setContactEmail] = useState('');
    const [contactMsg, setContactMsg] = useState('');
    const [sending, setSending] = useState(false);
    const [sent, setSent] = useState(false);

    useEffect(() => {
        const handleScroll = () => setScrolled(window.scrollY > 50);
        window.addEventListener('scroll', handleScroll);

        const params = new URLSearchParams(window.location.search);
        if (params.get('auth') === 'login') setShowLogin(true);
        if (params.get('auth') === 'register') setShowRegister(true);

        return () => window.removeEventListener('scroll', handleScroll);
    }, []);

    const toggleMenu = () => setIsMenuOpen(!isMenuOpen);
    const openLogin = () => { setShowLogin(true); setShowRegister(false); setIsMenuOpen(false); };
    const openRegister = () => { setShowRegister(true); setShowLogin(false); setIsMenuOpen(false); };

    const plans = Object.entries(LICENSE_CONFIG.plans).map(([key, plan]) => ({
        id: key,
        ...plan,
        features: plan.id === '1-year'
            ? ['Everything in Pro', 'Multi-device Sync (Beta)', 'Priority Support', 'Cloud Backups', 'Excel Exports']
            : plan.id === '1-month'
                ? ['Full POS System', 'Profit & Loss Charts', 'Advanced Reports', 'Thermal Printing', 'Barcode Scanning']
                : ['Inventory Tracking', 'Unlimited Products', 'Basic Dashboard', 'NO POS Access', 'NO Reports']
    }));

    const handleContact = async (e) => {
        e.preventDefault();
        setSending(true);
        try {
            const res = await fetch('/api/contacts', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: contactName, email: contactEmail, message: contactMsg })
            });
            if (res.ok) {
                setSent(true);
                setContactName(''); setContactEmail(''); setContactMsg('');
                setTimeout(() => setSent(false), 5000);
            }
        } catch (e) { alert('Failed to send message'); }
        finally { setSending(false); }
    };

    return (
        <div className="landing-wrap">
            {/* Header */}
            <nav className={`nav ${scrolled ? 'nav-scrolled' : ''}`}>
                <div className="container nav-container">
                    <Link href="/" className="logo">
                        <Box size={32} color="var(--primary)" />
                        <span>IMS Manager</span>
                    </Link>

                    <ul className="nav-links desktop-menu">
                        <li><a href="#features" className="nav-item">Features</a></li>
                        <li><a href="#downloads" className="nav-item">Downloads</a></li>
                        <li><a href="#pricing" className="nav-item">Pricing</a></li>
                        <li><Link href="/about" className="nav-item">About Us</Link></li>
                        <li><button onClick={openLogin} className="nav-btn">Login</button></li>
                        <li><button onClick={openRegister} className="btn btn-primary nav-cta">Get Started</button></li>
                    </ul>

                    <button className="mobile-toggle" onClick={toggleMenu}>
                        {isMenuOpen ? <X size={28} /> : <Menu size={28} />}
                    </button>
                </div>

                {isMenuOpen && (
                    <div className="mobile-menu">
                        <a href="#features" onClick={toggleMenu}>Features</a>
                        <a href="#downloads" onClick={toggleMenu}>Downloads</a>
                        <a href="#pricing" onClick={toggleMenu}>Pricing</a>
                        <Link href="/about" onClick={toggleMenu}>About Us</Link>
                        <button onClick={openLogin}>Login</button>
                        <button onClick={openRegister} className="btn btn-primary">Get Started</button>
                    </div>
                )}
            </nav>

            {/* Hero Section - Focused on IMS */}
            <header className="hero">
                <div className="container hero-content">
                    <div className="badge">PRODUCT VERSION 1.2.0-STABLE</div>
                    <h1 className="hero-title">
                        Advanced Inventory <br />
                        <span className="text-gradient">Management System.</span>
                    </h1>
                    <p className="hero-desc">
                        The ultimate offline-first software for stock control.
                        Request, activate, and manage your IMS Professional licenses through our secure hub.
                    </p>
                    <div className="hero-actions">
                        <button onClick={openRegister} className="btn btn-primary btn-lg">
                            Request My License <ArrowRight size={20} />
                        </button>
                        <button onClick={openLogin} className="btn btn-secondary btn-lg">
                            Software Login
                        </button>
                    </div>
                </div>
            </header>

            {/* Features Section - Focused on Product */}
            <section id="features" className="section bg-white">
                <div className="container">
                    <div className="section-header">
                        <h2 className="section-title">The IMS Professional Advantage</h2>
                        <p className="section-subtitle">Powerful tools designed for serious inventory management.</p>
                    </div>

                    <div className="feature-grid">
                        <FeatureBox
                            icon={<Zap size={32} color="var(--primary)" />}
                            title="RSA Activation"
                            description="Cryptographically signed licenses bound to your machine for maximum security."
                        />
                        <FeatureBox
                            icon={<Package size={32} color="#f59e0b" />}
                            title="Stock Lifecycle"
                            description="Complete control over Stock IN, Stock OUT, and real-time movement audits."
                        />
                        <FeatureBox
                            icon={<Laptop size={32} color="#10b981" />}
                            title="Native Desktop"
                            description="Native performance on Windows, Linux, and macOS without needing an internet connection."
                        />
                        <FeatureBox
                            icon={<CloudOff size={32} color="var(--secondary)" />}
                            title="Offline Reliable"
                            description="Work from anywhere. Your database lives on your machine, always accessible."
                        />
                    </div>

                    {/* V2 Section */}
                    <div className="v2-preview">
                        <div className="v2-content">
                            <span className="v2-tag">UPCOMING IN V2.0</span>
                            <h3>IMS Cloud Integration</h3>
                            <div className="v2-grid">
                                <div className="v2-item"><RefreshCw size={24} className="v2-icon" /><span>Universal Sync</span></div>
                                <div className="v2-item"><Database size={24} className="v2-icon" /><span>Encrypted Backup</span></div>
                                <div className="v2-item"><Smartphone size={24} className="v2-icon" /><span>Mobile Reports</span></div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            {/* Downloads Section */}
            <section id="downloads" className="section bg-gradient-to-b from-white to-gray-50">
                <div className="container">
                    <div className="section-header">
                        <h2 className="section-title">Download IMS Ready</h2>
                        <p className="section-subtitle">Get the native desktop application for your operating system.</p>
                    </div>

                    <div className="download-grid">
                        <div className="download-card">
                            <div className="os-icon win">
                                <Box size={40} />
                            </div>
                            <h3>Windows</h3>
                            <p>For Windows 10, 11 (64-bit)</p>
                            <a
                                href={process.env.NEXT_PUBLIC_DOWNLOAD_URL_WINDOWS || '#'}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="btn btn-primary btn-block"
                            >
                                <ArrowRight size={18} /> Download .EXE
                            </a>
                            <span className="file-info">Version 1.2.0 • Installer</span>
                        </div>

                        <div className="download-card">
                            <div className="os-icon lin">
                                <Box size={40} />
                            </div>
                            <h3>Linux</h3>
                            <p>Ubuntu, Debian, Fedora (64-bit)</p>
                            <a
                                href={process.env.NEXT_PUBLIC_DOWNLOAD_URL_LINUX || '#'}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="btn btn-secondary btn-block"
                            >
                                <ArrowRight size={18} /> Download .TAR.GZ
                            </a>
                            <span className="file-info">Version 1.2.0 • Archive</span>
                        </div>
                    </div>

                    <div className="install-guide">
                        <h3 className="guide-title">Installation & Activation Guide</h3>
                        <div className="steps-grid">
                            <div className="step-card">
                                <span className="step-num">01</span>
                                <h4>Download & Install</h4>
                                <p>Download the version compatible with your OS. Run the installer (Windows) or extract the archive (Linux).</p>
                            </div>
                            <div className="step-card">
                                <span className="step-num">02</span>
                                <h4>Create Account</h4>
                                <p>Click "Get Started" on this website to create your account. You need this to manage your licenses.</p>
                            </div>
                            <div className="step-card">
                                <span className="step-num">03</span>
                                <h4>Request License</h4>
                                <p>Log in to your dashboard and request a Free or Premium license. You will receive an activation key.</p>
                            </div>
                            <div className="step-card">
                                <span className="step-num">04</span>
                                <h4>Activate App</h4>
                                <p>Open the desktop app, go to Settings &gt; License, and enter your activation key to unlock features.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </section>


            {/* Pricing Section - Focused on Software License */}
            <section id="pricing" className="section">
                <div className="container">
                    <div className="section-header">
                        <h2 className="section-title">License Plans</h2>
                        <p className="section-subtitle">Choose the tier that fits your business scale.</p>
                    </div>

                    <div className="pricing-grid">
                        {plans.map((plan) => (
                            <div key={plan.id} className={`pricing-card ${plan.id === '1-month' ? 'pricing-highlight' : ''}`}>
                                {plan.id === '1-year' && <div className="save-badge">SAVE $12</div>}
                                <h3 className="plan-name">{plan.name}</h3>
                                <div className="plan-price">
                                    ${plan.price}
                                    <span className="period">{plan.id === '1-year' ? '/yr' : '/mo'}</span>
                                </div>
                                <p className="plan-desc">{plan.description}</p>
                                <ul className="plan-features">
                                    {plan.features.map((feat, i) => (
                                        <li key={i}><Check size={16} color="#10b981" /> {feat}</li>
                                    ))}
                                </ul>
                                <button onClick={openRegister} className={`btn btn-block ${plan.id === '1-month' ? 'btn-primary' : 'btn-outline'}`}>
                                    Activate IMS
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            {/* Contact Form */}
            <section id="contact" className="section bg-white">
                <div className="container" style={{ maxWidth: '600px' }}>
                    <div className="section-header">
                        <h2 className="section-title">Support Desk</h2>
                        <p className="section-subtitle">Need help with your IMS activation? Send us a message.</p>
                    </div>

                    <form className="glass-card" style={{ padding: '2.5rem' }} onSubmit={handleContact}>
                        {sent && <div style={{ background: '#e6fffa', color: '#2c7a7b', padding: '1rem', borderRadius: '8px', marginBottom: '1.5rem', textAlign: 'center', fontWeight: 600 }}>Message Sent! We will contact you shortly.</div>}
                        <div style={{ marginBottom: '1.2rem' }}>
                            <label className="input-label">FullName</label>
                            <input type="text" className="input" value={contactName} onChange={e => setContactName(e.target.value)} placeholder="Full Name" required />
                        </div>
                        <div style={{ marginBottom: '1.2rem' }}>
                            <label className="input-label">Email Address</label>
                            <input type="email" className="input" value={contactEmail} onChange={e => setContactEmail(e.target.value)} placeholder="your@email.com" required />
                        </div>
                        <div style={{ marginBottom: '1.5rem' }}>
                            <label className="input-label">How can we help?</label>
                            <textarea className="input" value={contactMsg} onChange={e => setContactMsg(e.target.value)} placeholder="Describe your issue..." rows="4" style={{ resize: 'none' }} required></textarea>
                        </div>
                        <button type="submit" className="btn btn-primary btn-block" disabled={sending}>
                            {sending ? 'Sending...' : <><Send size={18} /> Send Message</>}
                        </button>
                    </form>
                </div>
            </section>

            {/* Footer */}
            <footer className="footer">
                <div className="container footer-grid">
                    <div className="footer-info">
                        <div className="logo footer-logo">
                            <Box size={24} color="var(--primary)" />
                            <span>IMS Support</span>
                        </div>
                        <p style={{ marginBottom: '1rem' }}>Leading Management Information System (MIS) for professional inventory control.</p>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#666', fontSize: '0.9rem' }}>
                            <Mail size={16} /> mwimulebienvenu05@gmail.com
                        </div>
                    </div>
                    <div className="footer-links-wrap">
                        <div className="footer-links">
                            <h4>Resources</h4>
                            <Link href="/about">About Us</Link>
                            <Link href="/privacy">Privacy Policy</Link>
                            <Link href="/terms">License Terms</Link>
                        </div>
                    </div>
                </div>
                <div className="copyright">
                    &copy; {new Date().getFullYear()} IMS. All rights reserved.
                </div>
            </footer>

            <LoginModal isOpen={showLogin} onClose={() => setShowLogin(false)} onSwitchToRegister={openRegister} />
            <RegisterModal isOpen={showRegister} onClose={() => setShowRegister(false)} onSwitchToLogin={openLogin} />

            <style jsx global>{`
                .landing-wrap {
                    font-family: 'Inter', -apple-system, system-ui, sans-serif;
                    overflow-x: hidden;
                }
                .container {
                    max-width: 1200px;
                    margin: 0 auto;
                    padding: 0 1.5rem;
                }
                .nav {
                    position: fixed;
                    top: 0;
                    width: 100%;
                    z-index: 1000;
                    transition: all 0.3s;
                    background: transparent;
                }
                .nav-scrolled {
                    background: rgba(255, 255, 255, 0.95);
                    backdrop-filter: blur(10px);
                    box-shadow: 0 2px 20px rgba(0,0,0,0.08);
                    padding: 0.2rem 0;
                }
                .nav-container {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    padding: 1.2rem;
                }
                .logo {
                    display: flex;
                    align-items: center;
                    gap: 12px;
                    font-weight: 800;
                    font-size: 1.5rem;
                    color: #000;
                    text-decoration: none;
                }
                .nav-links {
                    display: flex;
                    list-style: none;
                    gap: 2.5rem; /* Increased gap to prevent collision */
                    align-items: center;
                }
                .nav-item {
                    white-space: nowrap; /* Prevent text wrapping */
                }
                .nav-links a, .nav-btn {
                    text-decoration: none;
                    color: #333;
                    font-weight: 600;
                    font-size: 1rem;
                    background: none;
                    border: none;
                    cursor: pointer;
                    transition: color 0.2s;
                }
                .nav-links a:hover, .nav-btn:hover {
                    color: var(--primary);
                }
                .nav-cta {
                    padding: 0.8rem 1.5rem !important;
                    font-size: 0.95rem !important;
                    white-space: nowrap;
                }
                .mobile-toggle {
                    display: none;
                    background: none;
                    border: none;
                    cursor: pointer;
                    color: #333;
                }

                .hero {
                    padding: 12rem 0 8rem;
                    background: radial-gradient(circle at top right, rgba(0, 112, 243, 0.08), transparent),
                                radial-gradient(circle at bottom left, rgba(121, 40, 202, 0.08), transparent);
                    text-align: center;
                }
                .hero-title {
                    font-size: 4.5rem;
                    line-height: 1.1;
                    font-weight: 900;
                    margin-bottom: 2rem;
                    letter-spacing: -0.05em;
                }
                .hero-desc {
                    font-size: 1.25rem;
                    color: #555;
                    max-width: 750px;
                    margin: 0 auto 3.5rem;
                    line-height: 1.6;
                }
                .hero-actions {
                    display: flex;
                    gap: 1.5rem;
                    justify-content: center;
                }
                .text-gradient {
                    background: linear-gradient(to right, var(--primary), var(--secondary));
                    -webkit-background-clip: text;
                    -webkit-text-fill-color: transparent;
                }

                .section { padding: 8rem 0; }
                .section-header { text-align: center; margin-bottom: 5rem; }
                .section-title { font-size: 3rem; font-weight: 800; margin-bottom: 1.2rem; letter-spacing: -0.02em; }
                .section-subtitle { color: #666; font-size: 1.2rem; max-width: 600px; margin: 0 auto; }

                .bg-white { background: #fff; }

                .feature-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
                    gap: 2.5rem;
                }

                .v2-preview {
                    margin-top: 6rem;
                    background: #000;
                    color: #fff;
                    border-radius: 32px;
                    padding: 5rem;
                    text-align: center;
                }
                .v2-tag {
                    display: inline-block;
                    padding: 0.5rem 1.2rem;
                    background: rgba(255,255,255,0.1);
                    border-radius: 50px;
                    font-size: 0.8rem;
                    font-weight: 800;
                    margin-bottom: 2rem;
                }
                .v2-preview h3 { font-size: 2.5rem; font-weight: 700; margin-bottom: 3rem; }
                .v2-grid {
                    display: flex;
                    justify-content: center;
                    gap: 4rem;
                    flex-wrap: wrap;
                }
                .v2-item {
                    display: flex;
                    align-items: center;
                    gap: 12px;
                    font-weight: 600;
                    color: #ccc;
                    font-size: 1.1rem;
                }
                .v2-icon { color: var(--primary); }
                
                /* Download Section Styles */
                .download-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
                    gap: 2rem;
                    max-width: 800px;
                    margin: 0 auto;
                }
                .download-card {
                    background: #fff;
                    padding: 3rem 2rem;
                    border-radius: 24px;
                    border: 1px solid #eee;
                    text-align: center;
                    transition: transform 0.3s ease, box-shadow 0.3s ease;
                }
                .download-card:hover {
                    transform: translateY(-5px);
                    box-shadow: 0 20px 40px rgba(0,0,0,0.08);
                    border-color: var(--primary);
                }
                .os-icon {
                    width: 80px;
                    height: 80px;
                    border-radius: 20px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    margin: 0 auto 1.5rem;
                    color: #fff;
                }
                .os-icon.win { background: linear-gradient(135deg, #0078D7, #00C7FD); }
                .os-icon.lin { background: linear-gradient(135deg, #E95420, #FAA61A); }
                
                .download-card h3 { font-size: 1.5rem; margin-bottom: 0.5rem; font-weight: 700; }
                .download-card p { color: #666; margin-bottom: 2rem; font-size: 0.95rem; }
                .download-card .btn { display: flex; align-items: center; justify-content: center; gap: 8px; margin-bottom: 1rem; }
                .file-info { font-size: 0.8rem; color: #999; font-weight: 500; }

                /* Installation Guide Styles */
                .install-guide { margin-top: 6rem; text-align: center; }
                .guide-title { font-size: 2rem; font-weight: 800; margin-bottom: 3rem; }
                .steps-grid { 
                    display: grid; 
                    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); 
                    gap: 2rem; 
                    text-align: left;
                }
                .step-card {
                    background: #fff;
                    padding: 2rem;
                    border-radius: 20px;
                    border: 1px solid #f0f0f0;
                    position: relative;
                }
                .step-num {
                    font-size: 3rem;
                    font-weight: 900;
                    color: rgba(0, 112, 243, 0.1);
                    position: absolute;
                    top: 10px;
                    right: 20px;
                    line-height: 1;
                }
                .step-card h4 { font-size: 1.1rem; font-weight: 700; margin-bottom: 0.8rem; position: relative; z-index: 1; }
                .step-card p { font-size: 0.95rem; color: #666; line-height: 1.6; position: relative; z-index: 1; }

                .pricing-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
                    gap: 2.5rem;
                    padding: 1rem 0;
                }
                .pricing-card {
                    background: #fff;
                    padding: 4rem 2.55rem;
                    border-radius: 24px;
                    border: 1px solid #eee;
                    position: relative;
                    text-align: center;
                    transition: all 0.4s ease;
                }
                .pricing-highlight {
                    border: 2px solid var(--primary);
                    box-shadow: 0 30px 60px rgba(0, 112, 243, 0.12);
                    transform: translateY(-10px);
                }
                .save-badge {
                    position: absolute;
                    top: -15px;
                    left: 50%;
                    transform: translateX(-50%);
                    background: #10b981;
                    color: #fff;
                    padding: 0.6rem 1.5rem;
                    border-radius: 50px;
                    font-size: 0.85rem;
                    font-weight: 800;
                    box-shadow: 0 4px 10px rgba(16, 185, 129, 0.3);
                }
                .plan-name { font-size: 1.4rem; color: #666; font-weight: 600; margin-bottom: 0.8rem; }
                .plan-price { font-size: 4rem; font-weight: 900; margin-bottom: 1.5rem; line-height: 1; }
                .plan-price .period { font-size: 1.1rem; color: #999; font-weight: 500; margin-left: 2px; }
                .plan-desc { color: #777; font-size: 1rem; margin-bottom: 2.5rem; height: 3em; }
                .plan-features { list-style: none; text-align: left; margin-bottom: 3rem; }
                .plan-features li { margin-bottom: 1rem; font-size: 1rem; display: flex; align-items: center; gap: 12px; color: #444; }

                .footer { padding: 6rem 0 3rem; background: #fff; border-top: 1px solid #eee; }
                .footer-grid { display: grid; grid-template-columns: 1.5fr 1fr; gap: 5rem; }
                .footer-info p { color: #666; margin-top: 1.5rem; line-height: 1.8; }
                .footer-links h4 { font-size: 1.1rem; margin-bottom: 2rem; font-weight: 800; }
                .footer-links { display: flex; flex-direction: column; gap: 1.2rem; }
                .footer-links a { color: #666; text-decoration: none; font-size: 1rem; transition: color 0.2s; }
                .footer-links a:hover { color: var(--primary); }
                .copyright { margin-top: 5rem; padding-top: 3rem; border-top: 1px solid #f5f5f5; text-align: center; font-size: 0.9rem; color: #aaa; }

                .input-label { font-size: 0.9rem; font-weight: 700; color: #333; margin-bottom: 0.6rem; display: block; }
                .input { 
                    width: 100%; 
                    padding: 1rem; 
                    border: 1px solid #eee; 
                    border-radius: 12px; 
                    background: #fdfdfd; 
                    font-size: 1rem;
                    transition: all 0.2s;
                }
                .input:focus { outline: none; border-color: var(--primary); background: #fff; box-shadow: 0 0 0 4px rgba(0, 112, 243, 0.05); }

                .btn-lg { padding: 1.2rem 2.5rem; font-size: 1.15rem; border-radius: 14px; }
                .btn-secondary { background: #fff; border: 1px solid #ddd; color: #444; }
                .btn-secondary:hover { border-color: #999; background: #fafafa; }
                .btn-outline { background: transparent; border: 1px solid #eee; color: #444; }
                .btn-outline:hover { background: #f9f9f9; border-color: #ddd; }
                .btn-block { width: 100%; padding: 1.2rem; border-radius: 12px; }

                @media (max-width: 1024px) {
                    .hero-title { font-size: 3.8rem; }
                    .footer-grid { grid-template-columns: 1fr; gap: 3rem; text-align: center; }
                    .footer-logo { justify-content: center; }
                    .footer-links { align-items: center; }
                    .nav-links { gap: 1rem; } /* Reduce gap but keep it safe */
                }

                @media (max-width: 768px) {
                    .desktop-menu { display: none; }
                    .mobile-toggle { display: block; }
                    .mobile-menu {
                        position: fixed;
                        top: 75px;
                        left: 0;
                        right: 0;
                        background: #fff;
                        padding: 2.5rem;
                        display: flex;
                        flex-direction: column;
                        gap: 1.8rem;
                        box-shadow: 0 15px 30px rgba(0,0,0,0.1);
                        z-index: 999;
                        border-top: 1px solid #eee;
                    }
                    .mobile-menu a, .mobile-menu button { font-size: 1.2rem; font-weight: 700; text-align: center; }
                    .hero { padding: 10rem 0 6rem; }
                    .hero-title { font-size: 3.2rem; }
                    .hero-desc { font-size: 1.1rem; }
                    .hero-actions { flex-direction: column; gap: 1rem; }
                    .hero-actions .btn { width: 100%; }
                    .section-title { font-size: 2.3rem; }
                    .v2-preview { padding: 3rem 2rem; border-radius: 20px; }
                    .v2-grid { gap: 2rem; flex-direction: column; align-items: center; }
                    .plan-price { font-size: 3.2rem; }
                    .section { padding: 5rem 0; }
                }

                /* Make steps grid single column on mobile */
                @media (max-width: 600px) {
                    .steps-grid { grid-template-columns: 1fr; }
                    .download-grid { grid-template-columns: 1fr; }
                }

                .badge {
                    display: inline-block;
                    padding: 0.5rem 1.2rem;
                    background: #ebf8ff;
                    color: var(--primary);
                    border-radius: 50px;
                    font-size: 0.8rem;
                    font-weight: 800;
                    margin-bottom: 2rem;
                    letter-spacing: 0.02em;
                }
            `}</style>
        </div>
    );
}

function FeatureBox({ icon, title, description }) {
    return (
        <div className="glass-card" style={{ padding: '3rem 2.5rem', textAlign: 'left', borderRadius: '24px' }}>
            <div style={{ marginBottom: '1.8rem' }}>{icon}</div>
            <h3 style={{ fontSize: '1.4rem', marginBottom: '1.2rem', fontWeight: 700 }}>{title}</h3>
            <p style={{ color: '#666', fontSize: '1rem', lineHeight: 1.7 }}>{description}</p>
        </div>
    );
}
