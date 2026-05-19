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
    ShieldCheck, 
    Activity, 
    Settings
} from 'lucide-react';

export default function ReleaseNotesPage() {
    const params = useParams();
    const router = useRouter();
    const version = params.version || '2.0.0';

    // Version-specific content mapping precisely to the actual software implementation
    const versionContent = {
        '1.0.0': {
            title: "The Core Foundations.",
            subtitle: "Version 1.0.0 introduces the secure, offline-first inventory management local engine built for business tracking.",
            transactionCount: "10,000",
            changes: [
                {
                    title: "Core Stock Controls",
                    icon: <Package size={24} className="text-amber-500" />,
                    description: "Dynamic product database containing category filters, price registers, stock counts, and direct Stock IN/OUT adjustment forms.",
                    features: ["Product Master Database", "Category & SKU Categorization", "Stock Adjustments Sheets"]
                },
                {
                    title: "Offline-First local SQLite",
                    icon: <Database size={24} className="text-amber-500" />,
                    description: "Secure local data persistence using structured sqlite-net-pcl relational tables and thread-safe db transactions.",
                    features: ["Zero Latency Local Database", "Manual SQLite Backup Logs", "Fully Offline Working Capability"]
                },
                {
                    title: "Basic User Account Access",
                    icon: <Settings size={24} className="text-amber-500" />,
                    description: "Operator profile permissions with protected login prompts and salted password hashing algorithms.",
                    features: ["Local Console Gating", "Salted Password Hashing", "Secure User Profiles Settings"]
                }
            ]
        },
        '2.0.0': {
            title: "The Enterprise Upgrade.",
            subtitle: "Version 2.0.0 introduces intelligent demand forecasting, strict FIFO batch depletion, multi-location networks, and hardware integrations.",
            transactionCount: "50,000",
            changes: [
                {
                    title: "FIFO Batch Costing Engine",
                    icon: <Database size={24} className="text-amber-500" />,
                    description: "Strict FIFO batch cost calculation. Every stock OUT depletes available PurchaseBatch units in chronological order, locking in precise historic COGS and profit metrics.",
                    features: ["Dynamic COGS & Profit margins", "Lot-by-lot batch tracker pools", "Historical transaction snapshots"]
                },
                {
                    title: "Supplier & Procurement Lifecycle",
                    icon: <Truck size={24} className="text-amber-500" />,
                    description: "Complete procurement pipeline from Draft to Approved, Shipped, and Received stages. Real-time supplier scorecards track on-time delivery rates and average lead times.",
                    features: ["Unique Auto-generated PO manifest", "Interactive PO Status badges", "Weighted Supplier Scoring dashboard"]
                },
                {
                    title: "Multi-Location Control Network",
                    icon: <Box size={24} className="text-amber-500" />,
                    description: "Track inventory valuation and quantities across physical warehouses, retail outlets, and mobile vans with active in-transit Stock Transfer workflows.",
                    features: ["Stock Transfer approval runs", "Location-specific safety thresholds", "Consolidated total company counts"]
                },
                {
                    title: "Advanced Analytics BI Matrix",
                    icon: <BarChart3 size={24} className="text-amber-500" />,
                    description: "Segment catalog performance using 90-day demand volatility metrics (XYZ classification) and dynamic historic revenue share contribution scores (ABC analysis).",
                    features: ["Pareto (80/20) revenue classification", "Demand stability tracking coefficients", "Consolidated inventory health metric"]
                },
                {
                    title: "Linear Regression Forecasting",
                    icon: <Zap size={24} className="text-amber-500" />,
                    description: "Project future item replenishment counts using historical linear regression lines ($y = m \\cdot x + b$) combined with Economic Order Quantity (EOQ) optimization models.",
                    features: ["Economic Order Quantity models", "Days-until-depletion countdown metrics", "Sales velocity trend calculators"]
                },
                {
                    title: "Proactive Morning Briefing",
                    icon: <LayoutDashboard size={24} className="text-amber-500" />,
                    description: "Start the day with a dynamic operational list of action items: batch recalls, expiring items, out-of-stock items, and pending PO confirmations.",
                    features: ["Impending stockout warnings", "Batch expiry quarantine list", "Single-click PO approval cards"]
                },
                {
                    title: "Thermal POS & Barcode Buffer",
                    icon: <Printer size={24} className="text-amber-500" />,
                    description: "Direct printing to 80mm and 58mm thermal receipt hardware using custom ESC/POS buffers. High-speed USB barcode capture updates carts in under 50ms.",
                    features: ["Native ESC/POS print commands", "Fast scanning buffer capture", "Integrated price sticker generation"]
                },
                {
                    title: "Enterprise Security & Audits",
                    icon: <ShieldCheck size={24} className="text-amber-500" />,
                    description: "Forensic state history capturing pre-and-post entity values into JSON strings. Cryptographic licensing verifies RSA-signed keys bound to system hardware bios fingerprints.",
                    features: ["JSON-snapshot audit trail tables", "Granular role access parameters (RBAC)", "System-clock anti-rollback guards"]
                },
                {
                    title: "Lot Quality & Kitting Engine",
                    icon: <Activity size={24} className="text-amber-500" />,
                    description: "Lock batches approaching lot expiration dates using status markers (Rejected, Recalled) and package standard items into custom assembly bundles.",
                    features: ["Batch quality gating parameters", "Kit assembly and disassembly logic", "Detailed waste and stock loss analysis"]
                }
            ]
        }
    };

    const currentVersionContent = versionContent[version as keyof typeof versionContent] || versionContent['2.0.0'];
    const changes = currentVersionContent.changes;

    return (
        <div className="bg-slate-950 min-h-screen text-slate-100 flex flex-col relative overflow-hidden font-sans">
            {/* Ambient Background Gradients */}
            <div className="absolute top-0 right-0 w-[600px] h-[600px] bg-amber-500/5 rounded-full blur-[120px] pointer-events-none" />
            <div className="absolute -left-20 top-40 w-[400px] h-[400px] bg-amber-500/5 rounded-full blur-[100px] pointer-events-none" />

            {/* Premium Sticky Glass Header */}
            <header className="sticky top-0 z-50 bg-slate-950/80 backdrop-blur-md border-b border-slate-900 py-4">
                <div className="max-w-7xl mx-auto px-6 flex justify-between items-center w-full">
                    <Link href="/" className="flex items-center gap-2 group text-slate-400 hover:text-amber-500 font-bold transition-all text-sm">
                        <ArrowLeft size={16} className="group-hover:-translate-x-1 transition-transform" />
                        <span>Back to Home</span>
                    </Link>
                    <div className="flex items-center gap-2">
                        <div className="w-8 h-8 rounded-lg bg-amber-500 flex items-center justify-center text-slate-950 font-black">
                            <Box className="w-5 h-5" />
                        </div>
                        <span className="font-extrabold text-sm tracking-wide text-white">
                            MT GLORY <span className="text-amber-500">IMS</span>
                        </span>
                    </div>
                </div>
            </header>

            {/* Main Content Container */}
            <main className="max-w-7xl mx-auto px-6 flex-1 py-16 relative z-10 w-full">
                {/* Hero Feature Block */}
                <motion.div 
                    initial={{ opacity: 0, y: 25 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.6 }}
                    className="text-center max-w-4xl mx-auto mb-16"
                >
                    <span className="bg-amber-500/10 text-amber-500 border border-amber-500/20 px-4 py-1.5 rounded-full text-xs font-black tracking-wider uppercase inline-block mb-6 shadow-sm">
                        RELEASE NOTES v{version}
                    </span>
                    <h1 className="bg-gradient-to-r from-amber-400 via-amber-500 to-amber-600 bg-clip-text text-transparent text-4xl sm:text-5xl md:text-6xl font-extrabold tracking-tight mb-6 leading-tight">
                        {currentVersionContent.title}
                    </h1>
                    <p className="text-slate-400 text-lg sm:text-xl leading-relaxed max-w-3xl mx-auto">
                        {currentVersionContent.subtitle} Already running over <strong className="text-slate-200">{currentVersionContent.transactionCount}</strong> transactions smoothly on active client devices.
                    </p>
                </motion.div>

                {/* Key Core Changes Section */}
                <div className="mt-12">
                    <div className="text-center mb-12">
                        <h2 className="text-2xl sm:text-3xl font-extrabold text-white tracking-tight relative inline-block">
                            Engineered System Upgrades
                            <span className="absolute bottom-[-10px] left-1/2 -translate-x-1/2 w-12 h-1 bg-amber-500 rounded-full" />
                        </h2>
                    </div>

                    {/* Dynamic Glassmorphic Card Grid */}
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                        {changes.map((change, idx) => (
                            <motion.div 
                                key={idx}
                                initial={{ opacity: 0, y: 20 }}
                                whileInView={{ opacity: 1, y: 0 }}
                                viewport={{ once: true }}
                                transition={{ delay: idx * 0.08, duration: 0.5 }}
                                className="bg-slate-900/40 border border-slate-800/80 hover:border-amber-500/30 rounded-3xl p-6 flex flex-col justify-between shadow-2xl hover:shadow-amber-500/5 transition-all duration-300 hover:-translate-y-1"
                            >
                                <div>
                                    <div className="flex items-center gap-4 mb-4">
                                        <div className="w-12 h-12 rounded-2xl bg-amber-500/10 text-amber-500 border border-amber-500/20 flex items-center justify-center shrink-0 shadow-inner">
                                            {change.icon}
                                        </div>
                                        <h3 className="text-lg font-bold text-white tracking-tight">{change.title}</h3>
                                    </div>
                                    <p className="text-slate-400 text-sm leading-relaxed mb-6">
                                        {change.description}
                                    </p>
                                </div>
                                <ul className="flex flex-col gap-2.5 border-t border-slate-800/50 pt-4">
                                    {change.features.map((feat, i) => (
                                        <li key={i} className="flex items-start gap-2 text-xs font-semibold text-slate-300 leading-tight">
                                            <ChevronRight size={14} className="text-amber-500 shrink-0 mt-0.5" />
                                            <span>{feat}</span>
                                        </li>
                                    ))}
                                </ul>
                            </motion.div>
                        ))}
                    </div>
                </div>

                {/* Call To Action Glassmorphic Box */}
                <motion.div 
                    initial={{ opacity: 0, scale: 0.98 }}
                    whileInView={{ opacity: 1, scale: 1 }}
                    viewport={{ once: true }}
                    className="bg-gradient-to-b from-slate-900/60 to-slate-950 border border-slate-800/80 rounded-3xl p-8 sm:p-12 text-center mt-20 max-w-4xl mx-auto shadow-2xl shadow-amber-500/5 relative overflow-hidden"
                >
                    <div className="absolute top-[-50px] right-[-50px] w-48 h-48 bg-amber-500/5 rounded-full blur-2xl pointer-events-none" />
                    <div className="relative z-10">
                        <h3 className="text-3xl font-extrabold text-white mb-4">Ready to upgrade your enterprise setup?</h3>
                        <p className="text-slate-400 max-w-xl mx-auto text-sm sm:text-base leading-relaxed mb-8">
                            Lock in your upgrade to Version 2.0.0. Activate our Enterprise or Pro subscription tier now to unlock premium forecasting metrics and live cloud backups.
                        </p>
                        <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
                            <button 
                                onClick={() => router.push('/?auth=register')} 
                                className="w-full sm:w-auto bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-slate-950 font-black px-8 py-3.5 rounded-xl shadow-lg shadow-amber-500/10 hover:shadow-amber-500/25 transition-all duration-150 text-sm hover:-translate-y-0.5 active:translate-y-0"
                            >
                                Request Enterprise Trial
                            </button>
                            <button 
                                onClick={() => router.push('/#contact')} 
                                className="w-full sm:w-auto bg-transparent border border-slate-800 hover:border-amber-500 hover:text-amber-500 text-slate-350 font-bold px-8 py-3.5 rounded-xl transition-all duration-150 text-sm hover:-translate-y-0.5"
                            >
                                Talk to an Expert
                            </button>
                        </div>
                    </div>
                </motion.div>
            </main>

            {/* Clean Centered Footer */}
            <footer className="py-8 border-t border-slate-900 text-center text-xs text-slate-500">
                <div className="max-w-7xl mx-auto px-6">
                    <p>&copy; {new Date().getFullYear()} MT Glory . Built for African Enterprises.</p>
                </div>
            </footer>
        </div>
    );
}
