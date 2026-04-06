'use client';

import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { motion } from 'framer-motion';
import { 
    ArrowLeft, 
    Box, 
    ChevronRight, 
    Package, 
    Zap, 
    Database, 
    BarChart3, 
    Truck, 
    LayoutDashboard, 
    Printer, 
    ScanBarcode, 
    ShieldCheck, 
    RefreshCw, 
    Settings, 
    Activity, 
    FileText 
} from 'lucide-react';

export default function ReleaseNotesPage() {
    const params = useParams();
    const router = useRouter();
    const version = params.version || '2.0.0';

    const changes = [
        {
            title: "Supplier & Procurement Management",
            icon: <Truck size={24} className="text-blue-600" />,
            description: "Full lifecycle procurement including PO generation, supplier rating, and on-time performance tracking.",
            features: ["PO Number Auto-generation", "Purchase Order Status Badges", "Supplier Performance Scorecard"]
        },
        {
            title: "Multi-Location Control",
            icon: <Box size={24} className="text-emerald-600" />,
            description: "Manage stock across warehouses, retail stores, and mobile vans from a single interface.",
            features: ["Stock Transfer Workflows", "Location-Specific Reorder Points", "Consolidated Stock Reports"]
        },
        {
            title: "Advanced Analytics & BI",
            icon: <BarChart3 size={24} className="text-purple-600" />,
            description: "Deep insights into your inventory health using ABC/XYZ classification and Pareto (80/20) rules.",
            features: ["Cohort Analysis", "Stockout Risk Alerts", "Inventory Health Score (0-100)"]
        },
        {
            title: "Intelligent Forecasting",
            icon: <Zap size={24} className="text-amber-600" />,
            description: "AI-driven demand prediction that calculates daily velocity and days-until-stockout.",
            features: ["EOQ Calculation", "Seasonal Trend Analysis", "Automated Safety Stock Management"]
        },
        {
            title: "Proactive Morning Briefing",
            icon: <LayoutDashboard size={24} className="text-emerald-600" />,
            description: "Start your day with a prioritized feed of action items: expiring stock, stockouts, and sales trends.",
            features: ["Daily To-Do Feed", "Top Performer Alert", "Overdue Delivery Reminders"]
        },
        {
            title: "Hardware Orchestration",
            icon: <Printer size={24} className="text-slate-600" />,
            description: "Native support for professional hardware for high-speed POS and inventory workflows.",
            features: ["ESC/POS Thermal Printing", "High-speed HID Barcode Scanning", "Custom Label Generation"]
        },
        {
            title: "Enterprise Governance",
            icon: <ShieldCheck size={24} className="text-indigo-600" />,
            description: "Granular Role-Based Access Control (RBAC) to ensure your data stays secure and staff stay focused.",
            features: ["Audit Trail & Timber Logs", "Custom User Permissions", "Role Presets (Admin, Staff, Guest)"]
        }
    ];

    return (
        <div className="release-page">
            <header className="release-header">
                <div className="container header-container">
                    <Link href="/" className="back-link">
                        <ArrowLeft size={18} />
                        <span>Back to Home</span>
                    </Link>
                    <div className="logo-small">
                        <Box size={24} className="icon-blue" />
                        <span>IMS Ready</span>
                    </div>
                </div>
            </header>

            <main className="container">
                <motion.div 
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.5 }}
                    className="hero-release"
                >
                    <div className="badge-v2">RELEASE VERSION {version}</div>
                    <h1 className="main-title">The Enterprise Upgrade.</h1>
                    <p className="main-subtitle">
                        Version {version} transforms IMS from a functional tool into an enterprise-grade ERP system. 
                        Powering over {version === '2.0.0' ? '50,000' : '10,000'} monthly transactions globally.
                    </p>
                </motion.div>

                <div className="features-section">
                    <h2 className="group-title">Key Core Changes</h2>
                    <div className="feature-grid-v2">
                        {changes.map((change, idx) => (
                            <motion.div 
                                key={idx}
                                initial={{ opacity: 0, scale: 0.95 }}
                                whileInView={{ opacity: 1, scale: 1 }}
                                viewport={{ once: true }}
                                transition={{ delay: idx * 0.1 }}
                                className="change-card"
                            >
                                <div className="card-header">
                                    {change.icon}
                                    <h3>{change.title}</h3>
                                </div>
                                <p className="card-desc">{change.description}</p>
                                <ul className="card-features">
                                    {change.features.map((f, i) => (
                                        <li key={i}><ChevronRight size={14} /> {f}</li>
                                    ))}
                                </ul>
                            </motion.div>
                        ))}
                    </div>
                </div>

                <motion.div 
                    initial={{ opacity: 0 }}
                    whileInView={{ opacity: 1 }}
                    className="cta-section-v2"
                >
                    <div className="cta-content">
                        <h3>Ready to upgrade your business?</h3>
                        <p>Activate the Enterprise tier today and unlock the full power of advanced automation.</p>
                        <div className="cta-buttons">
                            <button onClick={() => router.push('/?auth=register')} className="btn-v2 btn-primary-v2">
                                Request Enterprise Trial
                            </button>
                            <button onClick={() => router.push('/#contact')} className="btn-v2 btn-outline-v2">
                                Talk to an Expert
                            </button>
                        </div>
                    </div>
                </motion.div>
            </main>

            <footer className="release-footer">
                <div className="container">
                    <p>&copy; {new Date().getFullYear()} IMS ERP Solutions. Built for African Enterprises.</p>
                </div>
            </footer>

            <style jsx>{`
                .release-page {
                    min-height: 100vh;
                    background-color: #f8fafc;
                    font-family: 'Inter', -apple-system, system-ui, sans-serif;
                    color: #1e293b;
                    padding-bottom: 5rem;
                }
                .container {
                    max-width: 1100px;
                    margin: 0 auto;
                    padding: 0 1.5rem;
                }
                .release-header {
                    background: #fff;
                    border-bottom: 1px solid #e2e8f0;
                    position: sticky;
                    top: 0;
                    z-index: 100;
                    padding: 1rem 0;
                }
                .header-container {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }
                .back-link {
                    display: flex;
                    align-items: center;
                    gap: 8px;
                    color: #64748b;
                    text-decoration: none;
                    font-weight: 500;
                    font-size: 0.95rem;
                    transition: color 0.2s;
                }
                .back-link:hover { color: #020617; }
                .logo-small {
                    display: flex;
                    align-items: center;
                    gap: 10px;
                    font-weight: 700;
                    font-size: 1.1rem;
                }
                .icon-blue { color: #2563eb; }

                .hero-release {
                    text-align: center;
                    padding: 6rem 0 4rem;
                }
                .badge-v2 {
                    display: inline-block;
                    background: #f1f5f9;
                    color: #0f172a;
                    padding: 0.5rem 1rem;
                    border-radius: 50px;
                    font-size: 0.75rem;
                    font-weight: 700;
                    letter-spacing: 0.05em;
                    margin-bottom: 2rem;
                    border: 1px solid #e2e8f0;
                }
                .main-title {
                    font-size: 4rem;
                    font-weight: 900;
                    letter-spacing: -0.04em;
                    margin-bottom: 1.5rem;
                    line-height: 1;
                }
                .main-subtitle {
                    color: #475569;
                    font-size: 1.25rem;
                    max-width: 700px;
                    margin: 0 auto;
                    line-height: 1.6;
                }

                .features-section {
                    margin-top: 4rem;
                }
                .group-title {
                    font-size: 1.5rem;
                    font-weight: 800;
                    margin-bottom: 3rem;
                    text-align: center;
                    position: relative;
                }
                .group-title::after {
                    content: '';
                    position: absolute;
                    bottom: -15px;
                    left: 50%;
                    transform: translateX(-50%);
                    width: 40px;
                    height: 4px;
                    background: #2563eb;
                    border-radius: 2px;
                }

                .feature-grid-v2 {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
                    gap: 2rem;
                }
                .change-card {
                    background: #fff;
                    padding: 2.5rem;
                    border-radius: 24px;
                    border: 1px solid #e2e8f0;
                    transition: all 0.3s ease;
                    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
                }
                .change-card:hover {
                    box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
                    border-color: #cbd5e1;
                    transform: translateY(-4px);
                }
                .card-header {
                    display: flex;
                    align-items: center;
                    gap: 15px;
                    margin-bottom: 1.5rem;
                }
                .card-header h3 {
                    font-size: 1.25rem;
                    font-weight: 700;
                    letter-spacing: -0.01em;
                }
                .card-desc {
                    color: #64748b;
                    line-height: 1.6;
                    margin-bottom: 2rem;
                    font-size: 0.95rem;
                }
                .card-features {
                    list-style: none;
                    display: flex;
                    flex-direction: column;
                    gap: 10px;
                }
                .card-features li {
                    display: flex;
                    align-items: center;
                    gap: 10px;
                    font-size: 0.9rem;
                    font-weight: 600;
                    color: #334155;
                }

                .cta-section-v2 {
                    margin-top: 8rem;
                    background: #020617;
                    border-radius: 32px;
                    padding: 5rem 3rem;
                    text-align: center;
                    color: #fff;
                }
                .cta-content h3 {
                    font-size: 2.5rem;
                    font-weight: 800;
                    margin-bottom: 1.5rem;
                    letter-spacing: -0.02em;
                }
                .cta-content p {
                    color: #94a3b8;
                    font-size: 1.1rem;
                    margin-bottom: 3rem;
                    max-width: 500px;
                    margin-left: auto;
                    margin-right: auto;
                }
                .cta-buttons {
                    display: flex;
                    gap: 1.5rem;
                    justify-content: center;
                }
                .btn-v2 {
                    padding: 1rem 2rem;
                    border-radius: 12px;
                    font-weight: 700;
                    font-size: 1rem;
                    cursor: pointer;
                    transition: all 0.2s;
                }
                .btn-primary-v2 {
                    background: #fff;
                    color: #020617;
                    border: none;
                }
                .btn-primary-v2:hover {
                    background: #f1f5f9;
                    transform: scale(1.02);
                }
                .btn-outline-v2 {
                    background: transparent;
                    color: #fff;
                    border: 1px solid #334155;
                }
                .btn-outline-v2:hover {
                    background: rgba(255,255,255,0.05);
                    border-color: #475569;
                }

                .release-footer {
                    margin-top: 8rem;
                    text-align: center;
                    padding-bottom: 2rem;
                    color: #94a3b8;
                    font-size: 0.85rem;
                }

                @media (max-width: 768px) {
                    .main-title { font-size: 2.5rem; }
                    .cta-buttons { flex-direction: column; }
                    .feature-grid-v2 { grid-template-columns: 1fr; }
                    .cta-content h3 { font-size: 1.8rem; }
                    .hero-release { padding: 4rem 0 2rem; }
                }
            `}</style>
        </div>
    );
}
