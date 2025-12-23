'use client';
import { useState, useEffect } from 'react';
import Link from 'next/link';
import { Shield, Target, Globe, Box, Menu, X } from 'lucide-react';

export default function AboutPage() {
    const [scrolled, setScrolled] = useState(false);
    const [isMenuOpen, setIsMenuOpen] = useState(false);

    useEffect(() => {
        const handleScroll = () => setScrolled(window.scrollY > 50);
        window.addEventListener('scroll', handleScroll);
        return () => window.removeEventListener('scroll', handleScroll);
    }, []);

    const toggleMenu = () => setIsMenuOpen(!isMenuOpen);

    return (
        <div className="landing-wrap">
            {/* Unified Nav */}
            <nav className={`nav ${scrolled ? 'nav-scrolled' : 'nav-scrolled'}`} style={{ position: 'fixed' }}>
                <div className="container nav-container">
                    <Link href="/" className="logo">
                        <Box size={32} color="var(--primary)" />
                        <span>IMS Manager</span>
                    </Link>

                    <ul className="nav-links desktop-menu">
                        <li><Link href="/" className="nav-btn">Home</Link></li>
                        <li><Link href="/#features" className="nav-btn">Features</Link></li>
                        <li><Link href="/about" className="nav-btn" style={{ color: 'var(--primary)' }}>About Us</Link></li>
                        <li><Link href="/" className="btn btn-primary nav-cta">Back to Product</Link></li>
                    </ul>

                    <button className="mobile-toggle" onClick={toggleMenu}>
                        {isMenuOpen ? <X size={28} /> : <Menu size={28} />}
                    </button>
                </div>

                {isMenuOpen && (
                    <div className="mobile-menu">
                        <Link href="/" onClick={toggleMenu}>Home</Link>
                        <Link href="/#features" onClick={toggleMenu}>Features</Link>
                        <Link href="/about" onClick={toggleMenu} style={{ color: 'var(--primary)' }}>About Us</Link>
                        <Link href="/" className="btn btn-primary" onClick={toggleMenu}>Back to Product</Link>
                    </div>
                )}
            </nav>

            <div className="container" style={{ padding: '10rem 2rem 6rem' }}>
                <div style={{ textAlign: 'center', marginBottom: '5rem' }}>
                    <span className="badge">MANAGEMENT SYSTEMS (MSS)</span>
                    <h1 className="title" style={{ fontSize: '3.5rem', marginBottom: '1.5rem' }}>Our Organization</h1>
                    <p style={{ fontSize: '1.25rem', color: '#555', maxWidth: '800px', margin: '0 auto', lineHeight: 1.6 }}>
                        Architecting professional Business Intelligence through innovative ERP and MIS solutions.
                        We build the backbone of modern retail and industrial operations.
                    </p>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))', gap: '3rem' }}>
                    <div className="glass-card" style={{ padding: '3rem' }}>
                        <Target size={48} color="var(--primary)" style={{ marginBottom: '2rem' }} />
                        <h2 style={{ fontSize: '1.8rem', marginBottom: '1.5rem' }}>The Vision</h2>
                        <p style={{ color: '#666', fontSize: '1.1rem', lineHeight: 1.8 }}>
                            At <strong>MSS</strong>, we believe every business deserves enterprise-grade tools.
                            Our mission is to replace fragmented workflows with unified Management Information Systems (MIS)
                            that provide real-time clarity and control over global operations.
                        </p>
                    </div>

                    <div className="glass-card" style={{ padding: '3rem' }}>
                        <Globe size={48} color="var(--secondary)" style={{ marginBottom: '2rem' }} />
                        <h2 style={{ fontSize: '1.8rem', marginBottom: '1.5rem' }}>Full-Stack ERP</h2>
                        <p style={{ color: '#666', fontSize: '1.1rem', lineHeight: 1.8 }}>
                            Beyond the IMS software, MSS is a full-stack technical house specializing in
                            Enterprise Resource Planning systems, financial trackers, and logistics automation
                            software designed for high-availability environments.
                        </p>
                    </div>
                </div>

                <section style={{ marginTop: '8rem' }}>
                    <div style={{ background: '#000', color: '#fff', borderRadius: '32px', padding: '6rem 4rem', textAlign: 'center' }}>
                        <h2 style={{ fontSize: '3rem', marginBottom: '3rem', fontWeight: 800 }}>Engineering Excellence.</h2>
                        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '4rem' }}>
                            <div>
                                <h4 style={{ color: 'var(--primary)', marginBottom: '1.2rem', fontSize: '1.3rem' }}>MIS Core</h4>
                                <p style={{ fontSize: '1rem', color: '#aaa', lineHeight: 1.6 }}>Data-driven decision making at every level of your operation.</p>
                            </div>
                            <div>
                                <h4 style={{ color: 'var(--primary)', marginBottom: '1.2rem', fontSize: '1.3rem' }}>Logistics</h4>
                                <p style={{ fontSize: '1rem', color: '#aaa', lineHeight: 1.6 }}>Bespoke automation for supply chain and warehouse tracking.</p>
                            </div>
                            <div>
                                <h4 style={{ color: 'var(--primary)', marginBottom: '1.2rem', fontSize: '1.3rem' }}>Offline-First</h4>
                                <p style={{ fontSize: '1rem', color: '#aaa', lineHeight: 1.6 }}>Superior reliability in environments with limited connectivity.</p>
                            </div>
                        </div>
                    </div>
                </section>
            </div>

            <footer className="footer" style={{ marginTop: 0 }}>
                <div className="container" style={{ textAlign: 'center', color: '#aaa', fontSize: '0.9rem', paddingTop: '3rem', borderTop: '1px solid #f0f0f0', paddingBottom: '3rem' }}>
                    &copy; {new Date().getFullYear()} Management Systems (MSS). All rights reserved.
                </div>
            </footer>

            <style jsx>{`
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
                .mobile-menu a { font-size: 1.2rem; font-weight: 700; text-align: center; text-decoration: none; color: #333; }
            `}</style>
        </div>
    );
}
