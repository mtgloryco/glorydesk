'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import {
    Shield, Smartphone, Zap, CloudOff, Package, Check,
    ArrowRight, Menu, X, Laptop, Database, RefreshCw,
    Mail, Send, Box, Calendar, Clock, Download, Star,
    FolderOpen, Search, FileText, Layers, SmartphoneIcon,
    AlertTriangle, Sparkles, TrendingUp
} from 'lucide-react';
import { LICENSE_CONFIG } from '@/lib/config';
import LoginModal from '@/components/LoginModal';
import RegisterModal from '@/components/RegisterModal';
import DemoRequestModal from '@/components/DemoRequestModal';

export default function LandingPage() {
    const [isMenuOpen, setIsMenuOpen] = useState(false);
    const [showLogin, setShowLogin] = useState(false);
    const [showRegister, setShowRegister] = useState(false);
    const [showDemo, setShowDemo] = useState(false);
    const [scrolled, setScrolled] = useState(false);
    const [plans, setPlans] = useState([]);

    const [contactName, setContactName] = useState('');
    const [contactEmail, setContactEmail] = useState('');
    const [contactMsg, setContactMsg] = useState('');
    const [sending, setSending] = useState(false);
    const [sent, setSent] = useState(false);

    const [downloads, setDownloads] = useState([]);
    const [showHistory, setShowHistory] = useState(false);

    useEffect(() => {
        const handleScroll = () => setScrolled(window.scrollY > 50);
        window.addEventListener('scroll', handleScroll);

        const params = new URLSearchParams(window.location.search);
        if (params.get('auth') === 'login') setShowLogin(true);
        if (params.get('auth') === 'register') setShowRegister(true);

        fetch('/api/plans')
            .then(res => res.json())
            .then(data => {
                if (Array.isArray(data)) setPlans(data);
            })
            .catch(err => console.error('Failed to load plans:', err));

        fetch('/api/downloads')
            .then(res => res.json())
            .then(data => {
                if (Array.isArray(data)) setDownloads(data);
            })
            .catch(err => console.error('Failed to load downloads:', err));

        return () => window.removeEventListener('scroll', handleScroll);
    }, []);

    const toggleMenu = () => setIsMenuOpen(!isMenuOpen);
    const openLogin = () => { setShowLogin(true); setShowRegister(false); setIsMenuOpen(false); };
    const openRegister = () => { setShowRegister(true); setShowLogin(false); setIsMenuOpen(false); };

    const staticPlans = Object.entries(LICENSE_CONFIG.plans).map(([key, plan]) => ({
        id: key,
        ...plan,
        features: plan.tier === 'Enterprise'
            ? ['Real Cloud Sync', 'Global Audit Trail', 'Auto-Reorder Workflows', 'Custom API Integrations', 'Priority Support']
            : plan.tier === 'Pro'
                ? ['Intelligent Forecasting', 'Advanced BI Analytics', 'Full Reporting Engine', 'Kitting & Bundles', 'Unlimited Locations']
                : plan.tier === 'Medium'
                    ? ['Smart POS & Receipts', 'Supplier Procurement', 'Expiry Tracking', 'Multi-location (Up to 3)', 'Returns Management']
                    : ['Inventory Notebook', 'Stock Tracking', 'Local Backups', 'Max 50 Products', 'Single Location Only']
    }));

    const displayPlans = plans.length > 0 ? plans : staticPlans;
    const featuredDownloads = downloads.filter(d => d.isFeatured);

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
        } catch (e) {
            alert('Failed to send message');
        } finally {
            setSending(false);
        }
    };

    const handleDownload = async (download) => {
        try {
            await fetch('/api/downloads/track', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: download._id })
            });
        } catch (e) {
            console.error('Tracking failed', e);
        }
        window.open(download.link, '_blank');
        setDownloads(prev => prev.map(d =>
            d._id === download._id
                ? { ...d, downloadCount: (d.downloadCount || 0) + 1 }
                : d
        ));
    };

    return (
        <div className="min-h-screen bg-slate-50 text-slate-800 selection:bg-amber-200 selection:text-amber-950 font-sans antialiased overflow-x-hidden">
            {/* Header */}
            <nav className={`fixed top-0 left-0 right-0 z-50 transition-all duration-300 ${scrolled ? 'bg-white/90 backdrop-blur-md shadow-md py-3 border-b border-slate-100' : 'bg-transparent py-5'}`}>
                <div className="max-w-7xl mx-auto px-6 flex justify-between items-center">
                    <Link href="/" className="flex items-center gap-2 group">
                        <div className="w-10 h-10 rounded-xl bg-amber-500 flex items-center justify-center text-white shadow-md shadow-amber-500/20 group-hover:scale-105 transition-transform duration-200">
                            <Box className="w-6 h-6" />
                        </div>
                        <span className="font-extrabold text-xl tracking-tight text-slate-900 group-hover:text-amber-600 transition-colors duration-200">
                            MT GLORY <span className="text-amber-500">IMS</span>
                        </span>
                    </Link>

                    <div className="hidden lg:flex items-center gap-8">
                        <a href="#features" className="text-slate-600 hover:text-amber-600 font-semibold transition-colors duration-150">Features</a>
                        <a href="#downloads" className="text-slate-600 hover:text-amber-600 font-semibold transition-colors duration-150">Downloads</a>
                        <a href="#pricing" className="text-slate-600 hover:text-amber-600 font-semibold transition-colors duration-150">Pricing</a>
                        <Link href="/about" className="text-slate-600 hover:text-amber-600 font-semibold transition-colors duration-150">About Us</Link>
                        <a href="#contact" className="text-slate-600 hover:text-amber-600 font-semibold transition-colors duration-150">Contact</a>
                    </div>

                    <div className="hidden lg:flex items-center gap-4">
                        <button onClick={openLogin} className="text-slate-700 hover:text-amber-600 font-bold px-4 py-2 rounded-lg transition-colors">
                            Sign In
                        </button>
                        <button onClick={openRegister} className="bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold px-6 py-2.5 rounded-full shadow-lg shadow-amber-500/20 hover:shadow-amber-500/35 hover:-translate-y-0.5 active:translate-y-0 transition-all duration-150">
                            Start Free Trial
                        </button>
                    </div>

                    <button className="lg:hidden text-slate-800 hover:text-amber-600 focus:outline-none" onClick={toggleMenu}>
                        {isMenuOpen ? <X className="w-8 h-8" /> : <Menu className="w-8 h-8" />}
                    </button>
                </div>

                {isMenuOpen && (
                    <div className="lg:hidden absolute top-full left-0 right-0 bg-white border-b border-slate-100 shadow-xl py-6 px-6 flex flex-col gap-4 animate-fadeIn">
                        <a href="#features" onClick={toggleMenu} className="text-slate-700 font-bold hover:text-amber-600 py-1">Features</a>
                        <a href="#downloads" onClick={toggleMenu} className="text-slate-700 font-bold hover:text-amber-600 py-1">Downloads</a>
                        <a href="#pricing" onClick={toggleMenu} className="text-slate-700 font-bold hover:text-amber-600 py-1">Pricing</a>
                        <Link href="/about" onClick={toggleMenu} className="text-slate-700 font-bold hover:text-amber-600 py-1">About Us</Link>
                        <a href="#contact" onClick={toggleMenu} className="text-slate-700 font-bold hover:text-amber-600 py-1">Contact</a>
                        <hr className="border-slate-100 my-2" />
                        <button onClick={openLogin} className="text-slate-700 font-bold hover:text-amber-600 py-2 text-center rounded-lg border border-slate-200">
                            Sign In
                        </button>
                        <button onClick={openRegister} className="bg-amber-500 hover:bg-amber-600 text-white font-bold py-3 text-center rounded-lg shadow-md shadow-amber-500/20">
                            Start Free Trial
                        </button>
                    </div>
                )}
            </nav>

            {/* Hero Section */}
            <header className="relative pt-36 pb-24 overflow-hidden bg-gradient-to-b from-amber-50/50 via-white to-slate-50">
                <div className="absolute top-0 right-0 w-[500px] h-[500px] bg-amber-200/10 rounded-full blur-3xl pointer-events-none" />
                <div className="absolute -left-20 top-40 w-[300px] h-[300px] bg-amber-100/25 rounded-full blur-2xl pointer-events-none" />

                <div className="max-w-7xl mx-auto px-6 flex flex-col items-center text-center relative z-10">
                    {/* Badge Awards Row */}
                    <div className="flex items-center gap-1.5 bg-amber-50 border border-amber-200/60 rounded-full px-4 py-1.5 mb-8 animate-fadeIn">
                        <div className="flex gap-0.5 text-amber-500">
                            {[...Array(5)].map((_, i) => <Star key={i} className="w-4 h-4 fill-amber-500 text-amber-500" />)}
                        </div>
                        <span className="text-xs font-extrabold text-amber-800 tracking-wide uppercase">
                            Rated 4.8/5 on G2 & Capterra
                        </span>
                    </div>

                    <h1 className="text-4xl sm:text-5xl md:text-6xl font-extrabold text-slate-900 tracking-tight leading-[1.1] max-w-4xl mb-6 font-sans">
                        Simple, premium <br className="hidden sm:inline" />
                        <span className="text-transparent bg-clip-text bg-gradient-to-r from-amber-500 via-amber-600 to-amber-700">
                            inventory management
                        </span> <br />
                        software for small business.
                    </h1>

                    <p className="text-lg text-slate-600 max-w-2xl leading-relaxed mb-10">
                        The ultimate offline-first tool to track supplies, materials, tools, and assets. Fast machine-bound RSA keys, advanced barcode scanner support, and real-time cloud sync.
                    </p>

                    <div className="flex flex-col sm:flex-row items-center justify-center gap-4 w-full sm:w-auto mb-16">
                        <button onClick={openRegister} className="w-full sm:w-auto bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold text-lg px-8 py-4 rounded-full shadow-xl shadow-amber-500/20 hover:shadow-amber-500/40 hover:-translate-y-0.5 active:translate-y-0 transition-all duration-150 flex items-center justify-center gap-2">
                            Start Free Trial <ArrowRight className="w-5 h-5" />
                        </button>
                        <button onClick={() => setShowDemo(true)} className="w-full sm:w-auto bg-white hover:bg-slate-50 text-slate-700 border border-slate-200 font-bold text-lg px-8 py-4 rounded-full shadow-sm hover:shadow transition-all duration-150 flex items-center justify-center gap-2">
                            <Clock className="w-5 h-5 text-amber-500" /> Try Live Demo
                        </button>
                        <a href="#pricing" className="w-full sm:w-auto text-amber-700 hover:text-amber-850 font-bold text-lg px-4 py-2 hover:underline text-center">
                            See All Plans
                        </a>
                    </div>

                    {/* Dashboard Mockup Video Player Frame */}
                    <div className="w-full max-w-5xl rounded-2xl bg-slate-950 p-3 shadow-2xl shadow-slate-900/30 border border-slate-800/80 group relative">
                        <div className="absolute inset-0 bg-gradient-to-tr from-amber-500/10 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300 rounded-2xl pointer-events-none" />
                        
                        <div className="relative aspect-[16/9] w-full rounded-xl overflow-hidden bg-slate-900 flex flex-col">
                            {/* User customizable placeholder image */}
                            <img 
                                src="/images/mockup-hero.png" 
                                alt="Dashboard Mockup" 
                                className="w-full h-full object-cover absolute inset-0 opacity-80 mix-blend-normal group-hover:scale-[1.01] transition-transform duration-700" 
                                onError={(e) => {
                                    e.currentTarget.style.display = 'none';
                                }}
                            />

                            {/* Fallback CSS high-fidelity interactive dashboard mockup */}
                            <div className="flex-1 flex flex-col text-left font-sans text-xs text-slate-400 select-none p-4">
                                {/* Dashboard Top Bar */}
                                <div className="flex items-center justify-between pb-3 border-b border-slate-800/80 mb-4">
                                    <div className="flex items-center gap-2">
                                        <span className="w-3 h-3 rounded-full bg-slate-800 flex items-center justify-center"><Box className="w-2 h-2 text-amber-500" /></span>
                                        <span className="font-bold text-slate-200 text-sm">Dashboard Overview</span>
                                    </div>
                                    <div className="flex items-center gap-3">
                                        <span className="px-2 py-0.5 rounded bg-slate-800 text-slate-300 font-medium">v2.0.0 Pro Active</span>
                                        <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                                    </div>
                                </div>

                                {/* Main Dashboard Columns */}
                                <div className="grid grid-cols-4 gap-4 flex-1">
                                    {/* Sidebar */}
                                    <div className="col-span-1 border-r border-slate-800/50 pr-4 flex flex-col gap-2.5">
                                        <div className="p-2 rounded bg-amber-500/10 text-amber-400 font-bold flex items-center gap-2"><FolderOpen className="w-3.5 h-3.5" /> All Folders</div>
                                        <div className="p-2 rounded hover:bg-slate-800/50 text-slate-400 flex items-center gap-2"><Layers className="w-3.5 h-3.5" /> Categories</div>
                                        <div className="p-2 rounded hover:bg-slate-800/50 text-slate-400 flex items-center gap-2"><TrendingUp className="w-3.5 h-3.5" /> Analytics</div>
                                        <div className="p-2 rounded hover:bg-slate-800/50 text-slate-400 flex items-center gap-2"><FileText className="w-3.5 h-3.5" /> Reports</div>
                                        <div className="mt-auto p-2 rounded bg-slate-800/80 flex items-center gap-2"><Database className="w-3.5 h-3.5 text-emerald-400" /> Offline DB: 9.8MB</div>
                                    </div>

                                    {/* Content Grid */}
                                    <div className="col-span-3 flex flex-col gap-4">
                                        {/* Status Cards */}
                                        <div className="grid grid-cols-3 gap-3">
                                            <div className="p-3.5 rounded-xl bg-slate-800/60 border border-slate-800/80">
                                                <span className="block text-slate-500 mb-1">TOTAL VALUATION</span>
                                                <span className="text-lg font-bold text-slate-100">$48,250.00</span>
                                            </div>
                                            <div className="p-3.5 rounded-xl bg-slate-800/60 border border-slate-800/80">
                                                <span className="block text-slate-500 mb-1">UNIQUE ITEMS</span>
                                                <span className="text-lg font-bold text-slate-100">1,248 Units</span>
                                            </div>
                                            <div className="p-3.5 rounded-xl bg-amber-500/10 border border-amber-500/20">
                                                <span className="block text-amber-500 mb-1 font-bold flex items-center gap-1"><AlertTriangle className="w-3 h-3 text-amber-500" /> LOW STOCK ALERT</span>
                                                <span className="text-lg font-bold text-amber-400">4 Items Left</span>
                                            </div>
                                        </div>

                                        {/* Dynamic items table */}
                                        <div className="flex-1 bg-slate-800/30 rounded-xl border border-slate-800/80 p-3 flex flex-col">
                                            <div className="flex justify-between items-center mb-3">
                                                <span className="font-bold text-slate-300">Recent Movements Log</span>
                                                <span className="text-amber-500 text-[10px] hover:underline cursor-pointer">View full audit trail &rarr;</span>
                                            </div>
                                            <div className="flex-1 flex flex-col gap-2">
                                                <div className="flex items-center justify-between p-2 rounded bg-slate-800/40 border border-slate-800/50">
                                                    <div className="flex items-center gap-2">
                                                        <span className="w-2 h-2 rounded-full bg-amber-50" />
                                                        <span className="font-semibold text-slate-200">Milwaukee Heavy Drill M18</span>
                                                    </div>
                                                    <span className="text-slate-500">Box #42 (Warehouse A)</span>
                                                    <span className="px-2 py-0.5 rounded bg-emerald-500/15 text-emerald-400">+12 Stock In</span>
                                                </div>
                                                <div className="flex items-center justify-between p-2 rounded bg-slate-800/40 border border-slate-800/50">
                                                    <div className="flex items-center gap-2">
                                                        <span className="w-2 h-2 rounded-full bg-amber-50" />
                                                        <span className="font-semibold text-slate-200">Cat-6 Premium Cable 100m</span>
                                                    </div>
                                                    <span className="text-slate-500">Shelf C-3 (Supplies Closet)</span>
                                                    <span className="px-2 py-0.5 rounded bg-amber-500/15 text-amber-400">-5 Stock Out</span>
                                                </div>
                                                <div className="flex items-center justify-between p-2 rounded bg-slate-800/40 border border-slate-800/50">
                                                    <div className="flex items-center gap-2">
                                                        <span className="w-2 h-2 rounded-full bg-amber-50" />
                                                        <span className="font-semibold text-slate-200">Anker PowerPort USB-C Hub</span>
                                                    </div>
                                                    <span className="text-slate-500">Cabin #12 (Sales Desk)</span>
                                                    <span className="px-2 py-0.5 rounded bg-rose-500/15 text-rose-400">Zero Alert</span>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            {/* Floating gradient play button overlay */}
                            <div className="absolute inset-0 flex items-center justify-center bg-slate-900/30 backdrop-blur-[1px] hover:backdrop-blur-none transition-all duration-300">
                                <button className="w-20 h-20 rounded-full bg-amber-500 text-white flex items-center justify-center shadow-2xl shadow-amber-500/50 hover:scale-110 active:scale-95 transition-transform duration-200">
                                    <div className="w-0 h-0 border-t-8 border-t-transparent border-l-[16px] border-l-white border-b-8 border-b-transparent ml-1.5" />
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </header>

            {/* Premium Features Section */}
            <section id="features" className="py-24 bg-white border-y border-slate-100">
                <div className="max-w-7xl mx-auto px-6">
                    {/* Section Header */}
                    <div className="text-center max-w-3xl mx-auto mb-20">
                        <span className="text-xs font-bold uppercase tracking-wider text-amber-600 bg-amber-50 border border-amber-200 px-3.5 py-1.5 rounded-full inline-block mb-4">
                            Premium Advantage
                        </span>
                        <h2 className="text-3xl sm:text-4xl md:text-5xl font-extrabold text-slate-900 tracking-tight mb-4">
                            The advanced offline-first inventory advantage.
                        </h2>
                        <p className="text-lg text-slate-600">
                            Our system offers cutting-edge control systems designed to simplify tracking at scale.
                        </p>
                    </div>

                    {/* FEATURE ROW 1 - ORGANIZING */}
                    <div className="grid lg:grid-cols-12 gap-12 lg:gap-16 items-center mb-24 lg:mb-32">
                        <div className="lg:col-span-5 flex flex-col gap-6 order-2 lg:order-1">
                            <div className="flex items-center gap-2 bg-amber-50 border border-amber-200 rounded-full px-3 py-1 w-fit">
                                <FolderOpen className="w-4 h-4 text-amber-500" />
                                <span className="text-xs font-extrabold text-amber-800 tracking-wide uppercase">Organizing</span>
                            </div>
                            <h3 className="text-2xl sm:text-3xl font-extrabold text-slate-900 leading-tight">
                                Organize and automate inventory at the touch of a button.
                            </h3>
                            <div className="flex flex-col gap-4">
                                <div className="flex gap-3">
                                    <div className="w-6 h-6 rounded-full bg-amber-100 flex items-center justify-center text-amber-600 shrink-0 mt-0.5">
                                        <Check className="w-4 h-4 font-bold" />
                                    </div>
                                    <p className="text-slate-600"><strong className="text-slate-950 block">Bulk Import Lists:</strong> Easily upload your existing inventory database via clean Excel or CSV scripts instantly.</p>
                                </div>
                                <div className="flex gap-3">
                                    <div className="w-6 h-6 rounded-full bg-amber-100 flex items-center justify-center text-amber-600 shrink-0 mt-0.5">
                                        <Check className="w-4 h-4 font-bold" />
                                    </div>
                                    <p className="text-slate-600"><strong className="text-slate-950 block">Custom Tags & Folders:</strong> Group items based on precise physical locations, warehouse zones, departments, or custom category systems.</p>
                                </div>
                                <div className="flex gap-3">
                                    <div className="w-6 h-6 rounded-full bg-amber-100 flex items-center justify-center text-amber-600 shrink-0 mt-0.5">
                                        <Check className="w-4 h-4 font-bold" />
                                    </div>
                                    <p className="text-slate-600"><strong className="text-slate-950 block">Rich Metadata:</strong> Add custom fields like purchase batch codes, supplier data, item photos, and expiry dates.</p>
                                </div>
                            </div>
                            <div className="pt-2 flex items-center gap-4">
                                <button onClick={openRegister} className="bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold px-6 py-3 rounded-full shadow-lg shadow-amber-500/20 hover:shadow-amber-500/35 hover:-translate-y-0.5 active:translate-y-0 transition-all duration-150">
                                    Start Free Trial
                                </button>
                                <a href="#pricing" className="text-slate-700 hover:text-amber-600 font-bold flex items-center gap-1 group">
                                    See Plans <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" />
                                </a>
                            </div>
                        </div>

                        {/* Visual Mockup Column */}
                        <div className="lg:col-span-7 order-1 lg:order-2">
                            <div className="bg-slate-100 border border-slate-200/80 rounded-2xl p-4 shadow-xl shadow-slate-100/50 group relative overflow-hidden aspect-[4/3] flex items-center justify-center">
                                <img 
                                    src="/images/mockup-organizing.png" 
                                    alt="Organizing Mockup" 
                                    className="w-full h-full object-cover rounded-xl shadow border border-slate-200"
                                    onError={(e) => e.currentTarget.style.display = 'none'}
                                />
                                
                                {/* Fallback Interactive CSS Mockup */}
                                <div className="absolute inset-0 w-full h-full bg-white rounded-xl shadow border border-slate-200/80 p-6 flex flex-col justify-between">
                                    <div className="flex justify-between items-center pb-4 border-b border-slate-100">
                                        <div className="flex items-center gap-2">
                                            <span className="w-2.5 h-2.5 rounded-full bg-amber-500" />
                                            <span className="font-bold text-slate-800 text-sm">Organize Folders</span>
                                        </div>
                                        <div className="text-xs text-slate-500">4 Active Storage Vaults</div>
                                    </div>
                                    <div className="grid grid-cols-2 gap-4 my-4 flex-1 items-center">
                                        <div className="p-4 rounded-xl bg-gradient-to-tr from-amber-50 to-amber-100/30 border border-amber-200/50 shadow-sm hover:scale-[1.02] transition-transform">
                                            <FolderOpen className="w-8 h-8 text-amber-600 mb-2" />
                                            <h4 className="font-bold text-slate-900">Warehouse Alpha</h4>
                                            <span className="text-xs text-slate-500">842 Products Listed</span>
                                        </div>
                                        <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 shadow-sm hover:scale-[1.02] transition-transform">
                                            <FolderOpen className="w-8 h-8 text-slate-600 mb-2" />
                                            <h4 className="font-bold text-slate-900">Retail Outlet B</h4>
                                            <span className="text-xs text-slate-500">208 Products Listed</span>
                                        </div>
                                        <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 shadow-sm hover:scale-[1.02] transition-transform">
                                            <FolderOpen className="w-8 h-8 text-slate-600 mb-2" />
                                            <h4 className="font-bold text-slate-900">Mobile Van Stock</h4>
                                            <span className="text-xs text-slate-500">140 Products Listed</span>
                                        </div>
                                        <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 shadow-sm hover:scale-[1.02] transition-transform">
                                            <FolderOpen className="w-8 h-8 text-slate-600 mb-2" />
                                            <h4 className="font-bold text-slate-900">Main Office Spares</h4>
                                            <span className="text-xs text-slate-500">58 Products Listed</span>
                                        </div>
                                    </div>
                                    <div className="p-3 bg-amber-50 rounded-lg text-xs border border-amber-200/50 text-amber-800 flex items-center justify-between">
                                        <span>Need custom tagging for inventory locations?</span>
                                        <span className="font-bold cursor-pointer hover:underline">Get Custom Setup &arr;</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* FEATURE ROW 2 - MANAGING & SCANNING */}
                    <div className="grid lg:grid-cols-12 gap-12 lg:gap-16 items-center mb-24 lg:mb-32">
                        {/* Visual Mockup Column First */}
                        <div className="lg:col-span-7">
                            <div className="bg-slate-100 border border-slate-200/80 rounded-2xl p-4 shadow-xl shadow-slate-100/50 group relative overflow-hidden aspect-[4/3] flex items-center justify-center">
                                <img 
                                    src="/images/mockup-tracking.png" 
                                    alt="Tracking Mockup" 
                                    className="w-full h-full object-cover rounded-xl shadow border border-slate-200"
                                    onError={(e) => e.currentTarget.style.display = 'none'}
                                />
                                
                                {/* Fallback Interactive CSS Mockup */}
                                <div className="absolute inset-0 w-full h-full bg-white rounded-xl shadow border border-slate-200/80 p-6 flex flex-col justify-between">
                                    <div className="flex justify-between items-center pb-4 border-b border-slate-100">
                                        <div className="flex items-center gap-2">
                                            <SmartphoneIcon className="w-5 h-5 text-amber-500" />
                                            <span className="font-bold text-slate-800 text-sm">Item Digital Profile</span>
                                        </div>
                                        <span className="px-2.5 py-1 rounded-full bg-emerald-50 text-emerald-700 font-extrabold text-[10px]">IN STOCK</span>
                                    </div>
                                    
                                    <div className="flex-1 flex gap-6 items-center my-6">
                                        {/* Product Photo Box */}
                                        <div className="w-1/3 aspect-square rounded-xl bg-slate-100 border border-slate-200 flex flex-col items-center justify-center text-slate-400 relative overflow-hidden">
                                            <Package className="w-12 h-12 text-slate-300" />
                                            <span className="text-[10px] mt-2 block text-slate-500 font-bold">Image Attached</span>
                                        </div>
                                        
                                        {/* Product Identifiers */}
                                        <div className="flex-1 flex flex-col gap-2.5">
                                            <span className="text-[10px] font-bold text-amber-600 tracking-wider">ELECTRONIC TOOLS</span>
                                            <h4 className="font-bold text-slate-900 text-lg leading-tight">Milwaukee Drill Driver M18</h4>
                                            <div className="text-slate-500 text-xs flex flex-col gap-1">
                                                <span>SKU: <strong className="text-slate-700">MIL-M18-HD</strong></span>
                                                <span>Location: <strong className="text-slate-700">Warehouse Alpha (Box 12)</strong></span>
                                            </div>
                                        </div>
                                    </div>

                                    {/* Barcodes / Scan UI */}
                                    <div className="grid grid-cols-2 gap-4 pt-4 border-t border-slate-100">
                                        <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl flex flex-col items-center justify-center">
                                            <div className="w-full h-8 flex items-center justify-center gap-0.5 text-slate-800 mb-1 select-none font-mono tracking-widest text-[10px] bg-white border border-slate-200/50">
                                                ||||| | |||| || |
                                            </div>
                                            <span className="text-[10px] text-slate-500">Linked Barcode</span>
                                        </div>
                                        <div className="p-3 bg-slate-50 border border-slate-200 rounded-xl flex items-center gap-3">
                                            <div className="w-8 h-8 rounded bg-slate-900 flex items-center justify-center shrink-0">
                                                <div className="w-6 h-6 border-2 border-white rounded-sm bg-slate-950 flex flex-wrap gap-0.5 p-0.5">
                                                    {[...Array(4)].map((_, i) => <div key={i} className="w-2.5 h-2.5 bg-white" />)}
                                                </div>
                                            </div>
                                            <div className="flex flex-col">
                                                <span className="text-xs font-bold text-slate-800">Dynamic QR</span>
                                                <span className="text-[9px] text-slate-500">Scan to Stock Out</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Content Column */}
                        <div className="lg:col-span-5 flex flex-col gap-6">
                            <div className="flex items-center gap-2 bg-amber-50 border border-amber-200 rounded-full px-3 py-1 w-fit">
                                <Search className="w-4 h-4 text-amber-500" />
                                <span className="text-xs font-extrabold text-amber-800 tracking-wide uppercase">Managing</span>
                            </div>
                            <h3 className="text-2xl sm:text-3xl font-extrabold text-slate-900 leading-tight">
                                Track inventory details in real time with ease.
                            </h3>
                            <div className="flex flex-col gap-4">
                                <div className="flex gap-3">
                                    <div className="w-6 h-6 rounded-full bg-amber-100 flex items-center justify-center text-amber-600 shrink-0 mt-0.5">
                                        <Check className="w-4 h-4 font-bold" />
                                    </div>
                                    <p className="text-slate-600"><strong className="text-slate-950 block">Built-in Barcode & QR Code:</strong> Speed up stock counting. Scan barcodes using any device camera to search, edit, or adjust stock counts instantly.</p>
                                </div>
                                <div className="flex gap-3">
                                    <div className="w-6 h-6 rounded-full bg-amber-100 flex items-center justify-center text-amber-600 shrink-0 mt-0.5">
                                        <Check className="w-4 h-4 font-bold" />
                                    </div>
                                    <p className="text-slate-600"><strong className="text-slate-950 block">High-Res Visual Verification:</strong> Attach photos to verify item conditions, identify parts quickly, and keep a visual audit log.</p>
                                </div>
                                <div className="flex gap-3">
                                    <div className="w-6 h-6 rounded-full bg-amber-100 flex items-center justify-center text-amber-600 shrink-0 mt-0.5">
                                        <Check className="w-4 h-4 font-bold" />
                                    </div>
                                    <p className="text-slate-600"><strong className="text-slate-950 block">Smart Expiry & Reorder:</strong> Configure dynamic min-stock thresholds to receive alerts when quantities drop or products approach expiry.</p>
                                </div>
                            </div>
                            <div className="pt-2 flex items-center gap-4">
                                <button onClick={openRegister} className="bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold px-6 py-3 rounded-full shadow-lg shadow-amber-500/20 hover:shadow-amber-500/35 hover:-translate-y-0.5 active:translate-y-0 transition-all duration-150">
                                    Start Free Trial
                                </button>
                                <button onClick={() => setShowDemo(true)} className="text-slate-700 hover:text-amber-600 font-bold flex items-center gap-1 group bg-transparent border-none">
                                    Request Demo &rarr;
                                </button>
                            </div>
                        </div>
                    </div>

                    {/* FEATURE ROW 3 - REPORTING & AUDITS */}
                    <div className="grid lg:grid-cols-12 gap-12 lg:gap-16 items-center mb-24 lg:mb-32">
                        <div className="lg:col-span-5 flex flex-col gap-6 order-2 lg:order-1">
                            <div className="flex items-center gap-2 bg-amber-50 border border-amber-200 rounded-full px-3 py-1 w-fit">
                                <FileText className="w-4 h-4 text-amber-500" />
                                <span className="text-xs font-extrabold text-amber-800 tracking-wide uppercase">Reporting</span>
                            </div>
                            <h3 className="text-2xl sm:text-3xl font-extrabold text-slate-900 leading-tight">
                                Get instant, real-time reporting insights.
                            </h3>
                            <div className="flex flex-col gap-4">
                                <div className="flex gap-3">
                                    <div className="w-6 h-6 rounded-full bg-amber-100 flex items-center justify-center text-amber-600 shrink-0 mt-0.5">
                                        <Check className="w-4 h-4 font-bold" />
                                    </div>
                                    <p className="text-slate-600"><strong className="text-slate-950 block">Audit-Ready History:</strong> Keep a detailed log of every quantity adjustment, purchase date, and stock handler with full timestamps.</p>
                                </div>
                                <div className="flex gap-3">
                                    <div className="w-6 h-6 rounded-full bg-amber-100 flex items-center justify-center text-amber-600 shrink-0 mt-0.5">
                                        <Check className="w-4 h-4 font-bold" />
                                    </div>
                                    <p className="text-slate-600"><strong className="text-slate-950 block">Export Custom PDF & CSV:</strong> Instantly generate reports filters by location, category, or movement status for easy tax filing and physical audits.</p>
                                </div>
                                <div className="flex gap-3">
                                    <div className="w-6 h-6 rounded-full bg-amber-100 flex items-center justify-center text-amber-600 shrink-0 mt-0.5">
                                        <Check className="w-4 h-4 font-bold" />
                                    </div>
                                    <p className="text-slate-600"><strong className="text-slate-950 block">Forecasting & Reordering:</strong> Leverage built-in algorithms to predict demand, calculate stock depletion trends, and print reorder manifests.</p>
                                </div>
                            </div>
                            <div className="pt-2 flex items-center gap-4">
                                <button onClick={openRegister} className="bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold px-6 py-3 rounded-full shadow-lg shadow-amber-500/20 hover:shadow-amber-500/35 hover:-translate-y-0.5 active:translate-y-0 transition-all duration-150">
                                    Start Free Trial
                                </button>
                                <a href="#contact" className="text-slate-700 hover:text-amber-600 font-bold">Contact Sales Desk</a>
                            </div>
                        </div>

                        {/* Visual Mockup Column */}
                        <div className="lg:col-span-7 order-1 lg:order-2">
                            <div className="bg-slate-100 border border-slate-200/80 rounded-2xl p-4 shadow-xl shadow-slate-100/50 group relative overflow-hidden aspect-[4/3] flex items-center justify-center">
                                <img 
                                    src="/images/mockup-reporting.png" 
                                    alt="Reporting Mockup" 
                                    className="w-full h-full object-cover rounded-xl shadow border border-slate-200"
                                    onError={(e) => e.currentTarget.style.display = 'none'}
                                />
                                
                                {/* Fallback Interactive CSS Mockup */}
                                <div className="absolute inset-0 w-full h-full bg-white rounded-xl shadow border border-slate-200/80 p-6 flex flex-col justify-between">
                                    <div className="flex justify-between items-center pb-4 border-b border-slate-100">
                                        <div className="flex items-center gap-2">
                                            <FileText className="w-5 h-5 text-amber-500" />
                                            <span className="font-bold text-slate-800 text-sm">Reports Generator</span>
                                        </div>
                                        <button className="px-3 py-1 rounded bg-amber-500 text-white text-[10px] font-bold hover:bg-amber-600 shadow-sm shadow-amber-500/10">Export PDF</button>
                                    </div>
                                    
                                    {/* Mini Chart Mockup */}
                                    <div className="flex-1 flex flex-col justify-center gap-3 my-4">
                                        <div className="flex items-end justify-between h-20 px-4 border-b border-slate-200 pt-2 select-none">
                                            <div className="w-6 bg-slate-200 rounded-t-sm h-[30%]" />
                                            <div className="w-6 bg-slate-200 rounded-t-sm h-[45%]" />
                                            <div className="w-6 bg-slate-200 rounded-t-sm h-[60%]" />
                                            <div className="w-6 bg-amber-500 rounded-t-sm h-[85%]" />
                                            <div className="w-6 bg-amber-600 rounded-t-sm h-[75%]" />
                                            <div className="w-6 bg-slate-300 rounded-t-sm h-[50%]" />
                                        </div>
                                        <div className="flex justify-between text-[9px] text-slate-400 px-2 font-mono">
                                            <span>JAN</span>
                                            <span>FEB</span>
                                            <span>MAR</span>
                                            <span>APR</span>
                                            <span>MAY</span>
                                            <span>JUN</span>
                                        </div>
                                    </div>

                                    {/* Stats and Indicators */}
                                    <div className="grid grid-cols-2 gap-4 pt-3 border-t border-slate-100 text-xs">
                                        <div className="p-2.5 rounded bg-slate-50 border border-slate-200">
                                            <span className="block text-slate-400 text-[9px] mb-0.5">MOST ACTIVE</span>
                                            <span className="font-bold text-slate-800">Milwaukee Tools</span>
                                        </div>
                                        <div className="p-2.5 rounded bg-slate-50 border border-slate-200">
                                            <span className="block text-slate-400 text-[9px] mb-0.5">VALUATION CHANGE</span>
                                            <span className="font-bold text-emerald-600 flex items-center gap-0.5">+$4,280.00 (+12%)</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* FEATURE ROW 4 - SYNC & SECURITY */}
                    <div className="grid lg:grid-cols-12 gap-12 lg:gap-16 items-center">
                        {/* Visual Mockup Column First */}
                        <div className="lg:col-span-7">
                            <div className="bg-slate-100 border border-slate-200/80 rounded-2xl p-4 shadow-xl shadow-slate-100/50 group relative overflow-hidden aspect-[4/3] flex items-center justify-center">
                                <img 
                                    src="/images/mockup-sync.png" 
                                    alt="Sync Mockup" 
                                    className="w-full h-full object-cover rounded-xl shadow border border-slate-200"
                                    onError={(e) => e.currentTarget.style.display = 'none'}
                                />
                                
                                {/* Fallback Interactive CSS Mockup */}
                                <div className="absolute inset-0 w-full h-full bg-slate-900 rounded-xl shadow border border-slate-800 p-6 flex flex-col justify-between text-slate-400 text-xs">
                                    <div className="flex justify-between items-center pb-4 border-b border-slate-800">
                                        <div className="flex items-center gap-2">
                                            <RefreshCw className="w-4 h-4 text-amber-500 animate-spin" style={{ animationDuration: '6s' }} />
                                            <span className="font-bold text-slate-200 text-sm">Real Cloud-Sync Node</span>
                                        </div>
                                        <span className="px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 font-extrabold text-[9px]">ENCRYPTED</span>
                                    </div>

                                    {/* Desktop & Mobile device layout mockup */}
                                    <div className="flex-1 grid grid-cols-2 gap-4 items-center my-4">
                                        {/* Desktop Box representation */}
                                        <div className="p-4 rounded-xl bg-slate-800/40 border border-slate-800 flex flex-col items-center text-center">
                                            <Laptop className="w-10 h-10 text-amber-500 mb-2" />
                                            <span className="font-bold text-slate-200">Desktop Client</span>
                                            <span className="text-[10px] text-slate-500 mt-1">SQLite &bull; Offline Ready</span>
                                        </div>
                                        {/* Mobile Box representation */}
                                        <div className="p-4 rounded-xl bg-slate-800/40 border border-slate-800 flex flex-col items-center text-center">
                                            <Smartphone className="w-10 h-10 text-amber-500 mb-2" />
                                            <span className="font-bold text-slate-200">Mobile Client</span>
                                            <span className="text-[10px] text-slate-500 mt-1">PWA &bull; Real Time Sync</span>
                                        </div>
                                    </div>

                                    <div className="p-3 rounded bg-amber-500/5 border border-amber-500/15 text-amber-400 text-center">
                                        Active connection verified. RSA activation signature cryptographically secure.
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Content Column */}
                        <div className="lg:col-span-5 flex flex-col gap-6">
                            <div className="flex items-center gap-2 bg-amber-50 border border-amber-200 rounded-full px-3 py-1 w-fit">
                                <RefreshCw className="w-4 h-4 text-amber-500" />
                                <span className="text-xs font-extrabold text-amber-800 tracking-wide uppercase">Synchronization</span>
                            </div>
                            <h3 className="text-2xl sm:text-3xl font-extrabold text-slate-900 leading-tight">
                                Automatically sync your stock details across all devices.
                            </h3>
                            <p className="text-slate-600 leading-relaxed">
                                Use MT Glory IMS on mobile, desktop, or tablet without needing stable network connectivity. Our offline-first database saves actions locally and synchronizes seamlessly when a connection is established.
                            </p>
                            <div className="flex flex-col gap-3 text-slate-600">
                                <p className="flex items-center gap-2.5"><Check className="w-5 h-5 text-amber-500 shrink-0" /> Shared database nodes across unlimited users.</p>
                                <p className="flex items-center gap-2.5"><Check className="w-5 h-5 text-amber-500 shrink-0" /> Immediate machine-level backup exports.</p>
                                <p className="flex items-center gap-2.5"><Check className="w-5 h-5 text-amber-500 shrink-0" /> Offline data validation preventing conflicting changes.</p>
                            </div>
                            <div className="pt-2">
                                <button onClick={openRegister} className="bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold px-8 py-3.5 rounded-full shadow-lg shadow-amber-500/20 transition-all">
                                    Get Started Instantly
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            {/* Desktop Native App Downloads Section */}
            <section id="downloads" className="py-24 bg-gradient-to-b from-slate-50 to-white relative">
                <div className="max-w-7xl mx-auto px-6">
                    <div className="text-center max-w-3xl mx-auto mb-16">
                        <span className="text-xs font-bold uppercase tracking-wider text-amber-600 bg-amber-50 border border-amber-200 px-3.5 py-1.5 rounded-full inline-block mb-4">
                            Desktop Launchers
                        </span>
                        <h2 className="text-3xl sm:text-4xl md:text-5xl font-extrabold text-slate-900 tracking-tight mb-4">
                            Download native desktop apps.
                        </h2>
                        <p className="text-lg text-slate-600">
                            Get the native offline app for high-fidelity stock tracking on your office devices.
                        </p>
                    </div>

                    <div className="grid md:grid-cols-2 gap-8 max-w-4xl mx-auto mb-12">
                        {/* Windows Card */}
                        <div className="bg-white border border-slate-200 rounded-3xl p-8 hover:border-amber-500 hover:shadow-xl hover:shadow-slate-100/60 hover:-translate-y-1 transition-all duration-300 flex flex-col justify-between items-center text-center">
                            <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-500 to-cyan-400 text-white flex items-center justify-center shadow-lg shadow-blue-500/10 mb-6">
                                <Laptop className="w-8 h-8" />
                            </div>
                            <h3 className="font-extrabold text-2xl text-slate-950 mb-2">IMS for Windows</h3>
                            <p className="text-slate-500 text-sm mb-6 max-w-xs">
                                Compatible with Windows 10 & 11. Full native installation with integrated automatic updates.
                            </p>
                            
                            {featuredDownloads.find(d => d.os === 'Windows') ? (
                                <button
                                    onClick={() => handleDownload(featuredDownloads.find(d => d.os === 'Windows'))}
                                    className="w-full bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold py-3.5 px-6 rounded-2xl shadow-md shadow-amber-500/10 hover:shadow-amber-500/25 flex items-center justify-center gap-2 transition-all"
                                >
                                    <Download className="w-5 h-5" /> Download Installer (.exe)
                                </button>
                            ) : (
                                <button
                                    onClick={() => openRegister()}
                                    className="w-full bg-slate-900 hover:bg-slate-800 text-white font-bold py-3.5 px-6 rounded-2xl flex items-center justify-center gap-2 transition-all"
                                >
                                    Request Windows Version
                                </button>
                            )}
                            
                            <div className="flex justify-between items-center w-full mt-6 text-xs text-slate-400 font-medium">
                                <span>Release: v2.0.0 Stable</span>
                                <span>Size: ~45MB</span>
                            </div>
                        </div>

                        {/* Linux Card */}
                        <div className="bg-white border border-slate-200 rounded-3xl p-8 hover:border-amber-500 hover:shadow-xl hover:shadow-slate-100/60 hover:-translate-y-1 transition-all duration-300 flex flex-col justify-between items-center text-center">
                            <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-slate-800 to-slate-950 text-white flex items-center justify-center shadow-lg shadow-slate-950/10 mb-6">
                                <Database className="w-8 h-8" />
                            </div>
                            <h3 className="font-extrabold text-2xl text-slate-950 mb-2">IMS for Linux</h3>
                            <p className="text-slate-500 text-sm mb-6 max-w-xs">
                                Compatible with Ubuntu, Debian, RedHat & Arch. Packed inside stable desktop AppImage packages.
                            </p>
                            
                            {featuredDownloads.find(d => d.os === 'Linux') ? (
                                <button
                                    onClick={() => handleDownload(featuredDownloads.find(d => d.os === 'Linux'))}
                                    className="w-full bg-slate-900 hover:bg-slate-800 text-white font-bold py-3.5 px-6 rounded-2xl flex items-center justify-center gap-2 transition-all"
                                >
                                    <Download className="w-5 h-5" /> Download AppImage (.tar.gz)
                                </button>
                            ) : (
                                <button
                                    onClick={() => openRegister()}
                                    className="w-full bg-slate-900 hover:bg-slate-800 text-white font-bold py-3.5 px-6 rounded-2xl flex items-center justify-center gap-2 transition-all"
                                >
                                    Request Linux Version
                                </button>
                            )}
                            
                            <div className="flex justify-between items-center w-full mt-6 text-xs text-slate-400 font-medium">
                                <span>Release: v2.0.0-Beta</span>
                                <span>Size: ~38MB</span>
                            </div>
                        </div>
                    </div>

                    {downloads.length > 0 && (
                        <div className="text-center">
                            <button
                                onClick={() => setShowHistory(true)}
                                className="inline-flex items-center gap-1.5 text-amber-700 hover:text-amber-800 font-bold border border-amber-200 bg-amber-50/50 hover:bg-amber-50 px-5 py-2.5 rounded-full shadow-sm transition-all"
                            >
                                <Clock className="w-4 h-4" /> View All Previous Version History
                            </button>
                        </div>
                    )}

                    {/* Step-by-Step Installation Guides */}
                    <div className="mt-20 border-t border-slate-100 pt-16">
                        <h3 className="text-2xl font-extrabold text-slate-955 text-center mb-10">Quick Setup & Activation Guide</h3>
                        <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-6">
                            <div className="bg-white border border-slate-200/60 rounded-2xl p-6 relative">
                                <span className="absolute top-4 right-6 text-4xl font-black text-amber-500/10">01</span>
                                <h4 className="font-extrabold text-slate-900 mb-2">Download Client</h4>
                                <p className="text-slate-500 text-xs leading-relaxed">Download the secure installer above mapped to your machine operating system files.</p>
                            </div>
                            <div className="bg-white border border-slate-200/60 rounded-2xl p-6 relative">
                                <span className="absolute top-4 right-6 text-4xl font-black text-amber-500/10">02</span>
                                <h4 className="font-extrabold text-slate-900 mb-2">Create Account</h4>
                                <p className="text-slate-500 text-xs leading-relaxed">Register on this license portal to obtain a profile dashboard to track activations.</p>
                            </div>
                            <div className="bg-white border border-slate-200/60 rounded-2xl p-6 relative">
                                <span className="absolute top-4 right-6 text-4xl font-black text-amber-500/10">03</span>
                                <h4 className="font-extrabold text-slate-900 mb-2">Get Activation Key</h4>
                                <p className="text-slate-500 text-xs leading-relaxed">Request a dynamic activation code linked to your subscription tier in the panel.</p>
                            </div>
                            <div className="bg-white border border-slate-200/60 rounded-2xl p-6 relative">
                                <span className="absolute top-4 right-6 text-4xl font-black text-amber-500/10">04</span>
                                <h4 className="font-extrabold text-slate-900 mb-2">Activate Desktop App</h4>
                                <p className="text-slate-500 text-xs leading-relaxed">Boot your offline app, enter your activation signature in settings and unlock all features.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            {/* License Subscription Pricing Section */}
            <section id="pricing" className="py-24 bg-slate-950 text-white relative overflow-hidden">
                <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[800px] h-[800px] bg-amber-500/5 rounded-full blur-3xl pointer-events-none" />

                <div className="max-w-7xl mx-auto px-6 relative z-10">
                    <div className="text-center max-w-3xl mx-auto mb-20">
                        <span className="text-xs font-bold uppercase tracking-wider text-amber-400 bg-amber-500/10 border border-amber-500/20 px-3.5 py-1.5 rounded-full inline-block mb-4">
                            License Tiers
                        </span>
                        <h2 className="text-3xl sm:text-4xl md:text-5xl font-extrabold tracking-tight mb-4 text-white">
                            Flexible pricing scales.
                        </h2>
                        <p className="text-lg text-slate-400">
                            Unlock offline inventory tracking tools designed for your active workflow scale.
                        </p>
                    </div>

                    <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-6 items-stretch">
                        {displayPlans.map((plan) => {
                            const isPro = plan.id === 'pro';
                            return (
                                <div 
                                    key={plan.id} 
                                    className={`bg-slate-900/60 border rounded-3xl p-8 flex flex-col justify-between relative transition-all duration-300 ${isPro ? 'border-amber-500 shadow-xl shadow-amber-500/10 scale-105 z-10 bg-gradient-to-b from-slate-900 to-slate-950' : 'border-slate-800'}`}
                                >
                                    {isPro && (
                                        <span className="absolute -top-3.5 left-1/2 -translate-x-1/2 bg-amber-500 text-slate-950 text-[10px] font-black uppercase tracking-wider px-3.5 py-1 rounded-full shadow-lg shadow-amber-500/20">
                                            Recommended
                                        </span>
                                    )}

                                    <div>
                                        <h3 className="text-slate-400 font-bold uppercase tracking-wider text-xs mb-1">{plan.name}</h3>
                                        <div className="flex items-baseline gap-1 mb-4">
                                            <span className="text-4xl font-extrabold text-white">
                                                {plan.price === 0 ? 'Custom' : `$${plan.price}`}
                                            </span>
                                            {plan.price !== 0 && <span className="text-slate-500 text-sm">/mo</span>}
                                        </div>
                                        <p className="text-slate-400 text-xs leading-relaxed mb-6 h-10 overflow-hidden">{plan.description}</p>
                                        
                                        <hr className="border-slate-800/80 mb-6" />

                                        <ul className="flex flex-col gap-3 mb-8">
                                            {plan.features.map((feat, i) => (
                                                <li key={i} className="flex items-start gap-2 text-xs text-slate-300">
                                                    <Check className="w-4 h-4 text-amber-500 shrink-0 mt-0.5" />
                                                    <span>{feat}</span>
                                                </li>
                                            ))}
                                        </ul>
                                    </div>

                                    {plan.id === 'enterprise' ? (
                                        <a href="#contact" className="w-full py-3 px-4 rounded-xl font-bold bg-transparent border border-slate-700 hover:border-amber-500 hover:text-amber-500 text-center text-xs transition-all">
                                            Contact Sales Desk
                                        </a>
                                    ) : (
                                        <button 
                                            onClick={openRegister}
                                            className={`w-full py-3 px-4 rounded-xl font-bold text-center text-xs transition-all ${isPro ? 'bg-amber-500 hover:bg-amber-600 text-slate-950 shadow-md shadow-amber-500/20' : 'bg-transparent border border-slate-700 hover:border-slate-500 text-slate-200'}`}
                                        >
                                            Activate IMS Launcher
                                        </button>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                </div>
            </section>

            {/* Support Desk Section */}
            <section id="contact" className="py-24 bg-white relative">
                <div className="max-w-3xl mx-auto px-6">
                    <div className="text-center mb-12">
                        <span className="text-xs font-bold uppercase tracking-wider text-amber-600 bg-amber-50 border border-amber-200 px-3.5 py-1.5 rounded-full inline-block mb-4">
                            Support Desk
                        </span>
                        <h2 className="text-3xl sm:text-4xl font-extrabold text-slate-900 tracking-tight mb-3">
                            Need license support?
                        </h2>
                        <p className="text-slate-505 text-sm">
                            Submit a ticket directly. Our team resolves activation or pipeline inquiries within 12 hours.
                        </p>
                    </div>

                    <form className="bg-slate-50 border border-slate-200/80 rounded-3xl p-8 sm:p-10 shadow-lg shadow-slate-100/50 relative" onSubmit={handleContact}>
                        {sent && (
                            <div className="bg-emerald-50 text-emerald-800 p-4 rounded-xl border border-emerald-200 text-center font-bold text-sm mb-6 animate-fadeIn">
                                Message submitted! Our licensing agents will reach out shortly.
                            </div>
                        )}

                        <div className="grid sm:grid-cols-2 gap-6 mb-6">
                            <div>
                                <label className="block text-xs font-extrabold text-slate-700 uppercase tracking-wide mb-2">Full Name</label>
                                <input 
                                    type="text" 
                                    value={contactName} 
                                    onChange={e => setContactName(e.target.value)} 
                                    className="w-full px-4 py-3 rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-amber-500/25 focus:border-amber-500 text-sm transition-all"
                                    placeholder="Enter your name" 
                                    required 
                                />
                            </div>
                            <div>
                                <label className="block text-xs font-extrabold text-slate-700 uppercase tracking-wide mb-2">Email Address</label>
                                <input 
                                    type="email" 
                                    value={contactEmail} 
                                    onChange={e => setContactEmail(e.target.value)} 
                                    className="w-full px-4 py-3 rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-amber-500/25 focus:border-amber-500 text-sm transition-all"
                                    placeholder="your@email.com" 
                                    required 
                                />
                            </div>
                        </div>

                        <div className="mb-8">
                            <label className="block text-xs font-extrabold text-slate-700 uppercase tracking-wide mb-2">How can we help?</label>
                            <textarea 
                                value={contactMsg} 
                                onChange={e => setContactMsg(e.target.value)} 
                                className="w-full px-4 py-3 rounded-xl border border-slate-200 bg-white focus:outline-none focus:ring-2 focus:ring-amber-500/25 focus:border-amber-500 text-sm transition-all resize-none"
                                placeholder="Detail your stock size or activation issue..." 
                                rows={4} 
                                required 
                            />
                        </div>

                        <button 
                            type="submit" 
                            className="w-full bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold py-4 rounded-xl shadow-lg shadow-amber-500/20 hover:shadow-amber-500/35 transition-all flex items-center justify-center gap-2"
                            disabled={sending}
                        >
                            {sending ? 'Sending Ticket...' : <><Send className="w-4 h-4" /> Submit License Ticket</>}
                        </button>
                    </form>
                </div>
            </section>

            {/* Footer */}
            <footer className="bg-slate-900 border-t border-slate-800 text-slate-400 py-16">
                <div className="max-w-7xl mx-auto px-6 grid md:grid-cols-2 gap-12">
                    <div className="flex flex-col gap-4 max-w-sm">
                        <Link href="/" className="flex items-center gap-2 group w-fit">
                            <div className="w-8 h-8 rounded-lg bg-amber-500 flex items-center justify-center text-white shadow-md">
                                <Box className="w-5 h-5" />
                            </div>
                            <span className="font-extrabold text-lg tracking-tight text-white">
                                MT GLORY <span className="text-amber-500">IMS</span>
                            </span>
                        </Link>
                        <p className="text-xs leading-relaxed text-slate-400">
                            Leading enterprise Management Information System (MIS) for professional inventory control. Encrypted offline-first synchronization.
                        </p>
                        <div className="flex items-center gap-2 text-xs text-slate-400 mt-2">
                            <Mail className="w-4 h-4 text-amber-500" />
                            <span>mwimulebienvenu05@gmail.com</span>
                        </div>
                    </div>

                    <div className="flex justify-end gap-16">
                        <div className="flex flex-col gap-3">
                            <h4 className="font-bold text-white text-sm tracking-wide">Legal</h4>
                            <Link href="/about" className="text-xs hover:text-amber-500 transition-colors">About Us</Link>
                            <Link href="/privacy" className="text-xs hover:text-amber-500 transition-colors">Privacy Policy</Link>
                            <Link href="/terms" className="text-xs hover:text-amber-500 transition-colors">License Agreement</Link>
                        </div>
                    </div>
                </div>

                <div className="max-w-7xl mx-auto px-6 border-t border-slate-800/80 mt-12 pt-8 text-center text-xs text-slate-650 font-medium">
                    &copy; {new Date().getFullYear()} MT Glory CO. All rights reserved. Made by MSS.
                </div>
            </footer>

            {/* Support Modals */}
            <LoginModal isOpen={showLogin} onClose={() => setShowLogin(false)} onSwitchToRegister={openRegister} />
            <RegisterModal isOpen={showRegister} onClose={() => setShowRegister(false)} onSwitchToLogin={openLogin} />
            <DemoRequestModal isOpen={showDemo} onClose={() => setShowDemo(false)} />

            {/* Version History Modal */}
            {showHistory && (
                <div className="fixed inset-0 z-[2000] flex items-center justify-center p-4 bg-slate-950/60 backdrop-blur-sm animate-fadeIn">
                    <div className="bg-white rounded-3xl w-full max-w-2xl p-6 md:p-8 shadow-2xl border border-slate-100 flex flex-col justify-between max-h-[85vh]">
                        <div className="flex justify-between items-center pb-4 border-b border-slate-100">
                            <h3 className="text-xl font-extrabold text-slate-900 flex items-center gap-2">
                                <Clock className="w-5 h-5 text-amber-500" /> Desktop Version History
                            </h3>
                            <button onClick={() => setShowHistory(false)} className="p-1 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-slate-600">
                                <X className="w-6 h-6" />
                            </button>
                        </div>

                        <div className="flex-1 overflow-y-auto my-6 pr-2">
                            <table className="w-full text-left text-xs border-collapse">
                                <thead>
                                    <tr className="bg-slate-50 border-b border-slate-100 text-slate-500 font-extrabold uppercase tracking-wider">
                                        <th className="p-3">Version</th>
                                        <th className="p-3">OS Platform</th>
                                        <th className="p-3">Downloads</th>
                                        <th className="p-3">Release Date</th>
                                        <th className="p-3 text-right">Action</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {downloads.map(d => (
                                        <tr key={d._id} className="border-b border-slate-100 hover:bg-slate-50/50">
                                            <td className="p-3 font-bold text-slate-900">
                                                v{d.version} {d.isFeatured && <span className="bg-emerald-50 text-emerald-700 border border-emerald-200/50 text-[9px] font-extrabold px-2 py-0.5 rounded-full ml-1.5">LATEST</span>}
                                            </td>
                                            <td className="p-3 font-medium text-slate-600">{d.os}</td>
                                            <td className="p-3 text-slate-500">{d.downloadCount ? d.downloadCount.toLocaleString() : 0}</td>
                                            <td className="p-3 text-slate-400">{new Date(d.releaseDate).toLocaleDateString()}</td>
                                            <td className="p-3 text-right">
                                                <button 
                                                    onClick={() => { handleDownload(d); setShowHistory(false); }}
                                                    className="text-amber-600 hover:text-amber-700 font-bold hover:underline"
                                                >
                                                    Download &rarr;
                                                </button>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        <div className="pt-4 border-t border-slate-100 flex justify-end">
                            <button 
                                onClick={() => setShowHistory(false)}
                                className="bg-slate-900 hover:bg-slate-800 text-white font-bold py-2.5 px-6 rounded-xl text-xs"
                            >
                                Close History View
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
